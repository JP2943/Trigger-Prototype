using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class AttackHitbox2D : MonoBehaviour
{
    [Header("Hit Values")]
    [Min(0)] public int damage = 10;               // HP�_���[�W�� int �ɓ���
    [Min(0)] public float guardStaminaCost = 10f;  // �K�[�h�������̃X�^�~�i�R�X�g
    public Vector2 knockback = new Vector2(7f, 3f);

    [Header("Guard Interaction")]
    [Tooltip("true �̂Ƃ��A�ʏ�K�[�h���ђʁi�K�[�h���ł��ʏ��e�����j")]
    public bool unblockable = false;
    [Tooltip("true �̂Ƃ��A�W���X�g�K�[�h�����Ȃ疳�����ł���")]
    public bool justGuardable = true;

    [Header("Stun")]
    public bool causeStun = true;
    public float stunSeconds = 0.2f;

    [Header("Misc")]
    public LayerMask targetLayers;                   // ��: Player
    public string receiverMethod = "ReceiveHit";     // IHittable��������� SendMessage �ŌĂ�

    Collider2D _col;

    void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;
        gameObject.SetActive(false); // �f�t�H���g�͖����i�A�j���C�x���g����ON�ɂ���j
    }

    // === ON/OFF ���[�e�B���e�B�i�]���j ===
    public void EnableHitbox() { gameObject.SetActive(true); }
    public void EnableHitboxUnblockable(bool on) { unblockable = on; gameObject.SetActive(true); }
    public void DisableHitbox() { gameObject.SetActive(false); }

    // === �� �݊��p���b�p�[�F���X�N���v�g�� handHitbox.SetActive(..) ���Ă�ł��ʂ�悤�� ===
    public void SetActive(bool on) { gameObject.SetActive(on); }
    // �K�v�Ȃ�v���p�e�B���ɂ��Ή�
    public bool active
    {
        get => gameObject.activeSelf;
        set => gameObject.SetActive(value);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & targetLayers) == 0) return;

        // �U���ҁ���e�� �����i���E����� x ������p����j
        Vector2 dir = (other.bounds.center - _col.bounds.center);
        Vector2 hitNormal = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;

        var info = new HitInfo
        {
            damage = Mathf.Max(0, damage),
            guardStaminaCost = Mathf.Max(0f, guardStaminaCost),
            knockback = knockback,
            hitNormal = hitNormal,

            causeStun = causeStun,
            stunSeconds = Mathf.Max(0f, stunSeconds),

            unblockable = unblockable,
            justGuardable = justGuardable
        };

        // IHittable ���ėD��
        var hittable = other.GetComponent<IHittable>();
        if (hittable != null) { hittable.ReceiveHit(info); return; }

        // �t�H�[���o�b�N�FSendMessage
        other.SendMessage(receiverMethod, info, SendMessageOptions.DontRequireReceiver);
    }
}
