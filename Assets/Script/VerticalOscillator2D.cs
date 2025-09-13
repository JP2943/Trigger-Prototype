using UnityEngine;

/// <summary>
/// �^�[�Q�b�g���㉺�ɉ����ړ�������e�X�g�p�X�N���v�g�B
/// �����ʒu�𒆐S�ɁAamplitude [unit] �����㉺�� sine �ňړ����܂��B
/// Rigidbody2D ���t���Ă���� MovePosition�A������� Transform �ňړ����܂��B
/// </summary>
[DisallowMultipleComponent]
public class VerticalOscillator2D : MonoBehaviour
{
    [SerializeField] private float amplitude = 2f;     // �㉺�̐U�ꕝ�i���S����̋����j
    [SerializeField] private float frequency = 0.5f;   // 1�b������̉����񐔁iHz�j
    [SerializeField] private float phaseOffset = 0f;   // �����_�������������� 0�`2��

    private float startY;
    private Rigidbody2D rb;

    void Awake()
    {
        startY = transform.position.y;
        rb = GetComponent<Rigidbody2D>();
        // �������g��Ȃ��Ȃ� Rigidbody2D �͕s�v�B�t����Ȃ� Kinematic �����B
        if (rb && rb.bodyType == RigidbodyType2D.Dynamic)
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void FixedUpdate()
    {
        float t = Time.time;
        float y = startY + amplitude * Mathf.Sin((Mathf.PI * 2f * frequency) * t + phaseOffset);

        if (rb) rb.MovePosition(new Vector2(rb.position.x, y));
        else transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }

    // �G�f�B�^�őI�����Ɉړ��͈͂�\��
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 p = Application.isPlaying ? new Vector3(transform.position.x, startY, transform.position.z)
                                          : transform.position;
        Gizmos.DrawLine(p + Vector3.up * amplitude, p - Vector3.up * amplitude);
        Gizmos.DrawSphere(p + Vector3.up * amplitude, 0.05f);
        Gizmos.DrawSphere(p - Vector3.up * amplitude, 0.05f);
    }
}