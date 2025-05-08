using UnityEngine;
using UnityEngine.AI;

public class EnemyStateBehavior : StateMachineBehaviour
{
    private Skeleton skeleton;
    private NavMeshAgent agent;
    
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        skeleton = animator.GetComponent<Skeleton>();
        agent = animator.GetComponent<NavMeshAgent>();
        
        skeleton.isAttacking = true;
        agent.isStopped = true;
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {   
        skeleton.isAttacking = false;
        agent.isStopped = false;
    }
}
