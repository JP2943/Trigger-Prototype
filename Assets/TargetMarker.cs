using UnityEngine;

/// ロック対象に追従するマーカー
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class TargetMarker : MonoBehaviour
{
    [SerializeField] Vector3 worldOffset = new(0, 0.6f, 0); // 頭上に少しオフセット
    [SerializeField] bool keepFixedScreenSize = true;
    [SerializeField] float referenceOrthoSize = 5f; // カメラの基準ズーム
    [SerializeField] float referenceScale = 1f;     // 基準スケール
    [SerializeField] private SpriteRenderer sr;
    public void SetTint(Color c) { if (!sr) sr = GetComponent<SpriteRenderer>(); if (sr) sr.color = c; }

    Transform target;
    Camera cam;

    public void SetTarget(Transform t) => target = t;

    void Awake() => cam = Camera.main;

    void LateUpdate()
    {
        if (!target) { Destroy(gameObject); return; }

        // 位置追従（親にしないので回転の影響を受けない）
        transform.position = target.position + worldOffset;
        transform.rotation = Quaternion.identity;

        // ズームしても見た目サイズをほぼ一定に保つ（2Dオルソ用）
        if (keepFixedScreenSize && cam && cam.orthographic)
        {
            float k = cam.orthographicSize / referenceOrthoSize;
            transform.localScale = Vector3.one * (referenceScale * k);
        }
    }
}