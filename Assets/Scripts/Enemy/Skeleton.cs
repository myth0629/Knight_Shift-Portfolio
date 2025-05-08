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
    [SerializeField] float attackDamage = 20f; // Init 함수 통해 Weapon에 넘겨줌
    [SerializeField] float attackCooldown = 1f;
    Weapon weapon;
    float attackTimer;
    public bool isAttacking = false;
    bool isDead = false;
    
    public Transform player;
    NavMeshAgent agent;
    Animator animator;

    private void Awake()
    {
        weapon = GetComponentInChildren<Weapon>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        currentHp = maxHp;
        weapon.Init(attackDamage); // Weapon에 공격 데미지 전달
    }

    private void Update()
    {
        if(isDead) return;
        
        animator.SetFloat("Speed", agent.velocity.magnitude);
        
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

    public void attackEnd()
    {
        isAttacking = false;
        agent.isStopped = false;
    }

    void ChasePlayer()
    {
        if(isAttacking) return;
        
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
        isDead = true;
        animator.SetTrigger("Death");
        GetComponent<Collider>().enabled = false;
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