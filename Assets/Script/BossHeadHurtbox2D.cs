using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BossHeadHurtbox2D : MonoBehaviour
{
    [Header("Refs")]
    public BossStats boss;

    [Header("Layer Filter")]
    [Tooltip("0 �̏ꍇ�̓t�B���^�����i�ǂ̃��C���[�ł��󂯕t���j")]
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
        if (!boss) { if (debugLog) Debug.LogWarning("[BossHead] BossStats �Q�ƂȂ�", this); return; }

        // ���C���[�}�X�N�i0�Ȃ疳���j
        if (playerBulletLayers.value != 0)
        {
            if (((1 << other.gameObject.layer) & playerBulletLayers.value) == 0)
            {
                if (debugLog) Debug.Log($"[BossHead] {via} but layer '{LayerMask.LayerToName(other.gameObject.layer)}' not in mask", other);
                return;
            }
        }

        // Bullet2D �� DamageCarrier ��T���i�e�q���܂߂āj
        var bullet = other.GetComponentInParent<Bullet2D>() ?? other.GetComponent<Bullet2D>() ?? other.GetComponentInChildren<Bullet2D>();
        var carrier = other.GetComponentInParent<DamageCarrier>() ?? other.GetComponent<DamageCarrier>() ?? other.GetComponentInChildren<DamageCarrier>();
        if (!bullet && !carrier)
        {
            if (debugLog) Debug.Log($"[BossHead] {via} with '{other.name}' but no Bullet2D/DamageCarrier", other);
            return;
        }

        // �@ DamageCarrier ������΂�����ŗD��
        int hpDamage, staminaDamage;
        if (carrier)
        {
            hpDamage = Mathf.Max(0, carrier.hpDamage);
            staminaDamage = Mathf.Max(0, carrier.staminaDamage);
        }
        else
        {
            // �A Bullet2D ���甽�˂Ő��l���擾�iint/float ���Ή��E��▼���L�߂Ɂj
            float hpF = FindNumberOn(bullet, new string[] { "damage", "Damage", "power", "Power", "atk", "Atk", "attack", "Attack", "bulletDamage" }, 1f);
            float stF = FindNumberOn(bullet, new string[] { "guardStaminaCost", "staminaDamage", "staminaCost", "StaminaDamage", "StaminaCost", "stamina" }, -1f);
            if (stF < 0f) stF = Mathf.Max(1f, hpF * 0.5f); // ������Ȃ���� HP �� 50% ������l��
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

// �e���ɕt����Ίm���ɏ㏑���ł���g�����_���[�W�h
public class DamageCarrier : MonoBehaviour
{
    [Header("Override damage to boss head")]
    public int hpDamage = 10;
    public int staminaDamage = 5;
}
