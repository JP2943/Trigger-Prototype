using UnityEngine;
using UnityEngine.InputSystem;

public class ArmAimer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform shoulderAnchor;
    [SerializeField] private Transform armPivot;
    [SerializeField] private SpriteRenderer bodyRenderer;

    // ★ 追加
    [SerializeField] private SpriteRenderer armRenderer;
    [SerializeField] private bool autoFlipYOnLeft = true;
    [SerializeField] private float angleOffsetDeg = 0f; // 左腕が左向き絵なら 180

    [Header("Lock-on")]
    [SerializeField] private Key lockKey = Key.F;
    [SerializeField] private string targetTag = "Enemy";
    [SerializeField] private float lockRadius = 50f;

    [Header("Marker")]
    [SerializeField] private TargetMarker markerPrefab;
    [SerializeField] private Color markerTint = Color.white;

    // 肩のソケット位置（Bodyのローカル座標：右向き時の値を入れる）
    [SerializeField] private Vector2 shoulderLocalOffset = new Vector2(0.20f, 0.85f);
    // BodyのTransform（未設定ならbodyRenderer.transformを使う）
    [SerializeField] private Transform bodyTransform;

    Transform lockTarget;
    TargetMarker marker;

    void Awake()
    {
        if (shoulderAnchor && armPivot && armPivot.parent != shoulderAnchor)
            armPivot.SetParent(shoulderAnchor, worldPositionStays: true);
    }

    void LateUpdate()
    {
        // ★ 肩位置を Body のローカルオフセットから算出（flipXでXを反転）
        if (!bodyTransform && bodyRenderer) bodyTransform = bodyRenderer.transform;

        if (bodyTransform)
        {
            float x = bodyRenderer && bodyRenderer.flipX ? -shoulderLocalOffset.x : shoulderLocalOffset.x;
            Vector3 local = new Vector3(x, shoulderLocalOffset.y, 0f);
            armPivot.position = bodyTransform.TransformPoint(local);
        }

        var kb = Keyboard.current;
        if (kb != null && kb[lockKey].wasPressedThisFrame) ToggleLock();

        if (lockTarget)
        {
            Vector2 dir = (lockTarget.position - armPivot.position);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + angleOffsetDeg;
            armPivot.rotation = Quaternion.Euler(0, 0, angle);

            // ★ 縦反転補正
            if (armRenderer && autoFlipYOnLeft)
            {
                bool facingLeft = (angle > 90f || angle < -90f);
                armRenderer.flipY = facingLeft;
                armRenderer.flipX = false;
            }

            if (!lockTarget || dir.sqrMagnitude > lockRadius * lockRadius) ClearLock();
        }
        else
        {
            float baseAngle = (bodyRenderer && bodyRenderer.flipX) ? 180f : 0f;
            float a = baseAngle + angleOffsetDeg;
            armPivot.rotation = Quaternion.Euler(0, 0, a);

            // ★ 非ロック時も補正
            if (armRenderer && autoFlipYOnLeft)
            {
                armRenderer.flipY = (a > 90f || a < -90f);
                armRenderer.flipX = false;
            }
        }
    }

    void ToggleLock()
    {
        if (lockTarget) { ClearLock(); return; }

        Transform nearest = null; float best = lockRadius * lockRadius;
        foreach (var e in GameObject.FindGameObjectsWithTag(targetTag))
        {
            float d2 = (e.transform.position - armPivot.position).sqrMagnitude;
            if (d2 < best) { best = d2; nearest = e.transform; }
        }
        lockTarget = nearest;

        if (lockTarget)
        {
            if (!marker && markerPrefab) marker = Instantiate(markerPrefab);
            if (marker) { marker.SetTarget(lockTarget); marker.SetTint(markerTint); }
        }
    }

    void ClearLock()
    {
        lockTarget = null;
        if (marker) { Destroy(marker.gameObject); marker = null; }
    }
}