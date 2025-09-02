using UnityEngine;
using UnityEngine.InputSystem; // �VInput System

public class ArmAimer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform armPivot;         // ���̊(ArmPivot)
    [SerializeField] private SpriteRenderer bodyRenderer; // �����G(Body)

    [Header("Lock-on")]
    [SerializeField] private Key lockKey = Key.F;        // ���b�N�I���ؑփL�[
    [SerializeField] private string targetTag = "Enemy"; // �G�^�O
    [SerializeField] private float lockRadius = 50f;     // �T�[�`���a�i���[���h�P�ʁj

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
            // �^�[�Q�b�g�����ɏ�Ɍ�����i�v���C���[��G�������Ă�OK�j
            Vector2 dir = (lockTarget.position - armPivot.position);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            armPivot.rotation = Quaternion.Euler(0f, 0f, angle);

            // �����^�[�Q�b�g�����a�O/�j�����ꂽ�����
            if (!lockTarget || dir.sqrMagnitude > lockRadius * lockRadius)
                lockTarget = null;
        }
        else
        {
            // �񃍃b�N�I�����́u�̂̌����v�ɍ��킹�Ęr��O���֌����Ă����i�C�Ӂj
            armPivot.rotation = Quaternion.Euler(0, 0, bodyRenderer.flipX ? 180f : 0f);
        }
    }

    private void ToggleLockOn()
    {
        if (lockTarget != null) { lockTarget = null; return; }

        // �߂��G���T�[�`
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