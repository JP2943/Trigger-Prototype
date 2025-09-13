using UnityEngine;

public class GuardVisualHook : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.GetComponentInParent<GuardVisualSwitcher>()?.SetGuardVisual(true);
    }
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.GetComponentInParent<GuardVisualSwitcher>()?.SetGuardVisual(false);
    }
}
