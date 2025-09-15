using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements.Experimental;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class BodyLocomotion2D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Animator bodyAnimator;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new(0.28f, 0.06f);
    [SerializeField] private LayerMask groundMask;

    [Header("Blockers")]
    [SerializeField] private PlayerHealthGuard guardRef;
    [SerializeField] private float guardStopDecel = 40f;
    [SerializeField] private float hurtSlideDecel = 6f;

    [Header("Move/Jump")]
    [SerializeField] private float moveSpeed = 2.8f;
    [SerializeField] private float jumpForce = 6.0f;

    [Header("Input Actions")]
    public InputActionReference moveAction;  // Gameplay/Move
    public InputActionReference jumpAction;  // Gameplay/Jump

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int AirborneHash = Animator.StringToHash("Airborne");

    [SerializeField] float recoilReturn = 8f;
    float recoilX;                             // ノックバックの一時速度

    Rigidbody2D rb;
    float inputX;
    int lastDir = 1;
    bool grounded, wantJump;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
    }
    void OnEnable()
    {
        moveAction?.action.Enable();
        jumpAction?.action.Enable();
    }
    void OnDisable()
    {
        moveAction?.action.Disable();
        jumpAction?.action.Disable();
    }

    void Update()
    {
        bool guarding = guardRef && guardRef.IsGuarding;
        bool hurt = guardRef && guardRef.IsHurt;
        bool dashing = guardRef && guardRef.IsDashing;

        // 入力（ダッシュ/ガード/被弾中は0固定）
        float x = (moveAction && !guarding && !hurt && !dashing) ? moveAction.action.ReadValue<float>() : 0f;
        inputX = Mathf.Abs(x) < 0.1f ? 0f : Mathf.Sign(x);

        if (inputX != 0) lastDir = (int)Mathf.Sign(inputX);
        if (bodyRenderer) bodyRenderer.flipX = (lastDir < 0);

        grounded = IsGrounded();
        bodyAnimator.SetBool(AirborneHash, !grounded);
        bodyAnimator.SetFloat(SpeedHash, (guarding || hurt || dashing) ? 0f : Mathf.Abs(inputX));

        if (!guarding && !hurt && !dashing && jumpAction && jumpAction.action.WasPressedThisFrame())
            wantJump = true;
    }

    // ノックバック付与（右向きに撃ったとき backDir は -armPivot.right）
    public void AddRecoil(Vector2 backDir, float kickSpeed)
    {
        recoilX += backDir.x * kickSpeed; // 水平成分だけ足す（単位：速度）
    }

    void FixedUpdate()
    {
        recoilX = Mathf.MoveTowards(recoilX, 0f, recoilReturn * Time.fixedDeltaTime);
        Vector2 v = rb.linearVelocity;

        bool guarding = guardRef && guardRef.IsGuarding;
        bool hurt = guardRef && guardRef.IsHurt;
        bool dashing = guardRef && guardRef.IsDashing; // ← ここで再計算（修正点）

        if (guarding)
        {
            // ガード中：X速度を素早く0へ収束（重力はそのまま、外部の縦方向は維持）
            v.x = Mathf.MoveTowards(v.x, 0f, guardStopDecel * Time.fixedDeltaTime);
        }
        else if (hurt)
        {
            // 被弾中：AddForceで付けたノックバックを維持しつつ、ゆっくり減衰
            v.x = Mathf.MoveTowards(v.x, 0f, hurtSlideDecel * Time.fixedDeltaTime);
        }
        else if (dashing)
        {
            // ダッシュ中：速度は PlayerDash 側で管理。ここでは上書きしない。
        }
        else
        {
            v.x = inputX * moveSpeed + recoilX;
        }
        rb.linearVelocity = v;

        if (!guarding && !hurt && !dashing && wantJump && grounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
        wantJump = false;
    }

    bool IsGrounded()
    {
        if (!groundCheck) return false;
        return Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundMask) != null;
    }
    void OnDrawGizmosSelected()
    {
        if (!groundCheck) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
    }
}
