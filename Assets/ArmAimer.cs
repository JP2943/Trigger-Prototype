using UnityEngine;
using UnityEngine.InputSystem; // 新Input System

public class ArmAimer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform armPivot;         // 肩の基準(ArmPivot)
    [SerializeField] private SpriteRenderer bodyRenderer; // 立ち絵(Body)

    [Header("Lock-on")]
    [SerializeField] private Key lockKey = Key.F;        // ロックオン切替キー
    [SerializeField] private string targetTag = "Enemy"; // 敵タグ
    [SerializeField] private float lockRadius = 50f;     // サーチ半径（ワールド単位）

    private Transform lockTarget;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb[lockKey].wasPressedThisFrame)
        {
            ToggleLockOn();
        }

        if (lockTarget != null)
        {
            // ターゲット方向に常に向ける（プレイヤーや敵が動いてもOK）
            Vector2 dir = (lockTarget.position - armPivot.position);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            armPivot.rotation = Quaternion.Euler(0f, 0f, angle);

            // もしターゲットが半径外/破棄されたら解除
            if (!lockTarget || dir.sqrMagnitude > lockRadius * lockRadius)
                lockTarget = null;
        }
        else
        {
            // 非ロックオン時は「体の向き」に合わせて腕を前方へ向けておく（任意）
            armPivot.rotation = Quaternion.Euler(0, 0, bodyRenderer.flipX ? 180f : 0f);
        }
    }

    private void ToggleLockOn()
    {
        if (lockTarget != null) { lockTarget = null; return; }

        // 近い敵をサーチ
        Transform nearest = null;
        float best = lockRadius * lockRadius;
        var enemies = GameObject.FindGameObjectsWithTag(targetTag);
        foreach (var e in enemies)
        {
            float d2 = (e.transform.position - armPivot.position).sqrMagnitude;
            if (d2 < best) { best = d2; nearest = e.transform; }
        }
        lockTarget = nearest;
    }
}