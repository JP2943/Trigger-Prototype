using UnityEngine;
using UnityEngine.InputSystem;

public class ArmAimer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform armPivot;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private Animator bodyAnimator;

    [Header("Shoulder Offsets (Body local, right-facing)")]
    [SerializeField] private Vector2 shoulderOffsetIdle = new(0.20f, 0.85f);
    [SerializeField] private Vector2 shoulderOffsetWalk = new(0.20f, 0.85f);
    [SerializeField] private Vector2 shoulderOffsetJump = new(0.20f, 0.95f);
    [SerializeField] private float offsetLerpSpeed = 15f;

    [Header("Aim")]
    [SerializeField] private bool aimAtMouse = true;
    [SerializeField] private string targetTag = "Enemy";
    [SerializeField] private float lockRadius = 8f;
    public InputActionReference lockAction;
    [SerializeField] private float angleOffsetDeg = 0f;
    [SerializeField] private bool autoFlipYOnLeft = true;

    [Header("Target Marker")]
    [SerializeField] private TargetMarker markerPrefab;
    [SerializeField] private Color markerTint = Color.white;

    [Header("Recoil (added on top of aim angle)")]
    [SerializeField] private float recoilRecoverDegPerSec = 60f;    // 線形復帰
    [SerializeField] private bool useExponentialRecover = false;    // 指数減衰トグル
    [SerializeField, Range(0f, 1f)] private float recoilDecayPerSecond = 0.2f;

    [Header("Arm Sprite (optional)")]
    [SerializeField] private SpriteRenderer armRenderer;

    // 内部状態
    private Vector2 currentOffset;
    private float recoilAngle = 0f;
    private Transform lockTarget;
    private TargetMarker marker;

    void Reset()
    {
        if (!bodyRenderer) bodyRenderer = GetComponentInParent<SpriteRenderer>();
        if (!armPivot) armPivot = transform;
    }

    void Awake()
    {
        if (!bodyTransform && bodyRenderer) bodyTransform = bodyRenderer.transform;
        currentOffset = shoulderOffsetIdle;
    }

    void Update()
    {
        if (!armPivot || !bodyRenderer) return;

        // 1) 肩オフセット
        Vector2 to = shoulderOffsetIdle;
        if (bodyAnimator)
        {
            bool airborne = bodyAnimator.GetBool("Airborne");
            float spd = bodyAnimator.GetFloat("Speed");
            if (airborne) to = shoulderOffsetJump;
            else if (spd > 0.01f) to = shoulderOffsetWalk;
        }
        float k = 1f - Mathf.Exp(-offsetLerpSpeed * Time.deltaTime);
        currentOffset = Vector2.Lerp(currentOffset, to, k);

        if (!bodyTransform) bodyTransform = bodyRenderer.transform;
        float ox = bodyRenderer.flipX ? -currentOffset.x : currentOffset.x;
        Vector3 local = new Vector3(ox, currentOffset.y, 0f);
        armPivot.position = bodyTransform.TransformPoint(local);

        // 2) ロックオン切替
        bool toggle = false;
        if (lockAction) toggle = lockAction.action.WasPressedThisFrame();
        else if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) toggle = true;
        if (toggle) ToggleLock();

        // 3) 照準＋リコイル角の加算
        float ang;
        if (lockTarget)
        {
            Vector2 dir = (lockTarget.position - armPivot.position);
            ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + angleOffsetDeg;
        }
        else if (aimAtMouse && Camera.main)
        {
            Vector3 mp = Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Input.mousePosition;
            Vector3 world = Camera.main.ScreenToWorldPoint(mp);
            Vector2 dir = (world - armPivot.position);
            ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + angleOffsetDeg;
        }
        else
        {
            ang = (bodyRenderer.flipX ? 180f : 0f) + angleOffsetDeg;
        }

        ang += recoilAngle;
        armPivot.rotation = Quaternion.Euler(0, 0, ang);

        if (armRenderer && autoFlipYOnLeft)
        {
            bool facingLeft = (ang > 90f || ang < -90f);
            armRenderer.flipY = facingLeft;
            armRenderer.flipX = false;
        }

        // 4) リコイル復帰
        if (useExponentialRecover)
        {
            float kExp = Mathf.Pow(recoilDecayPerSecond, Time.deltaTime);
            recoilAngle *= kExp;
        }
        else
        {
            recoilAngle = Mathf.MoveTowards(recoilAngle, 0f, recoilRecoverDegPerSec * Time.deltaTime);
        }
    }

    // --- 外部API（角度をそのまま足す） ---
    public void AddRecoilAngle(float deg)
    {
        recoilAngle += deg;
    }

    // --- 外部API（常に“画面の上方向”へ跳ね上げる） ---
    public void AddRecoilUpwards(float deg)
    {
        if (!armPivot) return;
        // Z角（-180〜180）を基準に左向き判定
        float z = Mathf.DeltaAngle(0f, armPivot.eulerAngles.z);
        bool facingLeft = (z > 90f || z < -90f);
        // 左向き＝時計回り(負角)が上、右向き＝反時計回り(正角)が上
        recoilAngle += facingLeft ? -Mathf.Abs(deg) : +Mathf.Abs(deg);
    }

    // --- Lock on helpers ---
    void ToggleLock()
    {
        if (lockTarget) { ClearLock(); return; }
        Transform nearest = null;
        float best = lockRadius * lockRadius;
        foreach (var e in GameObject.FindGameObjectsWithTag(targetTag))
        {
            float d2 = (e.transform.position - armPivot.position).sqrMagnitude;
            if (d2 <= best) { best = d2; nearest = e.transform; }
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

    void OnDrawGizmosSelected()
    {
        if (!bodyRenderer) return;
        if (!bodyTransform) bodyTransform = bodyRenderer.transform;
        Vector2 use = Application.isPlaying ? currentOffset : shoulderOffsetIdle;
        float ox2 = (bodyRenderer.flipX ? -use.x : use.x);
        Vector3 p = bodyTransform.TransformPoint(new Vector3(ox2, use.y, 0f));
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(p, 0.045f);
    }
}
