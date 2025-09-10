using System.Collections.Generic;
using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int amount, Vector2 hitPoint, Vector2 hitNormal);
}

[RequireComponent(typeof(Collider2D))]
public class AttackHitbox2D : MonoBehaviour
{
    [SerializeField] private int damage = 10;
    [SerializeField] private LayerMask targetMask;     // Player‚È‚Ç
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

        var pos = transform.position;
        Vector2 normal = (other.transform.position - pos).normalized;

        if (other.TryGetComponent<IDamageable>(out var dmg))
            dmg.TakeDamage(damage, pos, normal);
    }
}
