using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class Gun2DCharge : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform armPivot;
    [SerializeField] private Transform muzzle;
    [SerializeField] private Rigidbody2D bodyRb;
    [SerializeField] private Bullet2D normalBullet;
    [SerializeField] private Bullet2D chargedBullet;
    [SerializeField] private ArmAimer aimer;
    [SerializeField] private PlayerHealthGuard guardRef;

    [Header("Input (RT / Mouse LMB optional)")]
    public InputActionReference fireAction;
    [SerializeField] private bool allowMouseFallback = true;

    [Header("Charge")]
    [SerializeField] private float chargeSeconds = 1.5f;

    [Header("Cooldowns")]
    [SerializeField] private float normalCooldown = 0.18f;
    [SerializeField] private float chargedCooldown = 0.35f;

    [Header("Recoil (Normal)")]
    [SerializeField] private float bodyKickSpeed_Normal = 2.5f;
    [SerializeField] private float armKickDeg_Normal = 14f;

    [Header("Recoil (Charged)")]
    [SerializeField] private float bodyKickSpeed_Charged = 5.5f;
    [SerializeField] private float armKickDeg_Charged = 28f;

    [Header("Charge VFX (Shuriken Prefab)")]
    [Tooltip("e.g. ForceField01_5secs.prefab")]
    [SerializeField] private GameObject chargeVfxPrefab;
    [SerializeField] private Transform vfxAnchor;
    [SerializeField] private Vector3 vfxLocalOffset = Vector3.zero;
    [SerializeField] private float vfxScale = 1f;

    [Header("VFX Sorting (frontmost)")]
    [SerializeField] private string vfxSortingLayer = "Foreground";
    [SerializeField] private int vfxOrderInLayer = 1000;

    [Header("VFX Opacity (fade during charge)")]
    [SerializeField] private bool fadeInDuringCharge = true;
    [SerializeField, Range(0f, 1f)] private float startOpacity = 0f;
    [SerializeField]
    private AnimationCurve opacityCurve =
        AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("SFX (optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sfxChargeStart;
    [SerializeField] private AudioClip sfxChargeLoop;
    [SerializeField] private AudioClip sfxFireNormal;
    [SerializeField] private AudioClip sfxFireCharged;
    [SerializeField, Range(0f, 1f)] private float shotVolume = 0.9f;

    float nextFireAt;
    bool charging;
    float chargeStart;
    BodyLocomotion2D locomotion;

    GameObject vfxInstance;
    List<ParticleSystem> vfxParticles = new();
    float vfxOneCycle = 0f;
    Coroutine vfxSpeedRoutine;

    // -------- Material / Opacity cache --------
    struct MatSlot
    {
        public Material mat;
        public int colorProp;     // _Color/_BaseColor/_TintColor/Color/_EmissionColor いずれか
        public Color baseColor;
        public List<int> floatProps;   // _Intensity/_Brightness/_Fresnel/Fresnel/_Glow/_EmissionStrength/Opacity 等
        public List<float> baseFloats;
    }
    List<MatSlot> vfxMatSlots = new();

    // 頂点カラー(COL)フェードも併用（Additiveでも効かせやすい）
    List<ParticleSystem.ColorOverLifetimeModule> vfxCOL = new();
    List<Gradient> vfxCOLOriginal = new();

    // よく使うプロパティID
    static readonly int ID_Color = Shader.PropertyToID("_Color");
    static readonly int ID_BaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int ID_TintColor = Shader.PropertyToID("_TintColor");
    static readonly int ID_Emission = Shader.PropertyToID("_EmissionColor");
    // 代表的な“明るさ”系
    static readonly int ID_Intensity = Shader.PropertyToID("_Intensity");
    static readonly int ID_Brightness = Shader.PropertyToID("_Brightness");
    static readonly int ID_Fresnel = Shader.PropertyToID("_Fresnel");
    static readonly int ID_Fresnel2 = Shader.PropertyToID("Fresnel");
    static readonly int ID_Glow = Shader.PropertyToID("_Glow");
    static readonly int ID_Emissive = Shader.PropertyToID("_EmissionStrength");
    static readonly int ID_Opacity = Shader.PropertyToID("_Opacity");

    void Awake()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (bodyRb) locomotion = bodyRb.GetComponent<BodyLocomotion2D>();
        if (!aimer && armPivot) aimer = armPivot.GetComponent<ArmAimer>();
        if (!aimer) aimer = GetComponentInParent<ArmAimer>() ?? GetComponentInChildren<ArmAimer>(true);
        if (!vfxAnchor) vfxAnchor = armPivot ? armPivot : transform;
    }

    void OnEnable() { fireAction?.action.Enable(); }
    void OnDisable() { fireAction?.action.Disable(); StopChargeLoop(); KillVfxImmediate(); }

    void Update()
    {
        if (guardRef && (guardRef.IsGuarding || guardRef.IsHurt || guardRef.IsDashing || guardRef.IsDead))
        {
            if (charging) CancelCharge();
            return;
        }

        bool pressedDown = false, released = false, pressed = false;
        if (fireAction)
        {
            var a = fireAction.action;
            pressedDown = a.WasPressedThisFrame();
            released = a.WasReleasedThisFrame();
            pressed = a.IsPressed();
        }
        if (allowMouseFallback && Mouse.current != null)
        {
            pressedDown |= Mouse.current.leftButton.wasPressedThisFrame;
            released |= Mouse.current.leftButton.wasReleasedThisFrame;
            pressed |= Mouse.current.leftButton.isPressed;
        }

        if (pressedDown) BeginCharge();
        if (charging && !pressed) released = true;
        if (charging && released) ReleaseToFire();

        // チャージ中のフェード
        if (charging && fadeInDuringCharge && vfxInstance)
        {
            float t = Mathf.Clamp01((Time.time - chargeStart) / Mathf.Max(0.01f, chargeSeconds));
            float o = Mathf.Clamp01(Mathf.Lerp(startOpacity, 1f, opacityCurve.Evaluate(t)));
            SetVfxOpacityUnified(o);     // マテリアルの色/強度を下げる（ShaderGraph対応）
            SetVfxOpacityByCOL(o);       // 頂点カラー経由でも下げる（Additive対策）
        }
    }

    // ========== Charge Begin / Release ==========
    void BeginCharge()
    {
        charging = true;
        chargeStart = Time.time;

        if (audioSource && sfxChargeStart) audioSource.PlayOneShot(sfxChargeStart, 1f);
        if (audioSource && sfxChargeLoop) { audioSource.clip = sfxChargeLoop; audioSource.loop = true; audioSource.PlayDelayed(0.05f); }

        SpawnAndPlayVfx();
        if (fadeInDuringCharge) SetVfxOpacityUnified(startOpacity);
    }

    void ReleaseToFire()
    {
        float held = Time.time - chargeStart;
        bool isCharged = held >= chargeSeconds;

        charging = false;
        StopChargeLoop();
        StopAndDestroyVfx();

        if (!muzzle) return;
        if (Time.time < nextFireAt) return;

        if (isCharged)
        {
            if (!chargedBullet) return;
            nextFireAt = Time.time + chargedCooldown;
            Instantiate(chargedBullet, muzzle.position, muzzle.rotation);
            ApplyRecoil(bodyKickSpeed_Charged, armKickDeg_Charged);
            if (audioSource && sfxFireCharged) audioSource.PlayOneShot(sfxFireCharged, shotVolume);
        }
        else
        {
            if (!normalBullet) return;
            nextFireAt = Time.time + normalCooldown;
            Instantiate(normalBullet, muzzle.position, muzzle.rotation);
            ApplyRecoil(bodyKickSpeed_Normal, armKickDeg_Normal);
            if (audioSource && sfxFireNormal) audioSource.PlayOneShot(sfxFireNormal, shotVolume);
        }
    }

    void CancelCharge()
    {
        charging = false;
        StopChargeLoop();
        StopAndDestroyVfx();
    }

    // ========== Recoil ==========
    void ApplyRecoil(float bodyKickSpeed, float armKickDeg)
    {
        if (locomotion != null)
        {
            Vector2 back = -muzzle.right;
            locomotion.AddRecoil(back, bodyKickSpeed);
        }
        else if (bodyRb != null)
        {
            Vector2 back = -muzzle.right * bodyKickSpeed;
            bodyRb.AddForce(back, ForceMode2D.Impulse);
        }
        if (aimer != null) aimer.AddRecoilUpwards(armKickDeg);
    }

    // ========== VFX Handling ==========
    void SpawnAndPlayVfx()
    {
        if (!chargeVfxPrefab || vfxInstance) return;

        vfxInstance = Instantiate(chargeVfxPrefab, vfxAnchor);
        vfxInstance.transform.localPosition = vfxLocalOffset;
        vfxInstance.transform.localRotation = Quaternion.identity;
        vfxInstance.transform.localScale = Vector3.one * Mathf.Max(0.0001f, vfxScale);

        foreach (var r in vfxInstance.GetComponentsInChildren<Renderer>(true))
        {
            r.sortingLayerName = vfxSortingLayer;
            r.sortingOrder = vfxOrderInLayer;
        }

        // ParticleSystems
        vfxParticles.Clear();
        vfxOneCycle = 0f;
        var psArray = vfxInstance.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in psArray)
        {
            vfxParticles.Add(ps);
            var m = ps.main; m.loop = true;
            vfxOneCycle = Mathf.Max(vfxOneCycle, m.duration);
            ps.Clear(true);
        }
        if (vfxOneCycle <= 0f) vfxOneCycle = 1f;

        float speedForFirstLoop = vfxOneCycle / Mathf.Max(0.01f, chargeSeconds);
        SetVfxSpeed(speedForFirstLoop);
        foreach (var ps in vfxParticles) ps.Play(true);

        if (vfxSpeedRoutine != null) StopCoroutine(vfxSpeedRoutine);
        vfxSpeedRoutine = StartCoroutine(VfxSpeedNormalizeAt(chargeSeconds));

        // マテリアル/カラー/強度のキャッシュ
        CacheVfxMaterials();
        // COL のキャッシュも（Additive対策）
        CacheColorOverLifetime();
    }

    IEnumerator VfxSpeedNormalizeAt(float afterSeconds)
    {
        float t0 = Time.time;
        while (charging && Time.time - t0 < afterSeconds) yield return null;
        if (!charging) yield break;

        SetVfxSpeed(1f);
        SetVfxOpacityUnified(1f);   // 完了時にMAX
        SetVfxOpacityByCOL(1f);
        vfxSpeedRoutine = null;
    }

    void SetVfxSpeed(float speed)
    {
        foreach (var ps in vfxParticles)
        {
            var m = ps.main;
            m.simulationSpeed = speed;
        }
    }

    // ---- Material brightness/alpha unified fade (ShaderGraphにも効く) ----
    void CacheVfxMaterials()
    {
        vfxMatSlots.Clear();
        var rends = vfxInstance.GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends)
        {
            foreach (var mat in r.materials) // 個別インスタンス化
            {
                MatSlot s = new MatSlot
                {
                    mat = mat,
                    colorProp = 0,
                    baseColor = Color.white,
                    floatProps = new List<int>(),
                    baseFloats = new List<float>()
                };

                // 色プロパティを探す（優先順）
                if (mat.HasProperty(ID_Color)) { s.colorProp = ID_Color; s.baseColor = mat.GetColor(ID_Color); }
                else if (mat.HasProperty(ID_BaseColor)) { s.colorProp = ID_BaseColor; s.baseColor = mat.GetColor(ID_BaseColor); }
                else if (mat.HasProperty(ID_TintColor)) { s.colorProp = ID_TintColor; s.baseColor = mat.GetColor(ID_TintColor); }
                else if (mat.HasProperty(ID_Emission)) { s.colorProp = ID_Emission; s.baseColor = mat.GetColor(ID_Emission); }
                else if (mat.HasProperty(Shader.PropertyToID("Color")))
                { // ShaderGraphで「Color」という参照名のとき
                    int id = Shader.PropertyToID("Color");
                    s.colorProp = id; s.baseColor = mat.GetColor(id);
                }

                // 明るさ系の代表的プロパティを片っ端から保持（存在するものだけ）
                TryAddFloatProp(ref s, ID_Intensity);
                TryAddFloatProp(ref s, ID_Brightness);
                TryAddFloatProp(ref s, ID_Fresnel);
                TryAddFloatProp(ref s, ID_Fresnel2);
                TryAddFloatProp(ref s, ID_Glow);
                TryAddFloatProp(ref s, ID_Emissive);
                TryAddFloatProp(ref s, ID_Opacity); // ShaderGraph側のOpacityがある場合

                vfxMatSlots.Add(s);
            }
        }
    }

    void TryAddFloatProp(ref MatSlot s, int prop)
    {
        if (s.mat.HasProperty(prop)) { s.floatProps.Add(prop); s.baseFloats.Add(s.mat.GetFloat(prop)); }
    }

    void SetVfxOpacityUnified(float o01)
    {
        float o = Mathf.Clamp01(o01);

        // 色RGB・アルファの両方を係数で落とす（どのタイプでも確実に暗くなる）
        for (int i = 0; i < vfxMatSlots.Count; i++)
        {
            var s = vfxMatSlots[i];

            if (s.colorProp != 0)
            {
                Color c = s.baseColor;
                c.r *= o; c.g *= o; c.b *= o;   // 明るさを落とす（Opaque/加算でも効く）
                c.a *= o;                      // 透明タイプのときはアルファでも薄くなる
                s.mat.SetColor(s.colorProp, c);
            }
            for (int j = 0; j < s.floatProps.Count; j++)
            {
                float v = s.baseFloats[j] * o; // Intensity/Fresnel等も係数で
                s.mat.SetFloat(s.floatProps[j], v);
            }
        }
    }

    // ---- COL（頂点カラー）を係数で落とす：Additive対策 ----
    void CacheColorOverLifetime()
    {
        vfxCOL.Clear();
        vfxCOLOriginal.Clear();
        foreach (var ps in vfxParticles)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            vfxCOL.Add(col);

            Gradient g = new Gradient();
            if (col.color.mode == ParticleSystemGradientMode.Gradient && col.color.gradient != null)
                g.SetKeys(col.color.gradient.colorKeys, col.color.gradient.alphaKeys);
            else
                g.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                    new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) }
                );
            vfxCOLOriginal.Add(g);
        }
    }

    void SetVfxOpacityByCOL(float o01)
    {
        float o = Mathf.Clamp01(o01);
        for (int i = 0; i < vfxCOL.Count; i++)
        {
            var col = vfxCOL[i];
            var g0 = vfxCOLOriginal[i];
            var a = g0.alphaKeys;
            for (int k = 0; k < a.Length; k++) a[k].alpha = a[k].alpha * o;
            Gradient g = new Gradient(); g.SetKeys(g0.colorKeys, a);
            col.color = new ParticleSystem.MinMaxGradient(g);
        }
    }

    void StopAndDestroyVfx()
    {
        if (!vfxInstance) return;
        foreach (var ps in vfxParticles)
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        Destroy(vfxInstance, 1.0f);
        vfxInstance = null;
        vfxParticles.Clear();
        vfxMatSlots.Clear();
        vfxCOL.Clear(); vfxCOLOriginal.Clear();
        if (vfxSpeedRoutine != null) { StopCoroutine(vfxSpeedRoutine); vfxSpeedRoutine = null; }
    }

    void KillVfxImmediate()
    {
        if (!vfxInstance) return;
        Destroy(vfxInstance);
        vfxInstance = null;
        vfxParticles.Clear();
        vfxMatSlots.Clear();
        vfxCOL.Clear(); vfxCOLOriginal.Clear();
        if (vfxSpeedRoutine != null) { StopCoroutine(vfxSpeedRoutine); vfxSpeedRoutine = null; }
    }

    // ========== SFX ==========
    void StopChargeLoop()
    {
        if (audioSource && audioSource.loop && audioSource.clip == sfxChargeLoop)
        { audioSource.loop = false; audioSource.Stop(); }
    }

    void OnValidate()
    {
        if (!armPivot) Debug.LogWarning("[Gun2DCharge] armPivot 未設定", this);
        if (!muzzle) Debug.LogWarning("[Gun2DCharge] muzzle 未設定", this);
        if (!normalBullet) Debug.LogWarning("[Gun2DCharge] normalBullet 未設定", this);
        if (!chargedBullet) Debug.LogWarning("[Gun2DCharge] chargedBullet 未設定", this);
        if (!bodyRb) Debug.LogWarning("[Gun2DCharge] bodyRb 未設定", this);
    }
}
