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
    [Tooltip("�X�^�~�i��0�ɂȂ����u�Ԃɔ��΁i���d���Ζh�~�͓����Ŗʓ|�����܂��j")]
    public UnityEvent onStaminaBreak;

    [Header("Stun Control")]
    [Tooltip("�X�^�~�i0�� Stun ���J�n����R���g���[���iBoss �{�̂ɕt�^�j")]
    public BossStunController stunController;
    [Tooltip("�X�^�~�i0�� Stun ���J�n����iOFF�ɂ����Stun�𔭐������Ȃ��j")]
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
    /// HP�ƃX�^�~�i�ɓ����Ƀ_���[�W��K�p���܂��B
    /// hpDamage: HP�ւ̃_���[�W�i���̒l�ŉ񕜉j / staminaDamage: �X�^�~�i�ւ̃_���[�W�i���̒l�ŉ񕜉j
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
                // �_�E����̏����i���o�Ȃǁj�͊O���Ńn���h�����O
            }
        }

        // STAMINA
        if (staminaDamage != 0)
        {
            stamina = Mathf.Clamp(stamina - Mathf.Max(0, staminaDamage), 0, maxStamina);

            // �X�^�~�i�u���C�N �� Stun
            if (stamina == 0 && stunOnStaminaZero)
            {
                // ���d���Ζh�~�F����Stun���Ȃ牽�����Ȃ��i�����������Ȃ� BeginStun(�ǉ��b) �ɐؑցj
                if (stunController != null && !stunController.IsStunned)
                {
                    onStaminaBreak?.Invoke();
                    stunController.BeginStun(); // �b���� BossStunController ���� stunSeconds
                }
            }
        }
    }

    /// <summary>HP�݂̂�ϓ�������i���Ō����A���ŉ񕜁j</summary>
    public void ApplyHP(int hpDamage)
    {
        ApplyHit(hpDamage, 0);
    }

    /// <summary>�X�^�~�i�݂̂�ϓ�������i���Ō����A���ŉ񕜁j</summary>
    public void ApplyStamina(int staminaDamage)
    {
        ApplyHit(0, staminaDamage);
    }

    /// <summary>�X�^�~�i�𑦎��ɐݒ�iUI/���o�p�j</summary>
    public void SetStamina(int value)
    {
        stamina = Mathf.Clamp(value, 0, maxStamina);
    }

    /// <summary>�X�^�~�i���ő�ɉ񕜁iStun�����ɌĂԏꍇ�Ȃǁj</summary>
    public void RefillStamina()
    {
        stamina = maxStamina;
    }

    public float Hp01 => maxHP > 0 ? Mathf.Clamp01(hp / (float)maxHP) : 0f;
    public float Stamina01 => maxStamina > 0 ? Mathf.Clamp01(stamina / (float)maxStamina) : 0f;
}

// Inspector �œǂݎ���p�\���ɂ��邽�߂̏���i�C�Ӂj
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
