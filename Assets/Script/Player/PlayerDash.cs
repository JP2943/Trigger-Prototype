using UnityEngine;
using UnityEngine.InputSystem;

/// Xボタンでダッシュ。無敵+iLayerすり抜け+クールダウン。
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDash : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerHealthGuard guardRef;        // HP/ガード管理
    [SerializeField] private SpriteRenderer facingSprite;       // 向き判定用（BodyのSR）
    [SerializeField] private Animator bodyAnimator;             // 見た目切替
    [SerializeField] private InputActionReference dashAction;   // Gameplay/Dash（Gamepad/x）

    [Header("Dash Params")]
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float staminaCost = 25f;
    [SerializeField] private float cooldown = 0.6f;
    [SerializeField] private string dashBoolParam = "Dash";

    [Header("Bypass Layers")]
    [SerializeField] private string enemyLayerName = "Enemy";
    [SerializeField] private string enemyAttackLayerName = "EnemyAttack";

    Rigidbody2D rb;
    float dirX = 1f;
    float dashEndTime;
    float nextDashTime;
    int playerLayer, enemyLayer, enemyAttackLayer;
    bool active;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!guardRef) guardRef = GetComponent<PlayerHealthGuard>();
        if (!bodyAnimator) bodyAnimator = GetComponentInChildren<Animator>();
        if (!facingSprite) facingSprite = GetComponentInChildren<SpriteRenderer>();
        playerLayer = gameObject.layer;
        enemyLayer = LayerMask.NameToLayer(enemyLayerName);
        enemyAttackLayer = LayerMask.NameToLayer(enemyAttackLayerName);
    }

    void OnEnable() { dashAction?.action.Enable(); }
    void OnDisable()
    {
        dashAction?.action.Disable();
        if (active) StopDash(); // クリーンアップ保険
    }

    void Update()
    {
        if (active)
        {
            if (Time.time >= dashEndTime) StopDash();
            return;
        }

        // 入力（Gamepad X）。キーボード確認用: LeftShift を追加
        bool pressed = dashAction && dashAction.action.WasPressedThisFrame();
#if UNITY_EDITOR
        if (!pressed && Keyboard.current != null)
            pressed = Keyboard.current.leftShiftKey.wasPressedThisFrame;
#endif
        if (pressed) TryStartDash();
    }

    void FixedUpdate()
    {
        if (active)
        {
            var v = rb.linearVelocity;
            v.x = dirX * dashSpeed; // 毎Fixedで上書きして一定速度を維持
            rb.linearVelocity = v;
        }
    }

    void TryStartDash()
    {
        if (Time.time < nextDashTime) return;
        if (!guardRef) return;
        if (guardRef.IsHurt || guardRef.IsGuarding) return; // のけぞり/ガード中は不可
        if (!guardRef.TrySpendStamina(staminaCost)) return; // スタミナ不足

        dirX = (facingSprite && facingSprite.flipX) ? -1f : 1f;

        // 無敵+iFrame延長＆「ダッシュ中」フラグ（BodyLocomotionから参照）
        guardRef.SetDashing(true, dashDuration);

        // すり抜け（Player×Enemy/EnemyAttack を無効化）
        if (enemyLayer >= 0) Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
        if (enemyAttackLayer >= 0) Physics2D.IgnoreLayerCollision(playerLayer, enemyAttackLayer, true);

        // アニメ
        if (bodyAnimator) bodyAnimator.SetBool(dashBoolParam, true);

        active = true;
        dashEndTime = Time.time + dashDuration;
        nextDashTime = dashEndTime + cooldown;
    }

    void StopDash()
    {
        active = false;
        guardRef.SetDashing(false, 0f);

        if (bodyAnimator) bodyAnimator.SetBool(dashBoolParam, false);

        // 衝突を元に戻す
        if (enemyLayer >= 0) Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
        if (enemyAttackLayer >= 0) Physics2D.IgnoreLayerCollision(playerLayer, enemyAttackLayer, false);
    }
}
