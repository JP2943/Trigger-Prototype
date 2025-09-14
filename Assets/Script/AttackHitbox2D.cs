using System.Collections.Generic;
using UnityEngine;

/// 近接攻撃判定（手など）。IHittable に HitInfo を送る
[RequireComponent(typeof(Collider2D))]
public class AttackHitbox2D : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 12;
    [SerializeField] private float guardStaminaCost = 25f;

    [Header("Stun / Knockback")]
    [SerializeField] private bool causeStun = true; // ← のけぞらせる？
    [SerializeField] private float stunSeconds = 0.22f;
    [SerializeField] private Vector2 knockback = new(5f, 2f); // (横, 上)のImpulse

    [Header("Targets")]
    [SerializeField] private LayerMask targetMask; // Player をON
    [SerializeField] private bool oneHitPerSwing = true;

    bool _active;
    readonly HashSet<Collider2D> _hit = new();

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }

    public void SetActive(bool on)
    {
        _active = on;
        if (on) _hit.Clear();
        var col = GetComponent<Collider2D>(); if (col) col.enabled = on;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!_active) return;
        if ((targetMask.value & (1 << other.gameObject.layer)) == 0) return;
        if (oneHitPerSwing && _hit.Contains(other)) return;
        _hit.Add(other);

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
    }
}
