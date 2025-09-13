using UnityEngine;

/// <summary>
/// ターゲットを上下に往復移動させるテスト用スクリプト。
/// 初期位置を中心に、amplitude [unit] だけ上下へ sine で移動します。
/// Rigidbody2D が付いていれば MovePosition、無ければ Transform で移動します。
/// </summary>
[DisallowMultipleComponent]
public class VerticalOscillator2D : MonoBehaviour
{
    [SerializeField] private float amplitude = 2f;     // 上下の振れ幅（中心からの距離）
    [SerializeField] private float frequency = 0.5f;   // 1秒あたりの往復回数（Hz）
    [SerializeField] private float phaseOffset = 0f;   // ランダム化したい時は 0～2π

    private float startY;
    private Rigidbody2D rb;

    void Awake()
    {
        startY = transform.position.y;
        rb = GetComponent<Rigidbody2D>();
        // 物理を使わないなら Rigidbody2D は不要。付けるなら Kinematic 推奨。
        if (rb && rb.bodyType == RigidbodyType2D.Dynamic)
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void FixedUpdate()
    {
        float t = Time.time;
        float y = startY + amplitude * Mathf.Sin((Mathf.PI * 2f * frequency) * t + phaseOffset);

        if (rb) rb.MovePosition(new Vector2(rb.position.x, y));
        else transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }

    // エディタで選択時に移動範囲を表示
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 p = Application.isPlaying ? new Vector3(transform.position.x, startY, transform.position.z)
                                          : transform.position;
        Gizmos.DrawLine(p + Vector3.up * amplitude, p - Vector3.up * amplitude);
        Gizmos.DrawSphere(p + Vector3.up * amplitude, 0.05f);
        Gizmos.DrawSphere(p - Vector3.up * amplitude, 0.05f);
    }
}