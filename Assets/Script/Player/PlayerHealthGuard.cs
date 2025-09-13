using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerHealthGuard : MonoBehaviour, IHittable
{
    [Header("HP")]
    [SerializeField] private int maxHP = 100;
    [SerializeField] private int hp = 100;

    [Header("Guard")]
    [Tooltip("ガードの入力。未設定なら G キーをフォールバックで使用")]
    public InputActionReference guardAction;
    [SerializeField, Range(0f, 1f)] private float guardDamageMultiplier = 0.1f; // 1/10

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float stamina = 100f;
    [SerializeField, Tooltip("毎秒回復量")] private float staminaRegenPerSec = 20f;
    [SerializeField, Tooltip("被弾後この秒数は回復停止")] private float regenDelayAfterGuardHit = 0.25f;

    [Header("Visual (optional)")]
    [SerializeField] private SpriteRenderer bodyRenderer;    // 見た目を切り替える場合
    [SerializeField] private Sprite guardSprite;             // ガード姿勢のスプライト（任意）

    [Header("Animator (optional)")]
    [SerializeField] private Animator bodyAnimator;          // 既存のAnimatorがあるなら割当
    [SerializeField] private string guardBoolParam = "Guard";// Animatorのフラグ名（ある場合）

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
        // 入力：InputAction が無ければ G キーで代用
        bool pressed = guardAction ? guardAction.action.IsPressed()
                                   : (Keyboard.current != null && Keyboard.current.tKey.isPressed);

        // 状態変化時に見た目を切替（必要な場合）
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

        // スタミナ回復（被弾後しばらく停止）
        bool canRegen = Time.time >= _regenBlockedUntil;
        if (canRegen && stamina < maxStamina)
        {
            stamina = Mathf.Min(maxStamina, stamina + staminaRegenPerSec * Time.deltaTime);
        }
    }

    // 攻撃を受ける（IHittable）
    public void ReceiveHit(in HitInfo hit)
    {
        int damageToApply = hit.damage;

        if (_guarding)
        {
            // ダメージ軽減
            damageToApply = Mathf.CeilToInt(hit.damage * guardDamageMultiplier);
            // スタミナ消費（攻撃ごとにコスト可変）
            stamina = Mathf.Max(0f, stamina - hit.guardStaminaCost);
            _regenBlockedUntil = Time.time + regenDelayAfterGuardHit;
        }

        ApplyDamage(damageToApply);
        // ここでヒットエフェクト/SE/ヒットストップ等を鳴らしてもOK（hit.hitPoint 参照可）
    }

    public void ApplyDamage(int amount)
    {
        if (amount <= 0) return;
        hp = Mathf.Max(0, hp - amount);
        if (hp <= 0)
        {
            // 死亡処理は後で実装予定：アニメ再生や入力無効化等
            Debug.Log("[PlayerHealthGuard] Player died.");
        }
    }

    // UI等から呼べる回復API（任意）
    public void Heal(int amount) => hp = Mathf.Min(maxHP, hp + Mathf.Max(0, amount));
}
