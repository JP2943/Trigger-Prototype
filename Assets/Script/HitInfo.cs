using UnityEngine;

/// �U��������e���ɓn�����
public struct HitInfo
{
    public int damage;                 // ��{�_���[�W
    public float guardStaminaCost;     // �K�[�h���ɏ���������X�^�~�i�ʁi�U�����Ƃɉρj
    public Vector2 hitPoint;           // �������W�i���o�p�j
    public Vector2 hitNormal;          // ���������i�m�b�N�o�b�N���Ɂj
    public GameObject source;          // ���M���i�U�������GameObject�j
}

/// ��������������Ώۂ͍U�����󂯂���
public interface IHittable
{
    void ReceiveHit(in HitInfo hit);
}
