using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class Bullet2D : MonoBehaviour
{
    [SerializeField] float speed = 20f;
    [SerializeField] float lifeTime = 3f;

    Rigidbody2D rb;

    void Awake() { rb = GetComponent<Rigidbody2D>(); }

    void OnEnable()
    {
        rb.gravityScale = 0f;
        rb.linearVelocity = (Vector2)(transform.right * speed); // ���[�J��+X�֒��i
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // TODO: �G���C���[���ŕ��򂵂ă_���[�W����
        Destroy(gameObject);
    }
}
