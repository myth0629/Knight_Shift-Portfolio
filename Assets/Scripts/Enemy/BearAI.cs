using UnityEngine;
using UnityEngine.AI;

public class BearAI : MonoBehaviour, IDamageable
{
    [Header("기본 설정")]
    [SerializeField] float maxHp = 100f;
    private float currentHp;
    private bool isDead = false;
    private bool isHit = false; // hit 애니메이션 재생 중인지 여부

    [Header("공격 설정")]
    [SerializeField] float attackRange = 2.5f;
    [SerializeField] float biteDamage = 30f;
    [SerializeField] float rightPawDamage = 20f;
    [SerializeField] float leftPawDamage = 20f;
    [SerializeField] float attackCooldown = 1.5f;

    private float attackTimer = 0f;
    private bool isAttacking = false;

    private Transform player;
    private NavMeshAgent agent;
    private Animator animator;
    private Collider mainCollider;

    [Header("공격 판정 콜라이더")]
    [SerializeField] AttackCollider biteCollider;
    [SerializeField] AttackCollider rightPawCollider;
    [SerializeField] AttackCollider leftPawCollider;

    [Header("회전 설정")]
    [SerializeField] float rotationSpeed = 10f; // 플레이어를 바라보는 회전 속도

    private enum BearAttackType { Bite, RightPaw, LeftPaw }
    private BearAttackType currentAttackType;

    private float animSpeed = 0f; // 부드러운 애니메이션 전환용

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false; // 수동 회전 제어를 위해 비활성화
        animator = GetComponent<Animator>();
        mainCollider = GetComponent<Collider>();

        // 공격 콜라이더는 평소에는 비활성화
        if (biteCollider) biteCollider.gameObject.SetActive(false);
        if (rightPawCollider) rightPawCollider.gameObject.SetActive(false);
        if (leftPawCollider) leftPawCollider.gameObject.SetActive(false);
        
        // 각 콜라이더에 데미지 값 설정
        if (biteCollider) biteCollider.damageAmount = biteDamage;
        if (rightPawCollider) rightPawCollider.damageAmount = rightPawDamage;
        if (leftPawCollider) leftPawCollider.damageAmount = leftPawDamage;
    }

    void Start()
    {
        currentHp = maxHp;
        FindPlayer();
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
        else Debug.LogError("Player 태그가 없는 오브젝트를 찾을 수 없습니다.");
    }

    void Update()
    {
        if (isDead || player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // 플레이어 방향 벡터
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        dirToPlayer.y = 0;

        // 상태에 따라 회전 처리
        if (!isDead && !isHit)
        {
            // 공격 범위 내에서는 항상 플레이어를 바라본다
            Quaternion targetRotation = Quaternion.LookRotation(dirToPlayer);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        // 애니메이션 Speed 파라미터 부드럽게 처리
        float targetSpeed = (!agent.isStopped) ? agent.velocity.magnitude : 0f;
        animSpeed = Mathf.Lerp(animSpeed, targetSpeed, Time.deltaTime * 10f);
        animator.SetFloat("Speed", animSpeed);

        if (distance <= attackRange)
        {
            agent.isStopped = true; // 이동 중지

            if (isAttacking)
            {
                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0) isAttacking = false;
            }
            else
            {
                Attack();
            }
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
    }

    void Attack()
    {
        if (isHit || isAttacking) return;

        currentAttackType = ChooseAttackType();

        // 공격 애니메이션 트리거
        switch (currentAttackType)
        {
            case BearAttackType.Bite:
                animator.SetTrigger("Bite");
                break;
            case BearAttackType.RightPaw:
                animator.SetTrigger("AttackRight");
                break;
            case BearAttackType.LeftPaw:
                animator.SetTrigger("AttackLeft");
                break;
        }

        isAttacking = true;
        attackTimer = attackCooldown;
    }

    // 공격 타입 선택 로직 (여기선 랜덤)
    BearAttackType ChooseAttackType()
    {
        int r = Random.Range(0, 3);
        return (BearAttackType)r;
    }

    // 애니메이션 이벤트에서 호출
    public void EnableAttackCollider()
    {
        switch (currentAttackType)
        {
            case BearAttackType.Bite:
                if (biteCollider) biteCollider.gameObject.SetActive(true);
                break;
            case BearAttackType.RightPaw:
                if (rightPawCollider) rightPawCollider.gameObject.SetActive(true);
                break;
            case BearAttackType.LeftPaw:
                if (leftPawCollider) leftPawCollider.gameObject.SetActive(true);
                break;
        }
    }
    
    public void DisableAttackCollider()
    {
        if (biteCollider) biteCollider.gameObject.SetActive(false);
        if (rightPawCollider) rightPawCollider.gameObject.SetActive(false);
        if (leftPawCollider) leftPawCollider.gameObject.SetActive(false);
    }

    // 애니메이션 이벤트에서 호출 (공격 애니메이션 종료 시)
    public void OnAttackAnimationEnd()
    {
        isAttacking = false;
        // 공격이 끝나도 플레이어를 계속 바라보게 하려면 별도 처리 필요 없음
    }

    // 데미지 처리
    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHp -= damageAmount;
        Debug.Log($"{name} 피격! 남은 체력: {currentHp}");

        animator.SetTrigger("Hit");
        isHit = true; // Hit 상태 설정

        if (currentHp <= 0)
        {
            Die();
        }
    }

    // 애니메이션 이벤트에서 호출할 메서드 - Hit 애니메이션 종료 시 호출
    public void OnHitAnimationEnd()
    {
        isHit = false; // Hit 상태 해제
    }

    void Die()
    {
        isDead = true;
        animator.SetTrigger("Death");
        mainCollider.enabled = false;
        agent.isStopped = true;
        Destroy(gameObject, 3f);
    }
}
