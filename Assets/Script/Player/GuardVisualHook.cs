using UnityEngine;

/// Guardステートにアタッチ。突入で腕を隠し、離脱で戻す。
public class GuardVisualHook : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.GetComponentInParent<GuardVisualSwitcher>()?.SetHiddenByGuard(true);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.GetComponentInParent<GuardVisualSwitcher>()?.SetHiddenByGuard(false);
    }
}
