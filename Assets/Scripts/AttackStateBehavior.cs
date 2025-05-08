using StarterAssets;
using UnityEngine;

public class AttackStateBehaviour : StateMachineBehaviour
{
    private ThirdPersonController thirdPersonController;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (thirdPersonController == null)
            thirdPersonController = animator.GetComponent<ThirdPersonController>();

        animator.SetBool("isAttacking", true); // 시작 시 항상 true

        // OnStateEnter마다 false로 강제로 막아줌
        thirdPersonController.canMove = false;
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (thirdPersonController == null)
            thirdPersonController = animator.GetComponent<ThirdPersonController>();

        // isAttacking이 false일 때만 이동 허용
        if (!animator.GetBool("isAttacking"))
        {
            thirdPersonController.canMove = true;
        }

        // 공격 관련 정리
        AttackController attackController = animator.GetComponent<AttackController>();
        if (attackController != null)
        {
            attackController.DisableWeaponCollider();
        }

        SkillController skillController = animator.GetComponent<SkillController>();
        if (skillController != null)
        {
            // 필요한 마무리 처리
        }
    }
}