using UnityEngine;

/// 攻撃側→被弾側に渡す情報
public struct HitInfo
{
    public int damage;                 // 与ダメージ
    public float guardStaminaCost;     // ガード時のスタミナ消費

    // 追加：のけぞり関連
    public bool causeStun;             // true のとき被弾モーション＋行動不能にする
    public float stunSeconds;          // スタン時間（0以下なら被弾側のデフォルト）
    public Vector2 knockback;          // (X:横の衝撃量, Y:上方向の衝撃量)。Xは攻撃方向に応じて±で適用

    // 付随情報（演出など）
    public Vector2 hitPoint;
    public Vector2 hitNormal;          // 攻撃→被弾者の方向（normalized）
    public GameObject source;
}

/// これを実装した対象は攻撃を受けられる
public interface IHittable
{
    void ReceiveHit(in HitInfo hit);
}
