using System.Linq;
using UnityEngine;

public class GuardVisualSwitcher : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerHealthGuard playerGuard; // �v���C���[�̃K�[�h��Ԃ��Q��
    [SerializeField] private Transform rightArmRoot;        // ��: R_Shoulder
    [SerializeField] private Transform leftArmRoot;         // ��: L_Shoulder
    [SerializeField] private SpriteRenderer guardPoseSprite;// guard�p�̈ꖇ�G(�C��)�BAnimator�Őؑ֍ς݂Ȃ���OK

    // ����
    SpriteRenderer[] _rightSprites, _leftSprites;
    bool _applied;

    void Reset()
    {
        if (!playerGuard) playerGuard = GetComponent<PlayerHealthGuard>();
        if (!rightArmRoot) rightArmRoot = transform.Find("R_Shoulder");
        if (!leftArmRoot) leftArmRoot = transform.Find("L_Shoulder");
    }

    void Awake()
    {
        if (!playerGuard) playerGuard = GetComponent<PlayerHealthGuard>();
        _rightSprites = rightArmRoot ? rightArmRoot.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0];
        _leftSprites = leftArmRoot ? leftArmRoot.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0];
        Apply(false); // �����͔�K�[�h�z��
    }

    public void SetGuardVisual(bool guarding) => Apply(guarding);

    void Apply(bool guarding)
    {
        // �r�̌����ڂ�����\���i�X�N���v�g��R���C�_�[�͐������j
        foreach (var sr in _rightSprites.Where(s => s)) sr.enabled = !guarding;
        foreach (var sr in _leftSprites.Where(s => s)) sr.enabled = !guarding;

        // Guard�p�ꖇ�G�i�g���Ȃ�ON/OFF�j
        if (guardPoseSprite) guardPoseSprite.enabled = guarding;

        _applied = guarding;
    }
}
