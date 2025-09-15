using System.Linq;
using UnityEngine;

/// �K�[�h/�_�b�V�����͗��r��SpriteRenderer�������\���ɂ���
/// �i���W�b�N�� StateMachineBehaviour ���� SetHiddenByGuard/SetHiddenByDash ���Ăԁj
public class GuardVisualSwitcher : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform rightArmRoot;   // ��: R_Shoulder
    [SerializeField] private Transform leftArmRoot;    // ��: L_Shoulder
    [SerializeField] private SpriteRenderer guardPoseSprite; // �C�Ӂi�g���Ă��Ȃ���΋��OK�j

    SpriteRenderer[] _rightSprites, _leftSprites;
    bool _hiddenByGuard;
    bool _hiddenByDash;
    bool _appliedHidden;

    void Reset()
    {
        if (!rightArmRoot) rightArmRoot = transform.Find("R_Shoulder");
        if (!leftArmRoot) leftArmRoot = transform.Find("L_Shoulder");
    }

    void Awake()
    {
        _rightSprites = rightArmRoot ? rightArmRoot.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0];
        _leftSprites = leftArmRoot ? leftArmRoot.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0];
        Apply(false); // �����͕\��
    }

    /// Guard �X�e�[�g�� Enter/Exit ����Ă�
    public void SetHiddenByGuard(bool on)
    {
        _hiddenByGuard = on;
        Apply(_hiddenByGuard || _hiddenByDash);
        // Guard�p�̈ꖇ�G���g���Ă���ꍇ����ON/OFF�i�g���Ă��Ȃ���Ζ��ݒ��OK�j
        if (guardPoseSprite) guardPoseSprite.enabled = on;
    }

    /// Dash �X�e�[�g�� Enter/Exit ����Ă�
    public void SetHiddenByDash(bool on)
    {
        _hiddenByDash = on;
        Apply(_hiddenByGuard || _hiddenByDash);
    }

    void Apply(bool hide)
    {
        if (hide == _appliedHidden) return;
        foreach (var sr in _rightSprites.Where(s => s)) sr.enabled = !hide;
        foreach (var sr in _leftSprites.Where(s => s)) sr.enabled = !hide;
        _appliedHidden = hide;
    }
}
