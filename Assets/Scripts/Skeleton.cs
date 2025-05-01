using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.PlayerLoop;

public class Skeleton : MonoBehaviour, IDamageable
{
    [Header("기본 설정")]
    [SerializeField] float maxHp = 100f;
    [SerializeField] float currentHp;
    
    [Header("공격 설정")]
    [SerializeField] float attackRange = 1.5f;
    [SerializeField] float attackDamage = 20f;
    [SerializeField] float attackCooldown = 1f;
    [SerializeField] Weapon weapon;
    float attackTimer;
    bool isAttacking = false;
    
    public Transform player;
    NavMeshAgent agent;
    Animator animator;
    Collider collider;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        collider = GetComponent<Collider>();
    }

    void Start()
    {
        currentHp = maxHp;
    }

    private void Update()
    {
        animator.SetFloat("Speed", agent.velocity.magnitude);
        
        Debug.Log(transform.position + " " + player.position);
        
        if(Vector3.Distance(transform.position, player.position) < attackRange)
        {
            if (isAttacking)
            {
                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0f)
                {
                    isAttacking = false;
                }
            }
            else Attack();
        }
        else
        {
            ChasePlayer();
        }
    }

    void Attack()
    {  
        animator.SetTrigger("Attack");
        agent.isStopped = true;
        isAttacking = true;
        attackTimer = attackCooldown;
    }

    void ChasePlayer()
    {
        agent.isStopped = false;
        animator.SetFloat("Speed", agent.velocity.magnitude);
        agent.SetDestination(player.position);
    }
    
    public void TakeDamage(float damageAmount)
    {
        attackTimer = attackCooldown;
        Debug.Log("Skeleton Damage Taken: " + damageAmount);
        currentHp -= damageAmount;
        animator.SetTrigger("Hit");
        if (currentHp <= 0)
        {
            currentHp = 0;
            Die();
        }
    }
    
    void Die()
    {
        Debug.Log("Skeleton Died");
        animator.SetTrigger("Death");
        collider.enabled = false;
        agent.isStopped = true;
        Destroy(gameObject, 3f);
    }
    
    public void EnableWeaponCollider()
    {
        weapon.EnableDamageCollider();
    }
    
    public void DisableWeaponCollider()
    {
        weapon.DisableDamageCollider();
    }
}
