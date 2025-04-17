// EnemyAI.cs (추적 & 상태 관리)
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [SerializeField] float chaseRange = 10f;
    [SerializeField] float attackRange = 2f;
    [SerializeField] float attackDelay = 2f; // 공격 쿨다운 시간 (초)
    
    private NavMeshAgent agent;
    private Transform player;
    private Animator anim;
    private EnemyHealth health;
    private bool isAttacking = false; // 공격 중 상태 플래그
    
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        health = GetComponent<EnemyHealth>();
        player = GameObject.FindWithTag("Player").transform;
    }

    void Update()
    {
        if(health.IsDead) return;

        float distance = Vector3.Distance(transform.position, player.position);
        
        // 추적 범위 내 진입
        if(distance <= chaseRange)
        {
            agent.SetDestination(player.position);
            anim.SetBool("isWalking", agent.velocity.magnitude > 0.1f);

            // 공격 범위 진입
            if(distance <= attackRange)
            {
                // 쿨다운 로직 추가: 공격 중이 아닐 때만 공격
                if (!isAttacking) {
                    anim.SetTrigger("Attack");
                    isAttacking = true;
                    agent.isStopped = true;
                    StartCoroutine(AttackCooldown());
                }
            }
            else
            {
                agent.isStopped = false;
            }
        }
    }
    
    // 공격 쿨다운 코루틴
    IEnumerator AttackCooldown() {
        yield return new WaitForSeconds(attackDelay);
        isAttacking = false;
        
        // 공격 후 거리 재확인하여 범위 밖이면 이동 재개
        float currentDistance = Vector3.Distance(transform.position, player.position);
        if (currentDistance > attackRange) {
            agent.isStopped = false;
        }
    }
}
