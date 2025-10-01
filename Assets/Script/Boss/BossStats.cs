using UnityEngine;
using UnityEngine.Events;

public class BossStats : MonoBehaviour
{
    [Header("Boss Values")]
    [Min(1)] public int maxHP = 10000;
    [Min(0)] public int maxStamina = 1000;

    [ReadOnlyInInspector] public int hp;
    [ReadOnlyInInspector] public int stamina;

    [Header("Events (optional)")]
    public UnityEvent onBossDown;
    [Tooltip("スタミナが0になった瞬間に発火（多重発火防止は内部で面倒を見ます）")]
    public UnityEvent onStaminaBreak;

    [Header("Stun Control")]
    [Tooltip("スタミナ0で Stun を開始するコントローラ（Boss 本体に付与）")]
    public BossStunController stunController;
    [Tooltip("スタミナ0で Stun を開始する（OFFにするとStunを発生させない）")]
    public bool stunOnStaminaZero = true;

    void Awake()
    {
        ResetValues();
    }

    public void ResetValues()
    {
        hp = Mathf.Max(1, maxHP);
        stamina = Mathf.Max(0, maxStamina);
    }

    /// <summary>
    /// HPとスタミナに同時にダメージを適用します。
    /// hpDamage: HPへのダメージ（負の値で回復可） / staminaDamage: スタミナへのダメージ（負の値で回復可）
    /// </summary>
    public void ApplyHit(int hpDamage, int staminaDamage)
    {
        if (hp <= 0) return;

        // HP
        if (hpDamage != 0)
        {
            hp = Mathf.Clamp(hp - Mathf.Max(0, hpDamage), 0, maxHP);
            if (hp <= 0)
            {
                onBossDown?.Invoke();
                // ダウン後の処理（演出など）は外部でハンドリング
            }
        }

        // STAMINA
        if (staminaDamage != 0)
        {
            stamina = Mathf.Clamp(stamina - Mathf.Max(0, staminaDamage), 0, maxStamina);

            // スタミナブレイク → Stun
            if (stamina == 0 && stunOnStaminaZero)
            {
                // 多重発火防止：既にStun中なら何もしない（延長したいなら BeginStun(追加秒) に切替）
                if (stunController != null && !stunController.IsStunned)
                {
                    onStaminaBreak?.Invoke();
                    stunController.BeginStun(); // 秒数は BossStunController 側の stunSeconds
                }
            }
        }
    }

    /// <summary>HPのみを変動させる（正で減少、負で回復）</summary>
    public void ApplyHP(int hpDamage)
    {
        ApplyHit(hpDamage, 0);
    }

    /// <summary>スタミナのみを変動させる（正で減少、負で回復）</summary>
    public void ApplyStamina(int staminaDamage)
    {
        ApplyHit(0, staminaDamage);
    }

    /// <summary>スタミナを即時に設定（UI/演出用）</summary>
    public void SetStamina(int value)
    {
        stamina = Mathf.Clamp(value, 0, maxStamina);
    }

    /// <summary>スタミナを最大に回復（Stun明けに呼ぶ場合など）</summary>
    public void RefillStamina()
    {
        stamina = maxStamina;
    }

    public float Hp01 => maxHP > 0 ? Mathf.Clamp01(hp / (float)maxHP) : 0f;
    public float Stamina01 => maxStamina > 0 ? Mathf.Clamp01(stamina / (float)maxStamina) : 0f;
}

// Inspector で読み取り専用表示にするための飾り（任意）
public class ReadOnlyInInspectorAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyInInspectorAttribute))]
public class ReadOnlyInInspectorDrawer : UnityEditor.PropertyDrawer
{
    public override void OnGUI(Rect pos, UnityEditor.SerializedProperty prop, GUIContent label)
    {
        bool was = GUI.enabled; GUI.enabled = false;
        UnityEditor.EditorGUI.PropertyField(pos, prop, label, true);
        GUI.enabled = was;
    }
}
#endif
