using UnityEngine;

public class EnemyShooter2D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform muzzle;      // 右向き(+X)が発射方向
    [SerializeField] private Bullet2D bulletPrefab; // Bullet_TestEnemy (Variant)
    [SerializeField] private Transform target;      // 未設定なら tag=Player を自動探索
    [SerializeField] private string targetTag = "Player";

    [Header("Fire")]
    [SerializeField] private float firstDelay = 0.5f;
    [SerializeField] private float fireInterval = 1.2f;
    [SerializeField, Tooltip("散り(度)。0なら真っ直ぐ")]
    private float spreadDeg = 0f;

    [Header("Aim")]
    [SerializeField] private bool aimContinuously = true; // 毎フレーム狙い続ける

    float timer;

    void OnEnable() { timer = Mathf.Max(0f, firstDelay); }

    void Update()
    {
        if (!muzzle || !bulletPrefab) return;

        // ターゲット自動取得
        if (!target)
        {
            var go = GameObject.FindGameObjectWithTag(targetTag);
            if (go) target = go.transform;
        }

        // 狙う（弾は+Xへ飛ぶので、muzzle.rightをターゲットへ向ける）
        if (aimContinuously && target)
        {
            Vector2 dir = (target.position - muzzle.position);
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            muzzle.rotation = Quaternion.Euler(0f, 0f, ang);
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Fire();
            timer += fireInterval;
        }
    }

    void Fire()
    {
        Quaternion rot = muzzle.rotation;
        if (spreadDeg > 0f)
        {
            float half = spreadDeg * 0.5f;
            rot *= Quaternion.Euler(0, 0, Random.Range(-half, half));
        }
        Instantiate(bulletPrefab, muzzle.position, rot);
    }

    void OnDrawGizmosSelected()
    {
        if (muzzle)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(muzzle.position, muzzle.position + muzzle.right * 0.8f);
        }
    }
}
