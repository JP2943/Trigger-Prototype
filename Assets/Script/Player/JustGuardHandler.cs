using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class JustGuardHandler : MonoBehaviour
{
    [Header("Links")]
    public MonoBehaviour guardProvider;          // 例: PlayerHealthGuard（IsGuarding を読むだけ）
    public InputActionReference guardAction;

    [Header("Window")]
    [Tooltip("ジャスト判定フレーム数（例:5）")] public int justGuardFrames = 5;
    [Tooltip("基準FPS。通常60固定でOK")] public float baseFps = 60f;

    [Header("VFX & SFX - Prefab / Sorting")]
    public GameObject justGuardVfxPrefab;       // 表示するプレファブ
    public Transform vfxAnchor;                 // エフェクトの親（未指定なら this）
    public Vector3 vfxLocalOffset;
    [Min(0.01f)] public float vfxScale = 1f;
    public string vfxSortingLayer = "Foreground";
    public int vfxOrderInLayer = 1200;

    [Header("VFX - Playback")]
    [Tooltip("この秒数で1周見せ切る（durationを圧縮）")] public float vfxPlaybackSeconds = 0.6f;
    [Tooltip("最長duration/秒数でsimulationSpeedを自動設定")] public bool fitFullCycleToPlayback = true;
    [Tooltip("速度の手動倍率（1=等速）")] public float manualSpeedScale = 1f;
    [Tooltip("ループを強制OFF（1周のみ表示）")] public bool forceNoLoop = true;
    [Tooltip("Destroyまでの余韻")] public float destroyGrace = 0.1f;

    [Header("VFX - Appearance")]
    [Range(0f, 1f)] public float vfxAlpha = 1f;  // 透明度（0〜1）
    [Tooltip("マテリアル/パーティクルの色アルファに vfxAlpha を乗算")]
    public bool applyAlphaToMaterials = true;

    [Header("SFX")]
    public AudioClip justGuardSe;
    public AudioSource audioSource;

    // ---- runtime ----
    float _lastGuardPressedTime = -999f;
    bool _prevGuard;

    float WindowSeconds => Mathf.Max(1, justGuardFrames) / Mathf.Max(15f, baseFps);

    void OnEnable() { guardAction?.action.Enable(); }
    void OnDisable() { guardAction?.action.Disable(); }

    void Update()
    {
        bool pressedThisFrame = false;

        if (guardAction && guardAction.action != null)
            pressedThisFrame |= guardAction.action.WasPressedThisFrame();

        if (guardProvider != null)
        {
            var prop = guardProvider.GetType().GetProperty(
                "IsGuarding",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (prop != null && prop.PropertyType == typeof(bool))
            {
                bool now = (bool)prop.GetValue(guardProvider);
                if (!_prevGuard && now) pressedThisFrame = true; // ガード押下立ち上がり
                _prevGuard = now;
            }
        }

        if (pressedThisFrame) _lastGuardPressedTime = Time.time;
    }

    /// <summary>
    /// ダメージ処理の冒頭で呼ぶ。成立したら 0/0/0 にして true を返す。
    /// </summary>
    public bool TryApplyJustGuard(ref int hpDamage, ref int staminaDamage, ref Vector2 knockback)
    {
        if (Time.time - _lastGuardPressedTime <= WindowSeconds)
        {
            hpDamage = 0;
            staminaDamage = 0;
            knockback = Vector2.zero;
            SpawnVfxAndSfx();
            return true;
        }
        return false;
    }

    void SpawnVfxAndSfx()
    {
        if (justGuardVfxPrefab)
        {
            var anchor = vfxAnchor ? vfxAnchor : transform;
            var go = Instantiate(justGuardVfxPrefab, anchor);
            go.transform.localPosition = vfxLocalOffset;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * vfxScale;

            // 1) 描画順を最前面へ（Transparentキューも強制）
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.sortingLayerName = vfxSortingLayer;
                r.sortingOrder = vfxOrderInLayer;
                var mats = r.materials; // インスタンス化
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i].renderQueue = 3000; // Transparent
                    if (applyAlphaToMaterials) TrySetMaterialAlpha(mats[i], vfxAlpha);
                }
            }

            // 2) ParticleSystem を短尺再生 & Alpha 乗算
            var psList = new List<ParticleSystem>(go.GetComponentsInChildren<ParticleSystem>(true));
            float longest = 0f;
            foreach (var ps in psList)
            {
                var m = ps.main;
                longest = Mathf.Max(longest, m.duration);
            }
            if (longest <= 0f) longest = 1f;

            float speed = manualSpeedScale;
            if (fitFullCycleToPlayback && vfxPlaybackSeconds > 0.01f)
                speed *= (longest / vfxPlaybackSeconds);

            foreach (var ps in psList)
            {
                var m = ps.main;
                if (forceNoLoop) m.loop = false;

                // 開始遅延を潰す
                if (m.startDelay.mode != ParticleSystemCurveMode.Constant || m.startDelay.constant > 0f)
                    m.startDelay = 0f;

                m.simulationSpeed = speed;

                if (applyAlphaToMaterials)
                {
                    // startColor
                    var col = GetColorFromMinMax(m.startColor);
                    col.a = Mathf.Clamp01(col.a * vfxAlpha);
                    m.startColor = new ParticleSystem.MinMaxGradient(col);

                    // colorOverLifetime のアルファを乗算
                    var colLife = ps.colorOverLifetime;
                    if (colLife.enabled)
                    {
                        var g = colLife.color;
                        MultiplyGradientAlpha(ref g, vfxAlpha);
                        colLife.color = g;
                    }
                }

                ps.Play(true);
            }

            Destroy(go, Mathf.Max(0.01f, vfxPlaybackSeconds + destroyGrace));
        }

        if (justGuardSe)
        {
            if (audioSource) audioSource.PlayOneShot(justGuardSe, 1f);
            else AudioSource.PlayClipAtPoint(justGuardSe, (vfxAnchor ? vfxAnchor.position : transform.position), 1f);
        }
    }

    // ===== Helper =====

    static Color GetColorFromMinMax(ParticleSystem.MinMaxGradient g)
    {
        switch (g.mode)
        {
            case ParticleSystemGradientMode.Color: return g.color;
            case ParticleSystemGradientMode.TwoColors: return (g.color + g.colorMax) * 0.5f;
            case ParticleSystemGradientMode.Gradient: return g.gradient.Evaluate(0.5f);
            case ParticleSystemGradientMode.TwoGradients: return g.gradient.Evaluate(0.5f);
            default: return Color.white;
        }
    }

    static void MultiplyGradientAlpha(ref ParticleSystem.MinMaxGradient g, float mul)
    {
        mul = Mathf.Clamp01(mul);
        if (g.mode == ParticleSystemGradientMode.Color)
        {
            var c = g.color; c.a = Mathf.Clamp01(c.a * mul); g.color = c;
        }
        else if (g.mode == ParticleSystemGradientMode.TwoColors)
        {
            var c1 = g.color; c1.a = Mathf.Clamp01(c1.a * mul);
            var c2 = g.colorMax; c2.a = Mathf.Clamp01(c2.a * mul);
            g = new ParticleSystem.MinMaxGradient(c1, c2);
        }
        else if (g.mode == ParticleSystemGradientMode.Gradient && g.gradient != null)
        {
            var grad = new Gradient();
            var keysC = g.gradient.colorKeys;
            var keysA = g.gradient.alphaKeys;
            for (int i = 0; i < keysA.Length; i++)
                keysA[i].alpha = Mathf.Clamp01(keysA[i].alpha * mul);
            grad.SetKeys(keysC, keysA);
            g = new ParticleSystem.MinMaxGradient(grad);
        }
        else if (g.mode == ParticleSystemGradientMode.TwoGradients && g.gradient != null && g.gradientMax != null)
        {
            Gradient Fix(Gradient src)
            {
                var grad = new Gradient();
                var keysC = src.colorKeys;
                var keysA = src.alphaKeys;
                for (int i = 0; i < keysA.Length; i++)
                    keysA[i].alpha = Mathf.Clamp01(keysA[i].alpha * mul);
                grad.SetKeys(keysC, keysA);
                return grad;
            }
            g = new ParticleSystem.MinMaxGradient(Fix(g.gradient), Fix(g.gradientMax));
        }
    }

    static void TrySetMaterialAlpha(Material m, float alpha)
    {
        alpha = Mathf.Clamp01(alpha);
        var ids = new int[]
        {
            Shader.PropertyToID("_BaseColor"),
            Shader.PropertyToID("_Color"),
            Shader.PropertyToID("_Tint"),
            Shader.PropertyToID("_TintColor"),
            Shader.PropertyToID("_MainColor")
        };
        foreach (var id in ids)
        {
            if (m.HasProperty(id))
            {
                var c = m.GetColor(id);
                c.a = Mathf.Clamp01(c.a * alpha);
                m.SetColor(id, c);
                break;
            }
        }
        m.renderQueue = 3000; // Transparent
    }
}
