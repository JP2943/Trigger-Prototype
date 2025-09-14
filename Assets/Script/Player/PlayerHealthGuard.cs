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
    public InputActionReference guardAction; // Gameplay/Guard。未設定なら G キー
    [SerializeField, Range(0f, 1f)] private float guardDamageMultiplier = 0.1f;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float stamina = 100f;
    [SerializeField] private float staminaRegenPerSec = 20f;
    [SerializeField] private float regenDelayAfterGuardHit = 0.25f;

    [Header("Hurt/Stun")]
    [SerializeField, Tooltip("攻撃側が値を送ってこない場合の既定スタン秒")]
    private float defaultStunSeconds = 0.22f;
    [SerializeField, Tooltip("攻撃方向に応じた±で適用される横衝撃と、上向き衝撃")]
    private Vector2 defaultKnockback = new Vector2(5.0f, 2.0f);
    [SerializeField, Tooltip("被弾モーション中の無敵時間（連続被弾防止）")]
    private float iFrameSeconds = 0.30f;

    [Header("Visual (optional)")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Animator bodyAnimator;
    [SerializeField] private string guardBoolParam = "Guard";
    [SerializeField] private string hurtBoolParam = "Hurt"; // Animator の Hurt(bool)

    // 状態
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
        // 入力（未設定なら Keyboard.G）
        bool pressed = guardAction ? guardAction.action.IsPressed()
                                   : (Keyboard.current != null && Keyboard.current.gKey.isPressed);

        // 被弾中は行動不能＝ガード入力も無効化（見た目もOFF）
        bool guardingNow = (!IsHurt) && pressed;
        if (guardingNow != IsGuarding)
        {
            IsGuarding = guardingNow;
            if (bodyAnimator) bodyAnimator.SetBool(guardBoolParam, IsGuarding);
        }

        // スタミナ回復
        if (Time.time >= _regenBlockedUntil && stamina < maxStamina)
            stamina = Mathf.Min(maxStamina, stamina + staminaRegenPerSec * Time.deltaTime);
    }

    public void ReceiveHit(in HitInfo hit)
    {
        // 無敵中または既に被弾モーション中は無視
        if (IsHurt || Time.time < _iFrameUntil) return;

        if (IsGuarding)
        {
            int dmg = Mathf.CeilToInt(hit.damage * guardDamageMultiplier);
            stamina = Mathf.Max(0f, stamina - hit.guardStaminaCost);
            _regenBlockedUntil = Time.time + regenDelayAfterGuardHit;
            ApplyDamage(dmg);
            return; // ガード中はのけぞらない
        }

        // 非ガード：ダメージ適用
        ApplyDamage(hit.damage);

        // 死亡時の演出は今後実装
        if (hp <= 0) return;

        // のけぞり（攻撃ごとに可/不可）
        if (hit.causeStun)
        {
            float stun = (hit.stunSeconds > 0f) ? hit.stunSeconds : defaultStunSeconds;

            // ノックバック：攻撃→自分の方向(hitNormal)のX符号に合わせる
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

        // 見た目：Hurt ステートへ
        if (bodyAnimator) bodyAnimator.SetBool(hurtBoolParam, true);

        // ノックバック：横移動を止めてから衝撃
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        rb.AddForce(knockbackImpulse, ForceMode2D.Impulse);

        yield return new WaitForSeconds(stunSeconds);

        // 復帰
        IsHurt = false;
        if (bodyAnimator) bodyAnimator.SetBool(hurtBoolParam, false);
    }

    public void ApplyDamage(int amount)
    {
        if (amount <= 0) return;
        hp = Mathf.Max(0, hp - amount);
        if (hp <= 0)
        {
            // TODO: 死亡演出
            Debug.Log("[PlayerHealthGuard] Player died.");
        }
    }
}
