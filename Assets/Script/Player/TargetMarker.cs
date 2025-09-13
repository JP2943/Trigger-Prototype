using UnityEngine;

/// ���b�N�ΏۂɒǏ]����}�[�J�[
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class TargetMarker : MonoBehaviour
{
    [SerializeField] Vector3 worldOffset = new(0, 0.6f, 0); // ����ɏ����I�t�Z�b�g
    [SerializeField] bool keepFixedScreenSize = true;
    [SerializeField] float referenceOrthoSize = 5f; // �J�����̊�Y�[��
    [SerializeField] float referenceScale = 1f;     // ��X�P�[��
    [SerializeField] private SpriteRenderer sr;
    public void SetTint(Color c) { if (!sr) sr = GetComponent<SpriteRenderer>(); if (sr) sr.color = c; }

    Transform target;
    Camera cam;

    public void SetTarget(Transform t) => target = t;

    void Awake() => cam = Camera.main;

    void LateUpdate()
    {
        if (!target) { Destroy(gameObject); return; }

        // �ʒu�Ǐ]�i�e�ɂ��Ȃ��̂ŉ�]�̉e�����󂯂Ȃ��j
        transform.position = target.position + worldOffset;
        transform.rotation = Quaternion.identity;

        // �Y�[�����Ă������ڃT�C�Y���قڈ��ɕۂi2D�I���\�p�j
        if (keepFixedScreenSize && cam && cam.orthographic)
        {
            float k = cam.orthographicSize / referenceOrthoSize;
            transform.localScale = Vector3.one * (referenceScale * k);
        }
    }
}