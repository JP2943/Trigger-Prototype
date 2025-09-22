using UnityEngine;
using UnityEngine.Events;

public class BossStats : MonoBehaviour
{
    [Header("Boss Values")]
    public int maxHP = 10000;
    public int maxStamina = 1000;

    [ReadOnlyInInspector] public int hp;
    [ReadOnlyInInspector] public int stamina;

    [Header("Events (optional)")]
    public UnityEvent onBossDown;

    void Awake()
    {
        hp = Mathf.Max(1, maxHP);
        stamina = Mathf.Max(0, maxStamina);
    }

    public void ResetValues()
    {
        hp = Mathf.Max(1, maxHP);
        stamina = Mathf.Max(0, maxStamina);
    }

    public void ApplyHit(int hpDamage, int staminaDamage)
    {
        if (hp <= 0) return;
        hp = Mathf.Max(0, hp - Mathf.Max(0, hpDamage));
        stamina = Mathf.Clamp(stamina - Mathf.Max(0, staminaDamage), 0, maxStamina);
        if (hp <= 0) onBossDown?.Invoke();
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
