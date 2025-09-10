using UnityEngine;

[RequireComponent(typeof(Animator))]
public class BossGorillaAttack : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private AttackHitbox2D handHitbox; // HandHitboxのコンポーネント
    [SerializeField] private string idleState = "Idle";
    [SerializeField] private string attackState = "Attack";

    private Animator _anim;

    void Awake() { _anim = GetComponent<Animator>(); }
    public void DoAttack() => _anim.Play(attackState, 0, 0f);

    // --- Animation Events ---
    public void AttackWindowStart()
    {
        if (handHitbox) handHitbox.SetActive(true);
    }
    public void AttackWindowEnd()
    {
        if (handHitbox) handHitbox.SetActive(false);
    }
}
