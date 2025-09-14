using UnityEngine;

public class EnemyShooter2D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform muzzle;      // �E����(+X)�����˕���
    [SerializeField] private Bullet2D bulletPrefab; // Bullet_TestEnemy (Variant)
    [SerializeField] private Transform target;      // ���ݒ�Ȃ� tag=Player �������T��
    [SerializeField] private string targetTag = "Player";

    [Header("Fire")]
    [SerializeField] private float firstDelay = 0.5f;
    [SerializeField] private float fireInterval = 1.2f;
    [SerializeField, Tooltip("�U��(�x)�B0�Ȃ�^������")]
    private float spreadDeg = 0f;

    [Header("Aim")]
    [SerializeField] private bool aimContinuously = true; // ���t���[���_��������

    float timer;

    void OnEnable() { timer = Mathf.Max(0f, firstDelay); }

    void Update()
    {
        if (!muzzle || !bulletPrefab) return;

        // �^�[�Q�b�g�����擾
        if (!target)
        {
            var go = GameObject.FindGameObjectWithTag(targetTag);
            if (go) target = go.transform;
        }

        // �_���i�e��+X�֔�Ԃ̂ŁAmuzzle.right���^�[�Q�b�g�֌�����j
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
