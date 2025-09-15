using UnityEngine;

/// Dash �X�e�[�g�ɃA�^�b�`�B�˓��Řr���B���A���E�Ŗ߂��B
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
