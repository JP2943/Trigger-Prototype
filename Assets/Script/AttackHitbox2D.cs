using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class AttackHitbox2D : MonoBehaviour
{
    [Header("Hit Values")]
    [Min(0)] public int damage = 10;               // HPダメージは int に統一
    [Min(0)] public float guardStaminaCost = 10f;  // ガード成立時のスタミナコスト
    public Vector2 knockback = new Vector2(7f, 3f);

    [Header("Guard Interaction")]
    [Tooltip("true のとき、通常ガードを貫通（ガード中でも通常被弾扱い）")]
    public bool unblockable = false;
    [Tooltip("true のとき、ジャストガード成功なら無効化できる")]
    public bool justGuardable = true;

    [Header("Stun")]
    public bool causeStun = true;
    public float stunSeconds = 0.2f;

    [Header("Misc")]
    public LayerMask targetLayers;                   // 例: Player
    public string receiverMethod = "ReceiveHit";     // IHittableが無ければ SendMessage で呼ぶ

    Collider2D _col;

    void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;
        gameObject.SetActive(false); // デフォルトは無効（アニメイベント等でONにする）
    }

    // === ON/OFF ユーティリティ（従来） ===
    public void EnableHitbox() { gameObject.SetActive(true); }
    public void EnableHitboxUnblockable(bool on) { unblockable = on; gameObject.SetActive(true); }
    public void DisableHitbox() { gameObject.SetActive(false); }

    // === ★ 互換用ラッパー：他スクリプトが handHitbox.SetActive(..) を呼んでも通るように ===
    public void SetActive(bool on) { gameObject.SetActive(on); }
    // 必要ならプロパティ風にも対応
    public bool active
    {
        get => gameObject.activeSelf;
        set => gameObject.SetActive(value);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & targetLayers) == 0) return;

        // 攻撃者→被弾者 方向（左右判定に x 符号を用いる）
        Vector2 dir = (other.bounds.center - _col.bounds.center);
        Vector2 hitNormal = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;

        var info = new HitInfo
        {
            damage = Mathf.Max(0, damage),
            guardStaminaCost = Mathf.Max(0f, guardStaminaCost),
            knockback = knockback,
            hitNormal = hitNormal,

            causeStun = causeStun,
            stunSeconds = Mathf.Max(0f, stunSeconds),

            unblockable = unblockable,
            justGuardable = justGuardable
        };

        // IHittable 宛て優先
        var hittable = other.GetComponent<IHittable>();
        if (hittable != null) { hittable.ReceiveHit(info); return; }

        // フォールバック：SendMessage
        other.SendMessage(receiverMethod, info, SendMessageOptions.DontRequireReceiver);
    }
}
