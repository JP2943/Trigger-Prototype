using UnityEngine;
using UnityEngine.InputSystem;

/// X�{�^���Ń_�b�V���B���G+iLayer���蔲��+�N�[���_�E���B
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDash : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerHealthGuard guardRef;        // HP/�K�[�h�Ǘ�
    [SerializeField] private SpriteRenderer facingSprite;       // ��������p�iBody��SR�j
    [SerializeField] private Animator bodyAnimator;             // �����ڐؑ�
    [SerializeField] private InputActionReference dashAction;   // Gameplay/Dash�iGamepad/x�j

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
        if (active) StopDash(); // �N���[���A�b�v�ی�
    }

    void Update()
    {
        if (active)
        {
            if (Time.time >= dashEndTime) StopDash();
            return;
        }

        // ���́iGamepad X�j�B�L�[�{�[�h�m�F�p: LeftShift ��ǉ�
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
            v.x = dirX * dashSpeed; // ��Fixed�ŏ㏑�����Ĉ�葬�x���ێ�
            rb.linearVelocity = v;
        }
    }

    void TryStartDash()
    {
        if (Time.time < nextDashTime) return;
        if (!guardRef) return;
        if (guardRef.IsHurt || guardRef.IsGuarding) return; // �̂�����/�K�[�h���͕s��
        if (!guardRef.TrySpendStamina(staminaCost)) return; // �X�^�~�i�s��

        dirX = (facingSprite && facingSprite.flipX) ? -1f : 1f;

        // ���G+iFrame�������u�_�b�V�����v�t���O�iBodyLocomotion����Q�Ɓj
        guardRef.SetDashing(true, dashDuration);

        // ���蔲���iPlayer�~Enemy/EnemyAttack �𖳌����j
        if (enemyLayer >= 0) Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
        if (enemyAttackLayer >= 0) Physics2D.IgnoreLayerCollision(playerLayer, enemyAttackLayer, true);

        // �A�j��
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

        // �Փ˂����ɖ߂�
        if (enemyLayer >= 0) Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
        if (enemyAttackLayer >= 0) Physics2D.IgnoreLayerCollision(playerLayer, enemyAttackLayer, false);
    }
}
