using UnityEngine;

/// 攻撃側→被弾側に渡す情報
public struct HitInfo
{
    public int damage;                 // 基本ダメージ
    public float guardStaminaCost;     // ガード時に消費させたいスタミナ量（攻撃ごとに可変）
    public Vector2 hitPoint;           // 命中座標（演出用）
    public Vector2 hitNormal;          // 命中方向（ノックバック等に）
    public GameObject source;          // 送信元（攻撃判定のGameObject）
}

/// これを実装した対象は攻撃を受けられる
public interface IHittable
{
    void ReceiveHit(in HitInfo hit);
}
