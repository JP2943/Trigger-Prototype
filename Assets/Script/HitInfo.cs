using UnityEngine;

/// �U��������e���ɓn�����
public struct HitInfo
{
    public int damage;                 // �^�_���[�W
    public float guardStaminaCost;     // �K�[�h���̃X�^�~�i����

    // �ǉ��F�̂�����֘A
    public bool causeStun;             // true �̂Ƃ���e���[�V�����{�s���s�\�ɂ���
    public float stunSeconds;          // �X�^�����ԁi0�ȉ��Ȃ��e���̃f�t�H���g�j
    public Vector2 knockback;          // (X:���̏Ռ���, Y:������̏Ռ���)�BX�͍U�������ɉ����ā}�œK�p

    // �t�����i���o�Ȃǁj
    public Vector2 hitPoint;
    public Vector2 hitNormal;          // �U������e�҂̕����inormalized�j
    public GameObject source;
}

/// ��������������Ώۂ͍U�����󂯂���
public interface IHittable
{
    void ReceiveHit(in HitInfo hit);
}
