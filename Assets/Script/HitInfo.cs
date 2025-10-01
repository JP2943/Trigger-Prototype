using UnityEngine;

/// �U��������e���ɓn�����
public struct HitInfo
{
    // �_���[�W
    public int damage;                 // HP �ւ̃_���[�W�i�����j
    public float guardStaminaCost;     // �K�[�h�������ɏ����X�^�~�i��

    // ����
    public Vector2 knockback;          // �C���p���X��
    public Vector2 hitNormal;          // �U���ҁ���e�� �����̖@���ix�����ō��E�𔻒�j

    // �� �ǉ��F�Փ˓_�i�C�ӂŎQ�Ɓj
    public Vector2 hitPoint;           // �q�b�g�������[���h���W

    // �� �ǉ��F�U�����i�C�ӂŎQ�ƁF���ˎ�/�e�Ȃǁj
    public GameObject source;          // ���̃q�b�g�̔�����

    // �X�^��
    public bool causeStun;             // true �Ȃ��e�d����^����
    public float stunSeconds;          // �d������

    // �K�[�h���ݍ�p
    public bool unblockable;           // �ʏ�K�[�h���ђʁi=�K�[�h���ł��ʏ��e�j
    public bool justGuardable;         // �W���X�g�����Ȃ疳�����ł���
}

/// ��������������Ώۂ͍U�����󂯂���
public interface IHittable
{
    void ReceiveHit(in HitInfo hit);
}
