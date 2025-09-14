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
    [SerializeField] float guardStaminaCost = 8f;

    [Header("Flags")]
    [SerializeField, Tooltip("trueにすると被弾スタンも発生する弾になる")]
    bool causeStun = false;                 // ← デフォルトは「のけぞらない」
    [SerializeField] float stunSeconds = 0.15f;
    [SerializeField] Vector2 knockback = new(2.5f, 1.2f);

    Rigidbody2D rb;

    void Awake() { rb = GetComponent<Rigidbody2D>(); }
    void OnEnable()
    {
        rb.gravityScale = 0f;
        rb.linearVelocity = (Vector2)(transform.right * speed);
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<IHittable>(out var h))
        {
            Vector2 p = transform.position;
            Vector2 n = (other.transform.position - transform.position).normalized;
            var hit = new HitInfo
            {
                damage = damage,
                guardStaminaCost = guardStaminaCost,
                causeStun = causeStun,
                stunSeconds = stunSeconds,
                knockback = knockback,
                hitPoint = p,
                hitNormal = n,
                source = gameObject
            };
            h.ReceiveHit(hit);
        }
        Destroy(gameObject);
    }
}
