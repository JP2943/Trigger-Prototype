using UnityEngine;
using UnityEngine.InputSystem; // New Input System �p

/// <summary>
/// Play���[�h���� P �L�[�� BossGorilla �̍U�����Đ����邾���̃f�o�b�O�p�g���K�[�B
/// �V�[������ Empty �ɕt���Ďg���܂��B
/// </summary>
public class AttackTestTrigger : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private BossGorillaAttack target; // BossGorilla ���[�g�ɕt���Ă���R���|�[�l���g������

    [Header("Hotkey")]
    [SerializeField] private bool editorOnly = false;   // �G�f�B�^�̂ݗL���ɂ������ꍇ
    [SerializeField] private bool logFire = true;       // ���������Ƀ��O���o��

    void Awake()
    {
        // �������Ȃ玩���ŃV�[��������T���i1�̂����z��j
        if (!target) target = FindObjectOfType<BossGorillaAttack>();
    }

    void Update()
    {
#if UNITY_EDITOR
        if (editorOnly && !Application.isEditor) return;
#endif
        // New Input System�i�����j
        var kbd = Keyboard.current;
        if (kbd != null && kbd.pKey.wasPressedThisFrame)
        {
            if (target)
            {
                target.DoAttack();
                if (logFire) Debug.Log("[AttackTestTrigger] P pressed -> DoAttack()");
            }
            else
            {
                Debug.LogWarning("[AttackTestTrigger] target ���������ł��BBossGorillaAttack ���A�T�C�����Ă��������B", this);
            }
        }

        // �\���̋�API�i�v���W�F�N�g��Both�ݒ�̏ꍇ�̂ݔ����j
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (target) target.DoAttack();
        }
    }

    void OnValidate()
    {
        if (!target)
            Debug.LogWarning("[AttackTestTrigger] target ������", this);
    }
}
