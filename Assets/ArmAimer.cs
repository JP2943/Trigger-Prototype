using UnityEngine;
using UnityEngine.InputSystem;

public class ArmAimer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform armPivot;            // ��]�̒��S�iR_ArmPivot / L_ArmPivot�j
    [SerializeField] private SpriteRenderer bodyRenderer;   // �̂̌����Q�ƁiBody �� SR�j
    [SerializeField] private Transform bodyTransform;       // ���ݒ�Ȃ� bodyRenderer.transform ���g�p
    [SerializeField] private Animator bodyAnimator;         // Body �� Animator�iSpeed / Airborne ��ǂށj

    [Header("Shoulder Offsets (Body local, right-facing)")]
    [SerializeField] private Vector2 shoulderOffsetIdle = new(0.20f, 0.85f);
    [SerializeField] private Vector2 shoulderOffsetWalk = new(0.20f, 0.85f);
    [SerializeField] private Vector2 shoulderOffsetJump = new(0.20f, 0.95f);
    [SerializeField] private float offsetLerpSpeed = 15f;   // �I�t�Z�b�g�ؑւ̊��炩��

    [Header("Animator Params (names)")]
    [SerializeField] private string speedParam = "Speed";       // float 0..1
    [SerializeField] private string airborneParam = "Airborne"; // bool

    [Header("Aim / Visual")]
    [SerializeField] private SpriteRenderer armRenderer;    // �r��SR�iflipY�␳�Ɏg�p�j
    [SerializeField] private bool autoFlipYOnLeft = true;   // ����������������flipY�Ō����ڕ␳
    [Tooltip("�r�X�v���C�g�̊�����B�E�����Ȃ� 0�A�������G�Ȃ� 180�B")]
    [SerializeField] private float angleOffsetDeg = 0f;

    [Header("Lock-on Input")]
    public InputActionReference lockAction;                 // LB/RB �Ȃǂ������i�Иr���ƂɁj
    [SerializeField] private Key lockKeyFallback = Key.F;   // InputAction���ݒ莞�̃t�H�[���o�b�N�L�[

    [Header("Lock-on Settings")]
    [SerializeField] private string targetTag = "Enemy";
    [SerializeField] private float lockRadius = 50f;

    [Header("Target Marker")]
    [SerializeField] private TargetMarker markerPrefab;
    [SerializeField] private Color markerTint = Color.white;

    [Header("Recoil (added on top of aim angle)")]
    [SerializeField] private float recoilRecoverDegPerSec = 600f; // 1�b������߂��p�x

    // --------------- runtime state ---------------
    private Transform lockTarget;
    private TargetMarker marker;
    private Vector2 currentOffset;
    private float recoilAngle; // �ꎞ�I�ȁg�r�̒��ˏグ�p�h�����Z

    private int speedHash;
    private int airborneHash;

    void Awake()
    {
        if (!bodyTransform && bodyRenderer) bodyTransform = bodyRenderer.transform;
        currentOffset = shoulderOffsetIdle;
        speedHash = Animator.StringToHash(speedParam);
        airborneHash = Animator.StringToHash(airborneParam);
    }

    void OnEnable() { lockAction?.action.Enable(); }
    void OnDisable() { lockAction?.action.Disable(); }

    void LateUpdate()
    {
        if (!armPivot || !bodyRenderer) return;

        // ---- 1) ���ʒu�F��ԕʃI�t�Z�b�g��I��ŁAflipX �ɉ����č��E�~���[ ----
        Vector2 targetOffset = shoulderOffsetIdle;
        if (bodyAnimator)
        {
            bool airborne = bodyAnimator.GetBool(airborneHash);
            float speed = bodyAnimator.GetFloat(speedHash);
            if (airborne) targetOffset = shoulderOffsetJump;
            else if (speed > 0.01f) targetOffset = shoulderOffsetWalk;
        }
        // �X���[�Y�ɐ؂�ւ�
        float k = 1f - Mathf.Exp(-offsetLerpSpeed * Time.deltaTime);
        currentOffset = Vector2.Lerp(currentOffset, targetOffset, k);

        // �E������I�t�Z�b�g�� flipX �ɍ��킹�ă~���[ �� ���[���h���W��
        if (!bodyTransform) bodyTransform = bodyRenderer.transform;
        float ox = bodyRenderer.flipX ? -currentOffset.x : currentOffset.x;
        Vector3 local = new Vector3(ox, currentOffset.y, 0f);
        armPivot.position = bodyTransform.TransformPoint(local);

        // ---- 2) ���́F���b�N�I���ؑցiInputAction or Fallback�L�[�j ----
        bool toggle = false;
        if (lockAction) toggle = lockAction.action.WasPressedThisFrame();
        else if (Keyboard.current != null) toggle = Keyboard.current[lockKeyFallback].wasPressedThisFrame;
        if (toggle) ToggleLock();

        // ---- 3) �Ə��p�i�ڕW�p + ���R�C���p�j & flipY�␳ ----
        if (lockTarget)
        {
            Vector2 dir = (lockTarget.position - armPivot.position);
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + angleOffsetDeg;
            ang += recoilAngle; // �����R�C�����Z
            armPivot.rotation = Quaternion.Euler(0, 0, ang);

            if (armRenderer && autoFlipYOnLeft)
            {
                bool facingLeft = (ang > 90f || ang < -90f);
                armRenderer.flipY = facingLeft;
                armRenderer.flipX = false;
            }

            // �͈͊O/�j���ŉ���
            if (!lockTarget || dir.sqrMagnitude > lockRadius * lockRadius)
                ClearLock();
        }
        else
        {
            float baseAng = bodyRenderer.flipX ? 180f : 0f;
            float ang = baseAng + angleOffsetDeg + recoilAngle; // �����R�C�����Z
            armPivot.rotation = Quaternion.Euler(0, 0, ang);

            if (armRenderer && autoFlipYOnLeft)
            {
                bool facingLeft = (ang > 90f || ang < -90f);
                armRenderer.flipY = facingLeft;
                armRenderer.flipX = false;
            }
        }

        // ---- 4) ���R�C���p�����X��0�֖߂� ----
        recoilAngle = Mathf.MoveTowards(recoilAngle, 0f, recoilRecoverDegPerSec * Time.deltaTime);
    }

    // ============== Public API (Gun2D ����Ă�) ==============
    public void AddRecoilAngle(float deg)
    {
        recoilAngle += deg; // ��F����a�Ȃ� -10�`-16 �Ȃǂ𑫂�
    }

    // ============== Lock-on helpers ==============
    void ToggleLock()
    {
        if (lockTarget) { ClearLock(); return; }

        Transform nearest = null;
        float best = lockRadius * lockRadius;
        foreach (var e in GameObject.FindGameObjectsWithTag(targetTag))
        {
            float d2 = (e.transform.position - armPivot.position).sqrMagnitude;
            if (d2 < best) { best = d2; nearest = e.transform; }
        }
        lockTarget = nearest;

        if (lockTarget && markerPrefab)
        {
            if (!marker) marker = Instantiate(markerPrefab);
            marker.SetTarget(lockTarget);
            marker.SetTint(markerTint);
        }
    }

    void ClearLock()
    {
        lockTarget = null;
        if (marker) { Destroy(marker.gameObject); marker = null; }
    }

    // ���o�f�o�b�O�i�I�����Ɍ��ʒu��\���j
    void OnDrawGizmosSelected()
    {
        if (!bodyRenderer) return;
        if (!bodyTransform) bodyTransform = bodyRenderer.transform;

        Vector2 use = Application.isPlaying ? currentOffset : shoulderOffsetIdle;
        float ox = (bodyRenderer.flipX ? -use.x : use.x);
        Vector3 p = bodyTransform.TransformPoint(new Vector3(ox, use.y, 0f));
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(p, 0.045f);
    }
}
