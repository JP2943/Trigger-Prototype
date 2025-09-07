using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class BodyLocomotion2D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SpriteRenderer bodyRenderer; // Body の SR
    [SerializeField] private Animator bodyAnimator;       // Idle/Walk/Jump を持つ
    [SerializeField] private Transform groundCheck;       // 足元の空オブジェクト
    [SerializeField] private Vector2 groundCheckSize = new(0.28f, 0.06f);
    [SerializeField] private LayerMask groundMask;        // Ground レイヤー

    [Header("Move/Jump")]
    [SerializeField] private float moveSpeed = 2.8f;
    [SerializeField] private float jumpForce = 6.0f;      // 2Dの重力Scale=3〜5を想定

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int AirborneHash = Animator.StringToHash("Airborne");

    Rigidbody2D rb;
    float inputX;
    int lastDir = 1;
    bool wantJump;    // スペースが押されたフラグ
    bool grounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;           // 転倒防止
    }

    void Update()
    {
        // --- 横入力 ---
        var kb = Keyboard.current;
        int x = 0;
        if (kb != null)
        {
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) x -= 1;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) x += 1;
            if (kb.spaceKey.wasPressedThisFrame) wantJump = true; // ジャンプ要求
        }
        inputX = x;

        if (x != 0) lastDir = x;
        if (bodyRenderer) bodyRenderer.flipX = (lastDir < 0);

        // --- 接地判定（毎フレーム） ---
        grounded = IsGrounded();
        bodyAnimator.SetBool(AirborneHash, !grounded);

        // --- 歩行アニメ速度 ---
        bodyAnimator.SetFloat(SpeedHash, Mathf.Abs(inputX));
    }

    void FixedUpdate()
    {
        // --- 横移動 ---
        Vector2 v = rb.linearVelocity;
        v.x = inputX * moveSpeed;
        rb.linearVelocity = v;

        // --- ジャンプ実行（接地中だけ） ---
        if (wantJump && grounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);                // 上向き速度をリセット
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);    // インパルス
        }
        wantJump = false;
    }

    bool IsGrounded()
    {
        if (!groundCheck) return false;
        Collider2D hit = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundMask);
        return hit != null;
    }

    // 足元の検出範囲を可視化
    void OnDrawGizmosSelected()
    {
        if (!groundCheck) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(groundCheck.position, (Vector3)groundCheckSize);
    }
}
