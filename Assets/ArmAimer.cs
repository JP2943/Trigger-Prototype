using UnityEngine;
using UnityEngine.InputSystem;

public class ArmAimerSingle : MonoBehaviour
{
    [Header("Arm Setup")]
    [SerializeField] private Transform shoulderAnchor;   // R_Shoulder or L_Shoulder
    [SerializeField] private Transform armPivot;         // R_ArmPivot or L_ArmPivot
    [SerializeField] private SpriteRenderer bodyRenderer;// Body（体の向き参照）
    [Tooltip("腕スプライトの基準向き。右向き=0°, 左向き=180°")]
    [SerializeField] private float angleOffsetDeg = 0f;  // 左腕が左向き絵なら 180 に

    [Header("Lock-on")]
    [SerializeField] private Key lockKey = Key.F;        // 右腕=F、左腕=G など
    [SerializeField] private string targetTag = "Enemy";
    [SerializeField] private float lockRadius = 50f;

    [Header("Marker")]
    [SerializeField] private TargetMarker markerPrefab;  // 共通プレハブ
    [SerializeField] private Color markerTint = Color.white;

    Transform target;
    TargetMarker marker;

    void Awake()
    {
        // 念のため親子関係を保証
        if (shoulderAnchor && armPivot && armPivot.parent != shoulderAnchor)
            armPivot.SetParent(shoulderAnchor, worldPositionStays: true);
    }

    void LateUpdate()
    {
        // 肩に固定（プレイヤー/アニメ/物理の後で）
        if (shoulderAnchor && armPivot)
            armPivot.position = shoulderAnchor.position;

        // キーでロック切替
        var kb = Keyboard.current;
        if (kb != null && kb[lockKey].wasPressedThisFrame)
            ToggleLock();

        if (target)
        {
            Vector2 dir = (target.position - armPivot.position);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + angleOffsetDeg;
            armPivot.rotation = Quaternion.Euler(0, 0, angle);

            // 距離が離れた/ターゲット破棄で解除
            if (!target || dir.sqrMagnitude > lockRadius * lockRadius) ClearLock();
        }
        else
        {
            // 非ロック時は体の向きに合わせて正面へ
            float baseAngle = (bodyRenderer && bodyRenderer.flipX) ? 180f : 0f;
            armPivot.rotation = Quaternion.Euler(0, 0, baseAngle + angleOffsetDeg);
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

        if (target)
        {
            if (!marker && markerPrefab) marker = Instantiate(markerPrefab);
            if (marker) { marker.SetTarget(target); marker.SetTint(markerTint); }
        }
    }

    void ClearLock()
    {
        target = null;
        if (marker) { Destroy(marker.gameObject); marker = null; }
    }
}