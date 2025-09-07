using UnityEngine;
using UnityEngine.InputSystem;

public class ArmAimerSingle : MonoBehaviour
{
    [Header("Arm Setup")]
    [SerializeField] private Transform armPivot;
    [SerializeField] private SpriteRenderer bodyRenderer;  // 体の向き参照
    [SerializeField] private Transform bodyTransform;      // 未設定なら bodyRenderer.transform を使用

    [Header("Shoulder Offsets (right-facing, local to Body)")]
    [SerializeField] private Vector2 shoulderOffsetIdle = new(0.20f, 0.85f);
    [SerializeField] private Vector2 shoulderOffsetWalk = new(0.20f, 0.85f);
    [SerializeField] private Vector2 shoulderOffsetJump = new(0.20f, 0.95f);
    [SerializeField] private float offsetLerpSpeed = 15f; // 切替の滑らかさ

    [Header("Animator Params")]
    [SerializeField] private Animator bodyAnimator;        // Body の Animator
    [SerializeField] private string speedParam = "Speed";  // 0..1
    [SerializeField] private string airborneParam = "Airborne"; // bool

    [Header("Aim/Look (既存)")]
    [SerializeField] private SpriteRenderer armRenderer;
    [SerializeField] private bool autoFlipYOnLeft = true;
    [SerializeField] private float angleOffsetDeg = 0f;
    [SerializeField] private Key lockKey = Key.F;
    [SerializeField] private string targetTag = "Enemy";
    [SerializeField] private float lockRadius = 50f;
    [SerializeField] private TargetMarker markerPrefab;
    [SerializeField] private Color markerTint = Color.white;

    Transform target; TargetMarker marker;
    Vector2 currentOffset; // 状態間をスムーズに補間

    void Awake()
    {
        if (!bodyTransform && bodyRenderer) bodyTransform = bodyRenderer.transform;
        currentOffset = shoulderOffsetIdle;
    }

    void LateUpdate()
    {
        // ------ 肩位置（状態に応じてオフセットを選ぶ） ------
        Vector2 targetOffset = shoulderOffsetIdle;
        if (bodyAnimator)
        {
            bool airborne = bodyAnimator.GetBool(Animator.StringToHash(airborneParam));
            float speed = bodyAnimator.GetFloat(Animator.StringToHash(speedParam));
            if (airborne) targetOffset = shoulderOffsetJump;
            else if (speed > 0.01f) targetOffset = shoulderOffsetWalk;
        }

        // スムーズに切替
        float t = 1f - Mathf.Exp(-offsetLerpSpeed * Time.deltaTime);
        currentOffset = Vector2.Lerp(currentOffset, targetOffset, t);

        // flipX なら X を反転して世界座標へ変換
        float x = (bodyRenderer && bodyRenderer.flipX) ? -currentOffset.x : currentOffset.x;
        Vector3 local = new Vector3(x, currentOffset.y, 0f);
        if (bodyTransform) armPivot.position = bodyTransform.TransformPoint(local);

        // ------ 以下：既存のロックオン＆角度計算 ------
        var kb = Keyboard.current;
        if (kb != null && kb[lockKey].wasPressedThisFrame) ToggleLock();

        if (target)
        {
            Vector2 dir = (target.position - armPivot.position);
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + angleOffsetDeg;
            armPivot.rotation = Quaternion.Euler(0, 0, ang);

            if (armRenderer && autoFlipYOnLeft)
            {
                bool left = (ang > 90f || ang < -90f);
                armRenderer.flipY = left; armRenderer.flipX = false;
            }
            if (!target || dir.sqrMagnitude > lockRadius * lockRadius) ClearLock();
        }
        else
        {
            float baseAng = (bodyRenderer && bodyRenderer.flipX) ? 180f : 0f;
            float a = baseAng + angleOffsetDeg;
            armPivot.rotation = Quaternion.Euler(0, 0, a);
            if (armRenderer && autoFlipYOnLeft)
            {
                armRenderer.flipY = (a > 90f || a < -90f); armRenderer.flipX = false;
            }
        }
    }

    void ToggleLock()
    {
        if (target) { ClearLock(); return; }
        Transform nearest = null; float best = lockRadius * lockRadius;
        foreach (var e in GameObject.FindGameObjectsWithTag(targetTag))
        {
            float d2 = (e.transform.position - armPivot.position).sqrMagnitude;
            if (d2 < best) { best = d2; nearest = e.transform; }
        }
        target = nearest;
        if (target && markerPrefab)
        {
            if (!marker) marker = Instantiate(markerPrefab);
            marker.SetTarget(target); marker.SetTint(markerTint);
        }
    }

    void ClearLock() { target = null; if (marker) { Destroy(marker.gameObject); marker = null; } }

    // 視覚デバッグ（選択時）
    void OnDrawGizmosSelected()
    {
        if (!bodyTransform) return;
        float x = (bodyRenderer && bodyRenderer.flipX) ? -currentOffset.x : currentOffset.x;
        Vector3 p = bodyTransform.TransformPoint(new Vector3(x, currentOffset.y, 0f));
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(p, 0.04f);
    }
}