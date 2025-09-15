using UnityEngine;

/// Guard�X�e�[�g�ɃA�^�b�`�B�˓��Řr���B���A���E�Ŗ߂��B
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
