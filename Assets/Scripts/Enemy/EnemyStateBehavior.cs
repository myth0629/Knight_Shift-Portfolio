using UnityEngine;
using UnityEngine.AI;

public class EnemyStateBehavior : StateMachineBehaviour
{
    private EnemyController skeleton;
    private NavMeshAgent agent;
    
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        skeleton = animator.GetComponent<EnemyController>();
        agent = animator.GetComponent<NavMeshAgent>();
        
        skeleton.EnableWeaponCollider();
        
        skeleton.isAttacking = true;
        agent.isStopped = true;
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {   
        skeleton.DisableWeaponCollider();
        
        skeleton.isAttacking = false;
        agent.isStopped = false;
    }
}
