using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerHealthGuard : MonoBehaviour, IHittable
{
    [Header("HP")]
    [SerializeField] private int maxHP = 100;
    [SerializeField] private int hp = 100;

    [Header("Guard")]
    [Tooltip("�K�[�h�̓��́B���ݒ�Ȃ� G �L�[���t�H�[���o�b�N�Ŏg�p")]
    public InputActionReference guardAction;
    [SerializeField, Range(0f, 1f)] private float guardDamageMultiplier = 0.1f; // 1/10

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float stamina = 100f;
    [SerializeField, Tooltip("���b�񕜗�")] private float staminaRegenPerSec = 20f;
    [SerializeField, Tooltip("��e�ケ�̕b���͉񕜒�~")] private float regenDelayAfterGuardHit = 0.25f;

    [Header("Visual (optional)")]
    [SerializeField] private SpriteRenderer bodyRenderer;    // �����ڂ�؂�ւ���ꍇ
    [SerializeField] private Sprite guardSprite;             // �K�[�h�p���̃X�v���C�g�i�C�Ӂj

    [Header("Animator (optional)")]
    [SerializeField] private Animator bodyAnimator;          // ������Animator������Ȃ犄��
    [SerializeField] private string guardBoolParam = "Guard";// Animator�̃t���O���i����ꍇ�j

    float _regenBlockedUntil;
    bool _guarding;
    Sprite _cachedSprite;

    public bool IsGuarding => _guarding;
    public float HP01 => Mathf.Clamp01(hp / (float)maxHP);
    public float Stamina01 => Mathf.Clamp01(stamina / maxStamina);

    void Awake()
    {
        if (!bodyRenderer) bodyRenderer = GetComponentInChildren<SpriteRenderer>();
        if (!bodyAnimator) bodyAnimator = GetComponentInChildren<Animator>();
        if (bodyRenderer) _cachedSprite = bodyRenderer.sprite;
        hp = Mathf.Clamp(hp, 0, maxHP);
        stamina = Mathf.Clamp(stamina, 0, maxStamina);
    }
    void OnEnable() { guardAction?.action.Enable(); }
    void OnDisable() { guardAction?.action.Disable(); }

    void Update()
    {
        // ���́FInputAction ��������� G �L�[�ő�p
        bool pressed = guardAction ? guardAction.action.IsPressed()
                                   : (Keyboard.current != null && Keyboard.current.tKey.isPressed);

        // ��ԕω����Ɍ����ڂ�ؑցi�K�v�ȏꍇ�j
        if (pressed != _guarding)
        {
            _guarding = pressed;
            if (bodyAnimator) bodyAnimator.SetBool(guardBoolParam, _guarding);
            if (guardSprite && bodyRenderer)
            {
                if (_guarding) _cachedSprite = bodyRenderer.sprite;
                bodyRenderer.sprite = _guarding ? guardSprite : _cachedSprite;
            }
        }

        // �X�^�~�i�񕜁i��e�サ�΂炭��~�j
        bool canRegen = Time.time >= _regenBlockedUntil;
        if (canRegen && stamina < maxStamina)
        {
            stamina = Mathf.Min(maxStamina, stamina + staminaRegenPerSec * Time.deltaTime);
        }
    }

    // �U�����󂯂�iIHittable�j
    public void ReceiveHit(in HitInfo hit)
    {
        int damageToApply = hit.damage;

        if (_guarding)
        {
            // �_���[�W�y��
            damageToApply = Mathf.CeilToInt(hit.damage * guardDamageMultiplier);
            // �X�^�~�i����i�U�����ƂɃR�X�g�ρj
            stamina = Mathf.Max(0f, stamina - hit.guardStaminaCost);
            _regenBlockedUntil = Time.time + regenDelayAfterGuardHit;
        }

        ApplyDamage(damageToApply);
        // �����Ńq�b�g�G�t�F�N�g/SE/�q�b�g�X�g�b�v����炵�Ă�OK�ihit.hitPoint �Q�Ɖj
    }

    public void ApplyDamage(int amount)
    {
        if (amount <= 0) return;
        hp = Mathf.Max(0, hp - amount);
        if (hp <= 0)
        {
            // ���S�����͌�Ŏ����\��F�A�j���Đ�����͖�������
            Debug.Log("[PlayerHealthGuard] Player died.");
        }
    }

    // UI������Ăׂ��API�i�C�Ӂj
    public void Heal(int amount) => hp = Mathf.Min(maxHP, hp + Mathf.Max(0, amount));
}
