using UnityEngine;

/// Dash ステートにアタッチ。突入で腕を隠し、離脱で戻す。
public class DashVisualHook : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.GetComponentInParent<GuardVisualSwitcher>()?.SetHiddenByDash(true);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.GetComponentInParent<GuardVisualSwitcher>()?.SetHiddenByDash(false);
    }
}
