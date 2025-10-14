using StarterAssets;
using UnityEngine;

public class AttackStateBehaviour : StateMachineBehaviour
{
    private ThirdPersonController thirdPersonController;
    Rigidbody rb;
    
    [Tooltip("애니메이션의 몇 % 지점에서 이동을 허용할지 (0.0 ~ 1.0)")]
    [SerializeField] private float moveUnlockThreshold = 0.8f;
    
    [Tooltip("연속 공격 중에는 이동을 막을지 여부")]
    [SerializeField] private bool blockMovementDuringCombo = true;
    
    private bool hasUnlockedMovement = false;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        thirdPersonController = animator.GetComponent<ThirdPersonController>();
        rb = animator.GetComponent<Rigidbody>();

        animator.SetBool("isAttacking", true);
        
        // 상태 진입 시 항상 이동 막기 (연속 공격 포함)
        thirdPersonController.canMove = false;
        hasUnlockedMovement = false;
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 연속 공격 체크
        bool isInCombo = animator.GetBool("isAttacking");
        
        if (blockMovementDuringCombo && isInCombo)
        {
            // 연속 공격 중이면 이동 계속 막기
            if (hasUnlockedMovement)
            {
                thirdPersonController.canMove = false;
                hasUnlockedMovement = false;
            }
        }
        else
        {
            // 연속 공격이 아니면 임계값 체크
            if (!hasUnlockedMovement && stateInfo.normalizedTime >= moveUnlockThreshold)
            {
                thirdPersonController.canMove = true;
                hasUnlockedMovement = true;
            }
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // isAttacking이 false일 때만 이동 허용 (연속 공격 종료)
        bool isStillAttacking = animator.GetBool("isAttacking");
        
        if (!isStillAttacking)
        {
            thirdPersonController.canMove = true;
        }
        else
        {
            // 연속 공격 중이면 이동 막기
            thirdPersonController.canMove = false;
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
            
        }
    }
}