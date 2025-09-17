using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealthGuard : MonoBehaviour, IHittable
{
    [Header("HP")]
    [SerializeField] private int maxHP = 100;
    [SerializeField] private int hp = 100;

    [Header("Guard")]
    public InputActionReference guardAction;
    [SerializeField, Range(0f, 1f)] private float guardDamageMultiplier = 0.1f;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float stamina = 100f;
    [SerializeField] private float staminaRegenPerSec = 20f;
    [SerializeField] private float regenDelayAfterGuardHit = 0.25f;

    [Header("Hurt/Stun")]
    [SerializeField] private float defaultStunSeconds = 0.22f;
    [SerializeField] private Vector2 defaultKnockback = new(5.0f, 2.0f);
    [SerializeField] private float iFrameSeconds = 0.30f;

    [Header("Visual/Animator")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Animator bodyAnimator;
    [SerializeField] private string guardBoolParam = "Guard";
    [SerializeField] private string hurtBoolParam = "Hurt";
    [SerializeField] private string deadBoolParam = "Dead";   // �� �ǉ��F���S�J�ڗp

    [Header("Death Flow")]
    [SerializeField] private DeathUI deathUI;                 // YOU DIED �̉��o�i���̎菇�ō쐬�j
    [SerializeField] private float reloadDelayAfterText = 0.5f;

    public bool IsGuarding { get; private set; }
    public bool IsHurt { get; private set; }
    public bool IsDashing { get; private set; }  // �_�b�V���A�g�p
    public bool IsDead { get; private set; }  // �� �ǉ�

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
        if (IsDead) return;

        bool pressed = guardAction ? guardAction.action.IsPressed()
                                   : (Keyboard.current != null && Keyboard.current.gKey.isPressed);
        bool guardingNow = (!IsHurt) && pressed;
        if (guardingNow != IsGuarding)
        {
            IsGuarding = guardingNow;
            if (bodyAnimator) bodyAnimator.SetBool(guardBoolParam, IsGuarding);
        }

        if (Time.time >= _regenBlockedUntil && stamina < maxStamina)
            stamina = Mathf.Min(maxStamina, stamina + staminaRegenPerSec * Time.deltaTime);
    }

    // �O��API�F�X�^�~�i����
    public bool TrySpendStamina(float amount)
    {
        if (amount <= 0f) return true;
        if (stamina < amount) return false;
        stamina -= amount;
        return true;
    }
    // �O��API�F�_�b�V���̃I��/�I�t�i���G���t���j
    public void SetDashing(bool on, float iframeSeconds = 0f)
    {
        IsDashing = on;
        if (on && iframeSeconds > 0f)
            _iFrameUntil = Mathf.Max(_iFrameUntil, Time.time + iframeSeconds);
    }

    public void ReceiveHit(in HitInfo hit)
    {
        if (IsDead || IsHurt || IsDashing || Time.time < _iFrameUntil) return;

        if (IsGuarding)
        {
            int dmg = Mathf.CeilToInt(hit.damage * guardDamageMultiplier);
            stamina = Mathf.Max(0f, stamina - hit.guardStaminaCost);
            _regenBlockedUntil = Time.time + regenDelayAfterGuardHit;
            ApplyDamage(dmg);
            return;
        }

        ApplyDamage(hit.damage);
        if (hp <= 0) return;

        if (hit.causeStun)
        {
            float stun = (hit.stunSeconds > 0f) ? hit.stunSeconds : defaultStunSeconds;
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

        if (bodyAnimator) bodyAnimator.SetBool(hurtBoolParam, true);

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        rb.AddForce(knockbackImpulse, ForceMode2D.Impulse);

        yield return new WaitForSeconds(stunSeconds);

        IsHurt = false;
        if (bodyAnimator) bodyAnimator.SetBool(hurtBoolParam, false);
    }

    public void ApplyDamage(int amount)
    {
        if (IsDead) return;
        if (amount <= 0) return;

        hp = Mathf.Max(0, hp - amount);
        if (hp <= 0) Die();
    }

    void Die()
    {
        if (IsDead) return;
        IsDead = true;

        // ���ׂĂ̍s���t���O�𗎂Ƃ������G�Œ�
        IsGuarding = false;
        IsHurt = false;
        IsDashing = false;
        _iFrameUntil = float.PositiveInfinity;

        // Animator�FDead��
        if (bodyAnimator)
        {
            bodyAnimator.SetBool(guardBoolParam, false);
            bodyAnimator.SetBool(hurtBoolParam, false);
            bodyAnimator.SetBool(deadBoolParam, true); // AnyState��Down ��

            bodyAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            bodyAnimator.updateMode = AnimatorUpdateMode.UnscaledTime; // timeScale 0�ł�����
            bodyAnimator.CrossFadeInFixedTime("Down", 0f, 0, 0f);       // ���̃t���[�����o���Đ�
        }

        // YOU DIED �� �����[�h
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        if (deathUI) yield return deathUI.PlayAndReload(reloadDelayAfterText);
        else
        {
            // �t�H�[���o�b�N�F�����҂��ă����[�h
            yield return new WaitForSeconds(1.2f + reloadDelayAfterText);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
