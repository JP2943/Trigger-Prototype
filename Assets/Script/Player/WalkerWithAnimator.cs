using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class WalkerWithAnimator : MonoBehaviour
{
    [SerializeField] float moveSpeed = 2.5f;

    Animator animator;
    Rigidbody2D rb;
    SpriteRenderer sr;

    static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    float inputX;
    int lastDir = 1; // ★ +1=右, -1=左（最後に動いた向き）

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        rb.freezeRotation = true;
    }

    void Update()
    {
        var kb = Keyboard.current;
        int x = 0;
        if (kb != null)
        {
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) x -= 1;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) x += 1;
        }
        inputX = x;

        if (x != 0) lastDir = x;     // ★ 入力がある時だけ更新（Idleでは保持）
        sr.flipX = (lastDir < 0);     // ★ 毎フレーム、最後の向きを適用
        animator.SetBool(IsWalkingHash, x != 0);
    }

    void FixedUpdate()
    {
        Vector2 v = rb.linearVelocity;
        v.x = inputX * moveSpeed;
        rb.linearVelocity = v;
    }
}