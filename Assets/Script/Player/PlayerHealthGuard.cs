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
    [SerializeField] private string deadBoolParam = "Dead";

    [Header("Death Flow")]
    [SerializeField] private DeathUI deathUI;
    [SerializeField] private float reloadDelayAfterText = 0.5f;

    [Header("Just Guard")]
    [SerializeField] private JustGuardHandler justGuardHandler; // 同一オブジェクト推奨

    public bool IsGuarding { get; private set; }
    public bool IsHurt { get; private set; }
    public bool IsDashing { get; private set; }
    public bool IsDead { get; private set; }

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

        if (!justGuardHandler) justGuardHandler = GetComponent<JustGuardHandler>();
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

    // 外部API：スタミナ消費
    public bool TrySpendStamina(float amount)
    {
        if (amount <= 0f) return true;
        if (stamina < amount) return false;
        stamina -= amount;
        return true;
    }

    // 外部API：ダッシュのオン/オフ（無敵も付加）
    public void SetDashing(bool on, float iframeSeconds = 0f)
    {
        IsDashing = on;
        if (on && iframeSeconds > 0f)
            _iFrameUntil = Mathf.Max(_iFrameUntil, Time.time + iframeSeconds);
    }

    // ---- ヒット受付（攻撃側の HitInfo 仕様に依存しないため、optional フラグは反射で読む） ----
    public void ReceiveHit(in HitInfo hit)
    {
        if (IsDead || IsHurt || IsDashing || Time.time < _iFrameUntil) return;

        // まず「このヒットで想定される値」をローカルに計算
        int hpDamage = hit.damage; // HitInfo は既存通り int を想定
        int staminaCost = Mathf.Max(0, Mathf.RoundToInt(hit.guardStaminaCost));

        // ノックバック（符号は当たり方向で決定）
        float sgn = Mathf.Sign(hit.hitNormal.x == 0 ? 1 : hit.hitNormal.x);
        Vector2 wouldKnockback = new Vector2(
            (hit.knockback.x != 0 ? hit.knockback.x : defaultKnockback.x) * sgn,
            (hit.knockback.y != 0 ? hit.knockback.y : defaultKnockback.y)
        );

        // --- 可変フラグ（あれば読む・なければ既定） ---
        bool unblockable = GetOptionalBoolFlag(hit, "unblockable", false);
        bool justGuardable = GetOptionalBoolFlag(hit, "justGuardable", true);

        // ★ ジャストガード（押下から5F以内）—— justGuardable の時のみ成立
        if (justGuardable && justGuardHandler != null &&
            justGuardHandler.TryApplyJustGuard(ref hpDamage, ref staminaCost, ref wouldKnockback))
        {
            // 完全防御：HP/スタミナ/ノックバックすべて 0。以降の処理は何もせず終了。
            return;
        }

        // ここから通常のガード/被弾処理
        if (IsGuarding)
        {
            if (unblockable)
            {
                // ガード不能：通常被弾扱い（軽減しない）
                ApplyDamage(hpDamage);
                if (hp <= 0) return;

                if (hit.causeStun)
                {
                    float stun = (hit.stunSeconds > 0f) ? hit.stunSeconds : defaultStunSeconds;
                    StartCoroutine(HurtRoutine(stun, wouldKnockback));
                }
                return;
            }
            else
            {
                // 通常ガード：ダメージ軽減＋スタミナ消費（ノックバックは軽減していない設計）
                int reduced = Mathf.CeilToInt(hpDamage * guardDamageMultiplier);
                stamina = Mathf.Max(0f, stamina - staminaCost);
                _regenBlockedUntil = Time.time + regenDelayAfterGuardHit;
                ApplyDamage(reduced);
                return;
            }
        }

        // 非ガード時
        ApplyDamage(hpDamage);
        if (hp <= 0) return;

        if (hit.causeStun)
        {
            float stun = (hit.stunSeconds > 0f) ? hit.stunSeconds : defaultStunSeconds;
            StartCoroutine(HurtRoutine(stun, wouldKnockback));
        }
    }

    // optional フラグを構造体/クラスから安全に読む（なければ defaultVal）
    static bool GetOptionalBoolFlag<T>(in T hit, string name, bool defaultVal)
    {
        object boxed = hit; // in 引数でも box すれば反射で読める
        var ty = boxed.GetType();
        // フィールド優先
        var f = ty.GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(bool)) return (bool)f.GetValue(boxed);
        // プロパティでも可
        var p = ty.GetProperty(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (p != null && p.PropertyType == typeof(bool)) return (bool)p.GetValue(boxed);
        return defaultVal;
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

        // すべての行動フラグを落とす＆無敵固定
        IsGuarding = false;
        IsHurt = false;
        IsDashing = false;
        _iFrameUntil = float.PositiveInfinity;

        // Animator：Deadへ
        if (bodyAnimator)
        {
            bodyAnimator.SetBool(guardBoolParam, false);
            bodyAnimator.SetBool(hurtBoolParam, false);
            bodyAnimator.SetBool(deadBoolParam, true); // AnyState→Down へ

            bodyAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            bodyAnimator.updateMode = AnimatorUpdateMode.UnscaledTime; // timeScale 0でも動く
            bodyAnimator.CrossFadeInFixedTime("Down", 0f, 0, 0f);       // そのフレーム頭出し
        }

        // YOU DIED → リロード
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        if (deathUI) yield return deathUI.PlayAndReload(reloadDelayAfterText);
        else
        {
            yield return new WaitForSeconds(1.2f + reloadDelayAfterText);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
