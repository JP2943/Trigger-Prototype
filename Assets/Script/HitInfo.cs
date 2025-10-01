using UnityEngine;

/// 攻撃側→被弾側に渡す情報
public struct HitInfo
{
    // ダメージ
    public int damage;                 // HP へのダメージ（整数）
    public float guardStaminaCost;     // ガード成立時に消費するスタミナ量

    // 反動
    public Vector2 knockback;          // インパルス量
    public Vector2 hitNormal;          // 攻撃者→被弾者 方向の法線（x符号で左右を判定）

    // ★ 追加：衝突点（任意で参照）
    public Vector2 hitPoint;           // ヒットしたワールド座標

    // ★ 追加：攻撃元（任意で参照：発射者/弾など）
    public GameObject source;          // このヒットの発生元

    // スタン
    public bool causeStun;             // true なら被弾硬直を与える
    public float stunSeconds;          // 硬直時間

    // ガード相互作用
    public bool unblockable;           // 通常ガードを貫通（=ガード中でも通常被弾）
    public bool justGuardable;         // ジャスト成立なら無効化できる
}

/// これを実装した対象は攻撃を受けられる
public interface IHittable
{
    void ReceiveHit(in HitInfo hit);
}
