using System.Collections.Generic;
using UnityEngine;

/// 攻撃判定（近接）。IHittable に HitInfo を渡す版
[RequireComponent(typeof(Collider2D))]
public class AttackHitbox2D : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 12;
    [SerializeField, Tooltip("ガードされた時に消費させたいスタミナ量")]
    private float guardStaminaCost = 25f;

    [Header("Targets")]
    [SerializeField] private LayerMask targetMask;      // Player レイヤーをON
    [SerializeField] private bool oneHitPerSwing = true;

    private bool _active;
    private readonly HashSet<Collider2D> _hit = new();

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }

    public void SetActive(bool on)
    {
        _active = on;
        if (on) _hit.Clear();
        var col = GetComponent<Collider2D>();
        if (col) col.enabled = on;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!_active) return;
        if ((targetMask.value & (1 << other.gameObject.layer)) == 0) return;
        if (oneHitPerSwing && _hit.Contains(other)) return;

        _hit.Add(other);

        // プレイヤーなど IHittable に渡す
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
    }
}
