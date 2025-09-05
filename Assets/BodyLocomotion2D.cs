using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class BodyLocomotion2D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SpriteRenderer bodyRenderer; // Body �� SR
    [SerializeField] private Animator bodyAnimator;       // Idle/Walk ������ Animator

    [Header("Move")]
    [SerializeField] private float moveSpeed = 2.8f;

    Rigidbody2D rb;
    float inputX;
    int lastDir = 1; // +1=�E, -1=���i��~���̌����ێ��j

    static readonly int SpeedHash = Animator.StringToHash("Speed");

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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

        if (x != 0) lastDir = x;             // ��~���͍Ō�̌������ێ�
        if (bodyRenderer) bodyRenderer.flipX = (lastDir < 0);

        // Animator �֑��x�i0�`1�j��n��
        float speed01 = Mathf.Abs(inputX);
        if (bodyAnimator) bodyAnimator.SetFloat(SpeedHash, speed01);
    }

    void FixedUpdate()
    {
        // �����ړ�
        Vector2 v = rb.linearVelocity;
        v.x = inputX * moveSpeed;
        rb.linearVelocity = v;
    }
}