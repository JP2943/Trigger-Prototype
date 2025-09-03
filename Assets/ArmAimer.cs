using UnityEngine;
using UnityEngine.InputSystem;

public class ArmAimerSingle : MonoBehaviour
{
    [Header("Arm Setup")]
    [SerializeField] private Transform shoulderAnchor;   // R_Shoulder or L_Shoulder
    [SerializeField] private Transform armPivot;         // R_ArmPivot or L_ArmPivot
    [SerializeField] private SpriteRenderer bodyRenderer;// Body�i�̂̌����Q�Ɓj
    [Tooltip("�r�X�v���C�g�̊�����B�E����=0��, ������=180��")]
    [SerializeField] private float angleOffsetDeg = 0f;  // ���r���������G�Ȃ� 180 ��

    [Header("Lock-on")]
    [SerializeField] private Key lockKey = Key.F;        // �E�r=F�A���r=G �Ȃ�
    [SerializeField] private string targetTag = "Enemy";
    [SerializeField] private float lockRadius = 50f;

    [Header("Marker")]
    [SerializeField] private TargetMarker markerPrefab;  // ���ʃv���n�u
    [SerializeField] private Color markerTint = Color.white;

    Transform target;
    TargetMarker marker;

    void Awake()
    {
        // �O�̂��ߐe�q�֌W��ۏ�
        if (shoulderAnchor && armPivot && armPivot.parent != shoulderAnchor)
            armPivot.SetParent(shoulderAnchor, worldPositionStays: true);
    }

    void LateUpdate()
    {
        // ���ɌŒ�i�v���C���[/�A�j��/�����̌�Łj
        if (shoulderAnchor && armPivot)
            armPivot.position = shoulderAnchor.position;

        // �L�[�Ń��b�N�ؑ�
        var kb = Keyboard.current;
        if (kb != null && kb[lockKey].wasPressedThisFrame)
            ToggleLock();

        if (target)
        {
            Vector2 dir = (target.position - armPivot.position);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + angleOffsetDeg;
            armPivot.rotation = Quaternion.Euler(0, 0, angle);

            // ���������ꂽ/�^�[�Q�b�g�j���ŉ���
            if (!target || dir.sqrMagnitude > lockRadius * lockRadius) ClearLock();
        }
        else
        {
            // �񃍃b�N���͑̂̌����ɍ��킹�Đ��ʂ�
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