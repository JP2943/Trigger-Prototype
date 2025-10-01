using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BossHeadHurtbox2D : MonoBehaviour
{
    [Header("Refs")]
    public BossStats boss;

    [Header("Layer Filter")]
    [Tooltip("0 の場合はフィルタ無効（どのレイヤーでも受け付け）")]
    public LayerMask playerBulletLayers = 0;

    [Header("Options")]
    public bool destroyBulletOnHit = true;
    public bool debugLog = false;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        if (!boss) boss = GetComponentInParent<BossStats>();
    }

    void OnTriggerEnter2D(Collider2D other) { TryHandle(other, "Trigger"); }
    void OnCollisionEnter2D(Collision2D col) { if (col.collider) TryHandle(col.collider, "Collision"); }

    void TryHandle(Collider2D other, string via)
    {
        if (!boss) { if (debugLog) Debug.LogWarning("[BossHead] BossStats 参照なし", this); return; }

        // レイヤーマスク（0なら無効）
        if (playerBulletLayers.value != 0)
        {
            if (((1 << other.gameObject.layer) & playerBulletLayers.value) == 0)
            {
                if (debugLog) Debug.Log($"[BossHead] {via} but layer '{LayerMask.LayerToName(other.gameObject.layer)}' not in mask", other);
                return;
            }
        }

        // Bullet2D と DamageCarrier を探す（親子も含めて）
        var bullet = other.GetComponentInParent<Bullet2D>() ?? other.GetComponent<Bullet2D>() ?? other.GetComponentInChildren<Bullet2D>();
        var carrier = other.GetComponentInParent<DamageCarrier>() ?? other.GetComponent<DamageCarrier>() ?? other.GetComponentInChildren<DamageCarrier>();
        if (!bullet && !carrier)
        {
            if (debugLog) Debug.Log($"[BossHead] {via} with '{other.name}' but no Bullet2D/DamageCarrier", other);
            return;
        }

        // ① DamageCarrier があればそれを最優先
        int hpDamage, staminaDamage;
        if (carrier)
        {
            hpDamage = Mathf.Max(0, carrier.hpDamage);
            staminaDamage = Mathf.Max(0, carrier.staminaDamage);
        }
        else
        {
            // ② Bullet2D から反射で数値を取得（int/float 両対応・候補名を広めに）
            float hpF = FindNumberOn(bullet, new string[] { "damage", "Damage", "power", "Power", "atk", "Atk", "attack", "Attack", "bulletDamage" }, 1f);
            float stF = FindNumberOn(bullet, new string[] { "guardStaminaCost", "staminaDamage", "staminaCost", "StaminaDamage", "StaminaCost", "stamina" }, -1f);
            if (stF < 0f) stF = Mathf.Max(1f, hpF * 0.5f); // 見つからなければ HP の 50% を既定値に
            hpDamage = Mathf.RoundToInt(hpF);
            staminaDamage = Mathf.RoundToInt(stF);
        }

        boss.ApplyHit(hpDamage, staminaDamage);
        if (debugLog) Debug.Log($"[BossHead] HIT via {via}: {other.name}  HP-{hpDamage}, STA-{staminaDamage}", this);

        if (destroyBulletOnHit && bullet) Destroy(bullet.gameObject);
        else if (destroyBulletOnHit && carrier) Destroy(carrier.gameObject);
    }

    float FindNumberOn(object obj, string[] names, float fallback)
    {
        if (obj == null) return fallback;
        var t = obj.GetType();
        const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
        foreach (var n in names)
        {
            var f = t.GetField(n, F);
            if (f != null)
            {
                var ft = f.FieldType;
                if (ft == typeof(int)) return (int)f.GetValue(obj);
                if (ft == typeof(float)) return (float)f.GetValue(obj);
            }
            var p = t.GetProperty(n, F);
            if (p != null)
            {
                var pt = p.PropertyType;
                if (pt == typeof(int)) return (int)p.GetValue(obj);
                if (pt == typeof(float)) return (float)p.GetValue(obj);
            }
        }
        return fallback;
    }
}

// 弾側に付ければ確実に上書きできる“明示ダメージ”
public class DamageCarrier : MonoBehaviour
{
    [Header("Override damage to boss head")]
    public int hpDamage = 10;
    public int staminaDamage = 5;
}
