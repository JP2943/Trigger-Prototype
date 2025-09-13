using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class Bullet2D : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] float speed = 20f;
    [SerializeField] float lifeTime = 3f;

    [Header("Damage")]
    [SerializeField] int damage = 6;
    [SerializeField, Tooltip("この弾をガードされたときに消費させたいスタミナ量")]
    float guardStaminaCost = 8f;

    Rigidbody2D rb;

    void Awake() { rb = GetComponent<Rigidbody2D>(); }

    void OnEnable()
    {
        rb.gravityScale = 0f;
        rb.linearVelocity = (Vector2)(transform.right * speed); // ローカル+Xへ
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // レイヤーで味方間衝突を切っておくのが前提（Player弾がPlayerに当たらないように）
        if (other.TryGetComponent<IHittable>(out var h))
        {
            Vector2 p = transform.position;
            Vector2 n = (other.transform.position - transform.position).normalized;
            var hit = new HitInfo
            {
                damage = damage,
                guardStaminaCost = guardStaminaCost,
                hitPoint = p,
                hitNormal = n,
                source = gameObject
            };
            h.ReceiveHit(hit);
        }
        Destroy(gameObject);
    }
}
