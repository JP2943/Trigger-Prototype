using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealthGuard : MonoBehaviour, IHittable
{
    [Header("HP")]
    [SerializeField] private int maxHP = 100;
    [SerializeField] private int hp = 100;

    [Header("Guard")]
    public InputActionReference guardAction; // Gameplay/Guard�B���ݒ�Ȃ� G �L�[
    [SerializeField, Range(0f, 1f)] private float guardDamageMultiplier = 0.1f;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float stamina = 100f;
    [SerializeField] private float staminaRegenPerSec = 20f;
    [SerializeField] private float regenDelayAfterGuardHit = 0.25f;

    [Header("Hurt/Stun")]
    [SerializeField, Tooltip("�U�������l�𑗂��Ă��Ȃ��ꍇ�̊���X�^���b")]
    private float defaultStunSeconds = 0.22f;
    [SerializeField, Tooltip("�U�������ɉ������}�œK�p����鉡�Ռ��ƁA������Ռ�")]
    private Vector2 defaultKnockback = new Vector2(5.0f, 2.0f);
    [SerializeField, Tooltip("��e���[�V�������̖��G���ԁi�A����e�h�~�j")]
    private float iFrameSeconds = 0.30f;

    [Header("Visual (optional)")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Animator bodyAnimator;
    [SerializeField] private string guardBoolParam = "Guard";
    [SerializeField] private string hurtBoolParam = "Hurt"; // Animator �� Hurt(bool)

    // ���
    public bool IsGuarding { get; private set; }
    public bool IsHurt { get; private set; }
    public float HP01 => Mathf.Clamp01(hp / (float)maxHP);
    public float Stamina01 => Mathf.Clamp01(stamina / maxStamina);

    Rigidbody2D rb;
    float _regenBlockedUntil;
    float _iFrameUntil;
    Sprite _cachedSprite;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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
        // ���́i���ݒ�Ȃ� Keyboard.G�j
        bool pressed = guardAction ? guardAction.action.IsPressed()
                                   : (Keyboard.current != null && Keyboard.current.gKey.isPressed);

        // ��e���͍s���s�\���K�[�h���͂��������i�����ڂ�OFF�j
        bool guardingNow = (!IsHurt) && pressed;
        if (guardingNow != IsGuarding)
        {
            IsGuarding = guardingNow;
            if (bodyAnimator) bodyAnimator.SetBool(guardBoolParam, IsGuarding);
        }

        // �X�^�~�i��
        if (Time.time >= _regenBlockedUntil && stamina < maxStamina)
            stamina = Mathf.Min(maxStamina, stamina + staminaRegenPerSec * Time.deltaTime);
    }

    public void ReceiveHit(in HitInfo hit)
    {
        // ���G���܂��͊��ɔ�e���[�V�������͖���
        if (IsHurt || Time.time < _iFrameUntil) return;

        if (IsGuarding)
        {
            int dmg = Mathf.CeilToInt(hit.damage * guardDamageMultiplier);
            stamina = Mathf.Max(0f, stamina - hit.guardStaminaCost);
            _regenBlockedUntil = Time.time + regenDelayAfterGuardHit;
            ApplyDamage(dmg);
            return; // �K�[�h���͂̂�����Ȃ�
        }

        // ��K�[�h�F�_���[�W�K�p
        ApplyDamage(hit.damage);

        // ���S���̉��o�͍������
        if (hp <= 0) return;

        // �̂�����i�U�����Ƃɉ�/�s�j
        if (hit.causeStun)
        {
            float stun = (hit.stunSeconds > 0f) ? hit.stunSeconds : defaultStunSeconds;

            // �m�b�N�o�b�N�F�U���������̕���(hitNormal)��X�����ɍ��킹��
            float sgn = Mathf.Sign(hit.hitNormal.x == 0 ? 1 : hit.hitNormal.x);
            Vector2 kb = new Vector2(
                (hit.knockback.x != 0 ? hit.knockback.x : defaultKnockback.x) * sgn,
                (hit.knockback.y != 0 ? hit.knockback.y : defaultKnockback.y)
            );

            StartCoroutine(HurtRoutine(stun, kb));
        }
    }

    IEnumerator HurtRoutine(float stunSeconds, Vector2 knockbackImpulse)
    {
        IsHurt = true;
        _iFrameUntil = Time.time + iFrameSeconds;

        // �����ځFHurt �X�e�[�g��
        if (bodyAnimator) bodyAnimator.SetBool(hurtBoolParam, true);

        // �m�b�N�o�b�N�F���ړ����~�߂Ă���Ռ�
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        rb.AddForce(knockbackImpulse, ForceMode2D.Impulse);

        yield return new WaitForSeconds(stunSeconds);

        // ���A
        IsHurt = false;
        if (bodyAnimator) bodyAnimator.SetBool(hurtBoolParam, false);
    }

    public void ApplyDamage(int amount)
    {
        if (amount <= 0) return;
        hp = Mathf.Max(0, hp - amount);
        if (hp <= 0)
        {
            // TODO: ���S���o
            Debug.Log("[PlayerHealthGuard] Player died.");
        }
    }
}
