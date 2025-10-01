using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BossStunController : MonoBehaviour
{
    [Header("Links")]
    public Animator animator;                       // Boss �� Animator
    public Rigidbody2D rb;                          // 2D�������g���Ă���Ȃ犄��
    [Tooltip("Stun����OFF�ɂ���X�N���v�g�iAI/�U�����䓙�j")]
    public List<MonoBehaviour> disableWhileStunned = new();
    [Tooltip("Stun����OFF�ɂ���U������")]
    public List<AttackHitbox2D> hitboxesToDisable = new();

    [Header("Animator Params")]
    public string stunTrigger = "Stun";             // Any��Stun
    public string recoverTrigger = "Recover";       // Stun��Idle

    [Header("Timing")]
    [Min(0.1f)] public float stunSeconds = 3.0f;    // �����Stun�b
    public bool zeroVelocityOnEnter = true;         // �������u�Ԃɒ�~
    public bool invulnerableDuringStun = false;     // �K�v�Ȃ�Stun�����G

    public bool IsStunned { get; private set; }
    Coroutine _running;
    float _stunUntil;

    void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>�X�^�~�i0�ȂǂŊO������Ă�</summary>
    public void BeginStun(float seconds = -1f)
    {
        if (IsStunned)
        {                      // ���ł�Stun���Ȃ�㏑������
            if (seconds > 0f) _stunUntil = Mathf.Max(_stunUntil, Time.time + seconds);
            return;
        }
        if (_running != null) StopCoroutine(_running);
        _running = StartCoroutine(StunRoutine(seconds > 0 ? seconds : stunSeconds));
    }

    IEnumerator StunRoutine(float dur)
    {
        IsStunned = true;
        _stunUntil = Time.time + dur;

        // Animator�g���K�[
        if (animator)
        {
            animator.ResetTrigger(recoverTrigger);
            animator.SetTrigger(stunTrigger);
        }

        // �������~�߂�/�U���𖳌���
        foreach (var mb in disableWhileStunned) if (mb) mb.enabled = false;
        foreach (var hb in hitboxesToDisable) if (hb) hb.SetActive(false);
        if (rb && zeroVelocityOnEnter) rb.linearVelocity = Vector2.zero;

        // �i�C�Ӂj���G���t���O�������Ă���Η��Ă�
        var hp = GetComponent<PlayerHealthGuard>(); // ��F�{�X���̔�e����ɍ��킹�ĕύX
        if (invulnerableDuringStun && hp != null)
        {
            // hp.SetInvulnerable(true); // �v���W�F�N�g�ɍ��킹�Ď�������Ă���Ύg�p
        }

        // �o�ߑ҂��iStun�̉����ɂ��Ή��j
        while (Time.time < _stunUntil)
            yield return null;

        // ����
        if (animator)
        {
            animator.ResetTrigger(stunTrigger);
            animator.SetTrigger(recoverTrigger);   // Idle �ɖ߂�
        }
        foreach (var mb in disableWhileStunned) if (mb) mb.enabled = true;
        foreach (var hb in hitboxesToDisable) if (hb) hb.SetActive(false); // �O�̂���OFF�̂܂�

        if (invulnerableDuringStun && hp != null)
        {
            // hp.SetInvulnerable(false);
        }

        IsStunned = false;
        _running = null;
    }
}
