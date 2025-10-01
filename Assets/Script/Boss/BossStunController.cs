using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BossStunController : MonoBehaviour
{
    [Header("Links")]
    public Animator animator;                       // Boss の Animator
    public Rigidbody2D rb;                          // 2D物理を使っているなら割当
    [Tooltip("Stun中はOFFにするスクリプト（AI/攻撃制御等）")]
    public List<MonoBehaviour> disableWhileStunned = new();
    [Tooltip("Stun中はOFFにする攻撃判定")]
    public List<AttackHitbox2D> hitboxesToDisable = new();

    [Header("Animator Params")]
    public string stunTrigger = "Stun";             // Any→Stun
    public string recoverTrigger = "Recover";       // Stun→Idle

    [Header("Timing")]
    [Min(0.1f)] public float stunSeconds = 3.0f;    // 既定のStun秒
    public bool zeroVelocityOnEnter = true;         // 入った瞬間に停止
    public bool invulnerableDuringStun = false;     // 必要ならStun中無敵

    public bool IsStunned { get; private set; }
    Coroutine _running;
    float _stunUntil;

    void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>スタミナ0などで外部から呼ぶ</summary>
    public void BeginStun(float seconds = -1f)
    {
        if (IsStunned)
        {                      // すでにStun中なら上書き延長
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

        // Animatorトリガー
        if (animator)
        {
            animator.ResetTrigger(recoverTrigger);
            animator.SetTrigger(stunTrigger);
        }

        // 動きを止める/攻撃を無効化
        foreach (var mb in disableWhileStunned) if (mb) mb.enabled = false;
        foreach (var hb in hitboxesToDisable) if (hb) hb.SetActive(false);
        if (rb && zeroVelocityOnEnter) rb.linearVelocity = Vector2.zero;

        // （任意）無敵化フラグを持っていれば立てる
        var hp = GetComponent<PlayerHealthGuard>(); // 例：ボス側の被弾制御に合わせて変更
        if (invulnerableDuringStun && hp != null)
        {
            // hp.SetInvulnerable(true); // プロジェクトに合わせて実装されていれば使用
        }

        // 経過待ち（Stunの延長にも対応）
        while (Time.time < _stunUntil)
            yield return null;

        // 解除
        if (animator)
        {
            animator.ResetTrigger(stunTrigger);
            animator.SetTrigger(recoverTrigger);   // Idle に戻す
        }
        foreach (var mb in disableWhileStunned) if (mb) mb.enabled = true;
        foreach (var hb in hitboxesToDisable) if (hb) hb.SetActive(false); // 念のためOFFのまま

        if (invulnerableDuringStun && hp != null)
        {
            // hp.SetInvulnerable(false);
        }

        IsStunned = false;
        _running = null;
    }
}
