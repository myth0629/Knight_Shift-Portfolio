using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.PlayerLoop;

public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("기본 설정")]
    [SerializeField] float maxHp = 100f;
    [SerializeField] float currentHp;
    [SerializeField] float recognitionRange = 10f; // 플레이어를 인식하는 범위
    [SerializeField] int dropGold = 10; // 적 처치 시 드랍되는 골드 양
    
    [Header("공격 설정")]
    [SerializeField] float attackRange = 1.5f;
    [SerializeField] float attackDamage = 20f; // Init 함수 통해 Weapon에 넘겨줌
    [SerializeField] float attackCooldown = 1f;
    [SerializeField] bool canAttack = true;
    
    Weapon weapon;
    PlayerStatus player;
    
    float attackTimer;
    public bool isAttacking = false;
    
    bool isDead = false;
    
    public Transform playerTransform;
    NavMeshAgent agent;
    Animator animator;

    private void Awake()
    {
        weapon = GetComponentInChildren<Weapon>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        player = FindFirstObjectByType<PlayerStatus>();
    }

    void Start()
    {
        currentHp = maxHp;
        weapon.Init(attackDamage); // Weapon에 공격 데미지 전달

        if (playerTransform == null)
        {
            string playerPositionName = "PlayerCameraRoot";
            playerTransform = GameObject.Find(playerPositionName).transform;
        }
    }

    private void Update()
    {
        if(isDead || player.isDead) return;
        
        attackTimer += Time.deltaTime;
        
        animator.SetFloat("Speed", agent.velocity.magnitude);
        
        if(Vector3.Distance(transform.position, playerTransform.position) < attackRange)
        {
            Attack();
        }
        else if(Vector3.Distance(transform.position, playerTransform.position) < recognitionRange)
        {
            ChasePlayer();
        }
        
        AttackCooldown();
    }

    void AttackCooldown()
    {
        if (attackTimer >= attackCooldown)
        {
            canAttack = true;
        }
    }

    void Attack()
    {  
        if(!canAttack) return;
        
        animator.SetTrigger("Attack");
        agent.isStopped = true;
        canAttack = false;
        attackTimer = 0;
        AttackCooldown();
    }

    void ChasePlayer()
    {
        if(isAttacking) return;
        
        agent.isStopped = false;
        animator.SetFloat("Speed", agent.velocity.magnitude);
        agent.SetDestination(playerTransform.position);
    }
    
    public void TakeDamage(float damageAmount)
    {
        attackTimer = attackCooldown;
        Debug.Log("Skeleton Damage Taken: " + damageAmount);
        currentHp -= damageAmount;
        attackTimer = 1; // 피격 시 공격 쿨타임 증가
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