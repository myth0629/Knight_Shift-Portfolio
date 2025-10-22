using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using EnemyAI;

public class HoundAI : MonoBehaviour, IDamageable
{
    [Header("기본 설정")]
    [SerializeField] public float maxHp = 100f;
    public float currentHp;
    private bool isDead = false;
    private bool isInvincible = false; // 무적 상태 플래그

    [SerializeField] int dropGold = 3000;
    
    // LockOnSystem이 사망 상태를 감지할 수 있도록 public 프로퍼티 제공
    public float CurrentHealth => currentHp;
    public bool IsDead => isDead;
    
    [Header("피격 사운드")]
    [SerializeField] private AudioClip[] hitSounds;
    [SerializeField] private float hitSoundVolume = 0.8f;

    [Header("공격 사운드")]
    [SerializeField] private AudioClip foxCrySound;
    [SerializeField] private AudioClip biteSound;
    [SerializeField] private AudioClip dashAttackSound;
    [SerializeField] private AudioClip leftAttackSound;
    [SerializeField] private AudioClip rightAttackSound;
    [SerializeField] private AudioClip tongueBiteSound;
    [SerializeField] private AudioClip jumpAttackSound;

    [SerializeField] private AudioClip spawnSound;
    [SerializeField] private AudioClip deathSound;

    [Header("인카운터 설정")]
    [SerializeField] private float encounterRange = 20f; // 플레이어 감지 및 조우 사운드 재생 범위
    private bool hasPlayedEncounterSound = false; // 조우 사운드 재생 여부
    
    private AudioSource audioSource; 

    [Header("보스 설정")]
    [SerializeField] bool isBoss = true;

    [Header("보스 UI")]
    [SerializeField] private SimpleBossHealthBar bossHealthBar;
    [SerializeField] private string bossName = "고대의 수호자"; // 한국어 이름
    private bool isVisible = false; // 체력바 표시 여부 추가
    
    [Header("이동 설정")]
    [SerializeField] float walkSpeed = 2f;
    [SerializeField] float runSpeed = 5f;
    [SerializeField] float runThreshold = 6f;

    [Header("공격 설정")]
    [SerializeField] float attackRange = 3.2f;
    [SerializeField] float closeRange = 1.8f;
    [SerializeField] float jumpAttackRange = 4.5f;
    [SerializeField] float optimalAttackDistance = 2.8f;
    [SerializeField] float leftPawDamage = 5f;
    [SerializeField] float rightPawDamage = 7f;
    [SerializeField] float lickBiteDamage = 10f;
    [SerializeField] float jumpAttackDamage = 20f;
    [SerializeField] float attackCooldown = 1.5f;
    [SerializeField] float jumpAttackRecoveryTime = 2f;
    [SerializeField] float rangeBuffer = 0.3f; // 공격 범위 버퍼

    [Header("점프 공격 설정 (NavMesh 전용)")]
    [SerializeField] float jumpSpeed = 8f;
    [SerializeField] float jumpDuration = 1f;
    private Vector3 jumpTargetPosition;
    private bool isJumpingToTarget = false;
    private float jumpTimer = 0f;
    private bool hasFallingTriggered = false;
    [Header("공격 쿨다운 설정")]
    [SerializeField] private float chargeAttackCooldown = 5f;    // 돌진 쿨다운
    [SerializeField] private float projectileAttackCooldown = 6f; // 투사체 쿨다운
    private float chargeAttackTimer = 0f;
    private float projectileAttackTimer = 0f;

    [Header("영혼 화살비 (Spirit Arrow Rain) 설정 - 2페이즈 원거리")]
    [SerializeField] private GameObject spiritArrowPrefab; // 화살비용 투사체 프리팹
    [SerializeField] private int spiritArrowCount = 5; // 발사할 투사체 개수
    [SerializeField] private float spiritArrowSpreadAngle = 45f; // 부채꼴 각도
    [SerializeField] private float spiritArrowSpeed = 10f;
    [SerializeField] private float spiritArrowDamage = 4f;
    [SerializeField] private float spiritArrowCooldown = 12f;
    private bool isFiringSpiritArrows = false;
    private float spiritArrowTimer = 0f;

    [Header("점프 공격 쿨다운")]
    [SerializeField] private float jumpAttackCooldown = 4f; // 점프 공격 쿨다운 시간
    private float jumpAttackTimer = 0f; // 점프 공격 타이머

    [Header("데미지 방지 설정")]
    [SerializeField] float damageImmunityTime = 0.5f;
    private float lastDamageTime = -1f;

    [Header("자연스러운 충돌 방지 설정")]
    [SerializeField] float collisionAvoidanceRadius = 1.0f;
    [SerializeField] float avoidanceForce = 1.5f;
    [SerializeField] float smoothAvoidance = 2f;
    [SerializeField] float minSeparationDistance = 0.9f;
    
    private Vector3 avoidanceDirection = Vector3.zero;
    private bool isAvoiding = false;

    [Header("투사체 공격 설정")]
    [SerializeField] GameObject projectileParticlePrefab; // 투사체 파티클 프리팹
    [SerializeField] int projectileCount = 3;
    [SerializeField] float projectileSpeed = 12f;
    [SerializeField] float projectileInterval = 0.4f;
    [SerializeField] float projectileDamage = 5f; // 기존 frontPawDamage와 동일

    bool isProjectileAttacking = false;

    [Header("돌진 공격 설정")]
    [SerializeField] float chargeSpeed = 12f;        // 속도 조정
    [SerializeField] float chargeDamage = 13f;       // 데미지 증가
    [SerializeField] float chargeDistance = 6f;      // 거리 조정
    [SerializeField] float chargeHitRadius = 2.5f;   // 히트 범위 증가
    [SerializeField] float chargePreparationTime = 0.5f; // 준비 시간
    [SerializeField] GameObject chargeParticlesPrefab;

    bool isCharging = false;



    [Header("공격 방향 체크 설정")]
    [SerializeField] float attackAngleThreshold = 30f;
    [SerializeField] float rotationBeforeAttack = 4f;
    [SerializeField] float maxRotationTime = 2f;
    
    private bool isRotatingToAttack = false;
    private float rotationTimer = 0f;

    [Header("행동 확률 설정")]
    [SerializeField] float retreatProbability = 0.05f;
    [SerializeField] float stayAndAttackProbability = 0.95f;
    [SerializeField] float aggressiveModeProbability = 0.4f;
    [SerializeField] float consecutiveAttackChance = 0.9f;
    [SerializeField] float feintAttackChance = 0.1f;

    private float lastRetreatTime = 0f;
    [SerializeField] float retreatCooldown = 8f;

    [Header("근거리 행동 확률")]
    [SerializeField] float closeRangeRetreatProbability = 0.08f;

    private bool isAggressiveMode = false;
    private int consecutiveAttacks = 0;
    private int maxConsecutiveAttacks = 3;

    private float attackTimer = 0f;
    private float jumpRecoveryTimer = 0f;
    private bool isAttacking = false;
    private bool isJumping = false;
    private bool isBackingAway = false;
    private bool isRecoveringFromJump = false;
    private bool isRetreatingForAttack = false;

    [Header("레이어 관리")]
    [SerializeField] float upperBodyLayerWeight = 1f;
    private int upperBodyLayerIndex;

    private Transform player;
    private NavMeshAgent agent;
    private Animator animator;
    private Collider mainCollider;
    private Rigidbody playerRb;

    [Header("페이즈 시스템")]
    [SerializeField] public bool isPhase2 = false;
    [SerializeField] float phase2HealthThreshold = 0.5f;
    [SerializeField] float phase2SpeedMultiplier = 1.3f;
    [SerializeField] float phase2AttackSpeedMultiplier = 0.8f;

    [Header("페이즈 전환 설정")]
    [SerializeField] public bool isPhaseTransitionTriggered = false; // 전환 트리거 여부
    [SerializeField] GameObject phase2AuraPrefab; // 오라 파티클 프리팹
    [SerializeField] float auraEffectDuration = 3f; // 오라 지속 시간

    private bool hasTriggeredTransitionBarrier = false; // 전환용 배리어 실행 여부
    private float originalWalkSpeed;
    private float originalRunSpeed;
    private float originalAttackCooldown;
    [Header("2페이즈 데미지 배율")]
    [SerializeField] float phase2DamageMultiplier = 1.2f;  // 2페이즈 데미지 증가율

    // 패턴별 확률 조정
    [Header("2페이즈 패턴 확률")]
    [SerializeField] float phase2ProjectileChance = 0.25f;   // 30% 투사체
    [SerializeField] float phase2ChargeChance = 0.15f;       // 20% 돌진
    [SerializeField] float phase2MeleeChance = 0.6f;        // 50% 근접

    [Header("사운드 매니저")]
    private HoundSoundManager soundManager;


    [Header("공격 판정 콜라이더")]
    [SerializeField] AttackCollider leftPawCollider;
    [SerializeField] AttackCollider rightPawCollider;
    [SerializeField] AttackCollider lickBiteCollider;
    [SerializeField] AttackCollider jumpAttackCollider;

    [Header("플레이어 타입 설정")]
    [SerializeField] bool playerUsesCharacterController = true;
    [SerializeField] float characterControllerPushForce = 5f;

    private CharacterController playerCharacterController;

    [Header("자연스러운 회전 설정")]
    [SerializeField] float rotationSpeed = 2f;
    [SerializeField] bool useNavMeshRotation = true;
    [SerializeField] float closeFacingDistanceOverride = 0.6f; // 아주 근접 시 각도 무시
    
    private Quaternion targetRotation;

    [Header("후퇴 설정")]
    [SerializeField] float backAwayDuration = 1.5f;

    [Header("후퇴 시 오로라 블라스트 공격 설정")]
    [SerializeField] GameObject auroraEffectPrefab;
        [SerializeField] Transform mouthTransform;
    
    [SerializeField] float retreatDuration = 2f;

    
    private GameObject currentAuroraEffect;

    [Header("2페이즈 전환 패턴 (바닥 물기)")]
    [SerializeField] private GameObject houndShieldPrefab; // 2페이즈 전환 시 사용할 쉴드 프리팹
    private GameObject houndShield; // 쉴드 인스턴스
    [SerializeField] private GameObject floorBitePrefab; // 바닥에서 솟아나는 물기 이펙트 프리팹
    [SerializeField] private float floorBiteDamage = 13f;
    [SerializeField] private float floorBiteDamageDelay = 6.0f; // 장판 생성 후 실제 데미지까지의 딜레이
    [SerializeField] private float floorBiteInterval = 6.0f; // 물기 공격 간격
    [SerializeField] private int floorBiteCount = 3; // 물기 공격 횟수

    [Header("점프 공격 범위 데미지 설정")]
    [SerializeField] GameObject jumpAreaEffectPrefab;
    [SerializeField] float jumpAreaRadius = 4f;
    [SerializeField] float jumpAreaDamage = 35f;
    [SerializeField] float jumpAreaEffectDuration = 2f;

    private float backAwayTimer = 0f;
    PlayerDataManager playerData;
    private enum HoundAttackType
    {
         LeftPaw, RightPaw, LickBite, JumpAttack
    }
    private HoundAttackType currentAttackType;

    void ForceRetreatWithProjectileAttack()
    {
        if (isProjectileAttacking)
        {
            Debug.Log("이미 투사체 공격 중 - 무시");
            return;
        }

        isAttacking = false;
        isJumping = false;
        isRecoveringFromJump = false;
        isRotatingToAttack = false;

        SafeStopAgent();
        DisableAttackCollider();

        isBackingAway = true;
        backAwayTimer = retreatDuration;

        lastRetreatTime = Time.time;

        Debug.Log("R키 - 2초 후퇴 후 제자리 8초 블라스트!");

        StartCoroutine(RetreatThenProjectileAttack());
    }
    public void PerformChargeAttack()
    {
        if (isPhaseTransitionTriggered) return; // 페이즈 전환 중에는 실행 방지

        if (chargeAttackTimer > 0)
        {
            // 돌진이 불가능할 경우 기본 공격
            PerformMeleeAttack();
            Debug.Log($"돌진 쿨다운 중 (남은 시간: {chargeAttackTimer:F1}초) - 기본 공격으로 대체");
            return;
        }
        if (isCharging) return;

        // Behavior Tree 사용 시 공격 상태 설정
        if (useBehaviorTree && houndBehaviorTree != null)
        {
            houndBehaviorTree.SetAttacking(true);
        }

        StartCoroutine(ChargeAttackPattern());
    chargeAttackTimer = chargeAttackCooldown;
        Debug.Log($"돌진 공격 실행 - 다음 돌진까지 {chargeAttackCooldown}초 대기");
}

    IEnumerator ChargeAttackPattern()
    {
        isCharging = true;
        PlaySound(dashAttackSound);
        SafeStopAgent();

        Vector3 chargeDirection = (player.position - transform.position).normalized;
        chargeDirection.y = 0;

        transform.rotation = Quaternion.LookRotation(chargeDirection);

        GameObject chargeParticles = null;
        if (chargeParticlesPrefab != null)
        {
            chargeParticles = Instantiate(chargeParticlesPrefab, transform.position, transform.rotation);
            chargeParticles.transform.SetParent(transform);
        }
        SetAnimationParameters(0f, 1f, 1f); // 전방 달리기
        yield return new WaitForSeconds(0.5f); // 준비 시간

        Debug.Log("돌진 시작!");

        float chargedDistance = 0f;
        bool hasHitPlayer = false;

        while (chargedDistance < chargeDistance && isCharging)
        {
            // 돌진 이동
            float moveStep = chargeSpeed * Time.deltaTime;
            transform.position += chargeDirection * moveStep;
            chargedDistance += moveStep;

            // 플레이어 타격 체크
            if (!hasHitPlayer)
            {
                Collider[] hitColliders = Physics.OverlapSphere(transform.position, chargeHitRadius);
                foreach (Collider hitCollider in hitColliders)
                {
                    if (hitCollider.CompareTag("Player"))
                    {
                        var damageable = hitCollider.GetComponent<IDamageable>();
                        if (damageable != null)
                        {
                            damageable.TakeDamage(chargeDamage);
                            hasHitPlayer = true;
                            Debug.Log($"돌진 데미지 적용! {chargeDamage} 데미지");

                            // 플레이어 밀어내기
                            if (playerCharacterController != null)
                            {
                                Vector3 knockbackDir = chargeDirection;
                                StartCoroutine(ApplyKnockback(hitCollider.transform, knockbackDir, 10f, 0.2f));
                            }
                        }
                        break;
                    }
                }
            }

            yield return null;
        }

        // 돌진 종료
        SetAnimationParameters(0f, 0f, 0f);
        isCharging = false;
        SafeResumeAgent();

        // Behavior Tree 사용 시 공격 상태 해제
        if (useBehaviorTree && houndBehaviorTree != null)
        {
            houndBehaviorTree.SetAttacking(false);
        }

        if (chargeParticles != null)
        {
            Destroy(chargeParticles, 1f);
        }

        yield return new WaitForSeconds(1.5f); // 회복 시간
        Debug.Log("돌진 공격 완료!");
    }
    // 플레이어 밀어내기 효과
    IEnumerator ApplyKnockback(Transform target, Vector3 direction, float force, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            target.position += direction * force * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    void SafeStopAgent()
    {
        if (agent != null && agent.isOnNavMesh && agent.enabled)
        {
            agent.isStopped = true;
        }
    }

    void SafeResumeAgent()
    {
        if (agent != null && agent.isOnNavMesh && agent.enabled)
        {
            agent.isStopped = false;
        }
    }

    void SafeSetDestination(Vector3 destination)
    {
        if (agent != null && agent.isOnNavMesh && agent.enabled)
        {
            agent.SetDestination(destination);
        }
    }

    void SafeWarpAgent(Vector3 position)
    {
        if (agent != null && agent.enabled)
        {
            agent.Warp(position);
        }
    }

    void SafeSetSpeed(float speed)
    {
        if (agent != null && agent.isOnNavMesh && agent.enabled)
        {
            agent.speed = speed;
        }
    }

    bool IsLookingAtPlayer()
    {
        if (player == null) return false;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        float dist = toPlayer.magnitude;
        if (dist < 0.01f) return false;

        // 아주 근접하면 각도 무시하고 true 처리
        if (dist <= closeFacingDistanceOverride) return true;

        Vector3 dir = toPlayer / dist; // 정규화
        float angle = Vector3.Angle(transform.forward, dir);
        return angle <= attackAngleThreshold;
    }

    void RotateTowardsPlayer()
    {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        dirToPlayer.y = 0;
        
        if (dirToPlayer.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dirToPlayer);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                targetRotation, 
                rotationBeforeAttack * Time.deltaTime
            );
        }
    }
    void StartPhase2WithAura()
{
    Debug.Log("=== 오라 파티클과 함께 페이즈 2 시작! ===");
    
    // 오라 파티클 생성
    if (phase2AuraPrefab != null)
    {
        GameObject auraEffect = Instantiate(phase2AuraPrefab, transform.position, Quaternion.identity);
        auraEffect.transform.SetParent(transform);
        
        // 오라가 계속 따라다니도록 설정
        StartCoroutine(MaintainAuraEffect(auraEffect));
    }
    soundManager?.PlayPhase2TransitionSound();
    // 페이즈 2 활성화
    ActivatePhase2();
    
    isPhaseTransitionTriggered = false;
}

IEnumerator MaintainAuraEffect(GameObject auraEffect)
{
    
    while (auraEffect != null && !isDead)
    {
        // 오라가 하운드를 따라다니도록
        auraEffect.transform.position = transform.position;
        yield return null;
    }
    
    // 죽을때만 제거
    if (auraEffect != null)
    {
        Destroy(auraEffect);
    }
    
    Debug.Log("페이즈 2 오라 효과 종료");
}

    void ActivatePhase2()
    {
        isPhase2 = true;
        
        // 속도 증가
        walkSpeed = originalWalkSpeed * phase2SpeedMultiplier;
        runSpeed = originalRunSpeed * phase2SpeedMultiplier;
        attackCooldown = originalAttackCooldown * phase2AttackSpeedMultiplier;
        
        if (agent != null)
        {
            agent.speed = walkSpeed;
        }
        // 데미지 증가
        UpdatePhase2Damages();
        Debug.Log($"=== 페이즈 2 활성화 완료! === 이동속도: {walkSpeed:F1}, 공격쿨다운: {attackCooldown:F1}");
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // NavMeshAgent 충돌 방지 설정 강화
        agent.radius = 0.8f;                    // 약간 줄임
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = 50;           // 중간 우선순위

        // 정지 거리 설정으로 끼임 방지
        agent.stoppingDistance = 1.5f;

        
        if (useNavMeshRotation)
        {
            agent.updateRotation = true;
            agent.angularSpeed = 120f;
        }
        else
        {
            agent.updateRotation = false;
        }
        playerData = FindFirstObjectByType<PlayerDataManager>();
        animator = GetComponent<Animator>();
        mainCollider = GetComponent<Collider>();

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            DestroyImmediate(rb);
            Debug.Log("Rigidbody 제거됨 - NavMesh 전용 모드");
        }

        upperBodyLayerIndex = animator.GetLayerIndex("Upper Body Layer");
        if (upperBodyLayerIndex == -1)
        {
            Debug.LogError("Upper Body Layer를 찾을 수 없습니다!");
        }

        InitializeAttackColliders();
        originalWalkSpeed = walkSpeed;
        originalRunSpeed = runSpeed;
        originalAttackCooldown = attackCooldown;
    }

    void InitializeAttackColliders()
    {
        var colliders = new AttackCollider[] 
        { 
             leftPawCollider, rightPawCollider, 
            lickBiteCollider, jumpAttackCollider 
        };
        
        var damages = new float[] 
        { 
             leftPawDamage, rightPawDamage, 
            lickBiteDamage, jumpAttackDamage 
        };

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].gameObject.SetActive(false);
                // 수정된 SetDamageSource 호출
                colliders[i].SetDamageSource(this, damages[i]);
            }
        }
    }

    void Start()
    {
        currentHp = maxHp;
        FindPlayer();
        
        if (upperBodyLayerIndex != -1)
        {
            animator.SetLayerWeight(upperBodyLayerIndex, upperBodyLayerWeight);
        }
        if (bossHealthBar == null)
            bossHealthBar = FindFirstObjectByType<SimpleBossHealthBar>();
        if (bossHealthBar != null)
        {
            bossHealthBar.ShowBossHealthBar(bossName, maxHp, currentHp);
            isVisible = true;
            Debug.Log("하운드 씬 로드 완료! 체력바 즉시 표시");
        }
        soundManager = GetComponent<HoundSoundManager>();
        DisableAttackCollider();

        // Behavior Tree 초기화
        if (useBehaviorTree && houndBehaviorTree == null)
        {
            houndBehaviorTree = GetComponent<HoundBehaviorTree>();
        }
        
        // AudioSource 초기화 (피격 사운드용)
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D 사운드
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.maxDistance = 40f;
            audioSource.volume = 0.5f;
        }
        
        // 피격 판정 초기 상태 확인
        Debug.Log($"[하운드 초기화] HP: {currentHp}/{maxHp}, 무적: {isInvincible}, 콜라이더 활성: {(mainCollider != null ? mainCollider.enabled : false)}");
    }

    void FindPlayer()
{
    GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
    if (playerObj != null) 
    {
        player = playerObj.transform;
        
        // CharacterController 확인
        playerCharacterController = playerObj.GetComponent<CharacterController>();
        if (playerCharacterController != null)
        {
            Debug.Log("플레이어 CharacterController 감지됨 - OverlapSphere 방식 사용");
        }
        else
        {
            playerRb = playerObj.GetComponent<Rigidbody>();
            if (playerRb == null)
            {
                Debug.LogWarning("플레이어에 CharacterController나 Rigidbody가 없습니다.");
            }
        }
    }
    else Debug.LogError("Player 태그를 찾을 수 없습니다.");
}


    [Header("Behavior Tree Mode")]
    [SerializeField] private bool useBehaviorTree = false; // 인스펙터에서 설정 가능
    private HoundBehaviorTree houndBehaviorTree;
    
    void Update()
    {
        if (isDead || player == null) return;

        // 플레이어 첫 조우 시 사운드 재생
        if (!hasPlayedEncounterSound)
        {
            if (Vector3.Distance(transform.position, player.position) < encounterRange)
            {
                PlaySound(spawnSound);
                hasPlayedEncounterSound = true;
                Debug.Log("플레이어 발견! 조우 사운드를 재생합니다.");
            }
        }

        // 페이즈 전환 패턴이 실행 중일 때는 다른 모든 로직을 중단하고 제자리에 고정시킵니다.
        if (isPhaseTransitionTriggered)
        {
            SafeStopAgent();
            SetAnimationParameters(0f, 0f, 0f); // 이동 애니메이션 정지
            return;
        }

        // 점프 중이면 Behavior Tree 모드와 관계없이 처리해야 함
        if (isJumpingToTarget)
        {
            jumpTimer -= Time.deltaTime;
            HandleNavMeshJump();
        }

        // Behavior Tree 모드가 활성화되면 나머지 AI 로직 비활성화
        if (useBehaviorTree) return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("R키 눌림 - 강제 투사체 후퇴 공격 실행!");
            ForceRetreatWithProjectileAttack(); // 메서드명 변경
        }
        if (Input.GetKeyDown(KeyCode.O)) // 돌진 테스트 키 추가
        {
            Debug.Log("C키 눌림 - 강제 돌진 공격 실행!");
            PerformChargeAttack();
        }

        if (Input.GetKeyDown(KeyCode.P)) // P키로 페이즈 2 강제 전환
    {
        if (!isPhase2 && !isPhaseTransitionTriggered)
        {
            Debug.Log("P키 - 강제 페이즈 2 전환!");
            TriggerPhaseTransition();
        }
    }

        float distance = Vector3.Distance(transform.position, player.position);
        
        UpdateTimers();
        HandleNaturalCollisionAvoidance(distance);
        HandleCombatState(distance);
        HandleMovementWithBlendTree(distance);
    }
    void UpdatePhase2Damages()
    {
        if (isPhase2)
        {
            leftPawDamage *= phase2DamageMultiplier;
            rightPawDamage *= phase2DamageMultiplier;
            lickBiteDamage *= phase2DamageMultiplier;
            jumpAttackDamage *= phase2DamageMultiplier;
            projectileDamage *= phase2DamageMultiplier; 
            chargeDamage *= phase2DamageMultiplier; 
            
            Debug.Log($"=== 2페이즈 데미지 증가! ===");
            Debug.Log($"발차기: {leftPawDamage}, 물기: {lickBiteDamage}");
            Debug.Log($"점프: {jumpAttackDamage}, 투사체: {projectileDamage}");
            Debug.Log($"돌진: {chargeDamage}");
        }
    }
    public void PerformProjectileAttack()
    {
        if (isPhaseTransitionTriggered) return; // 페이즈 전환 중에는 실행 방지
        if (isAttacking || attackTimer > 0) return; // 메인 공격 타이머 사용
        if (isProjectileAttacking || isFiringSpiritArrows) return; // 다른 원거리 공격 중인지 확인

        // Behavior Tree 사용 시 공격 상태 설정
        if (useBehaviorTree && houndBehaviorTree != null)
        {
            houndBehaviorTree.SetAttacking(true);
        }

        isAttacking = true; // 메인 공격 플래그 설정
        isProjectileAttacking = true;
        attackTimer = projectileAttackCooldown; // 메인 공격 타이머 설정

        StartCoroutine(ProjectileAttackPattern());
        Debug.Log($"투사체 공격 실행 - 다음 투사체까지 {projectileAttackCooldown}초 대기");
    }
    IEnumerator ProjectileAttackPattern()
    {
        SafeStopAgent();
        // isProjectileAttacking = true; // PerformProjectileAttack에서 이미 설정됨
        
        // FrontPawAttack 애니메이션 사용
        animator.SetTrigger("FrontPawAttack");
        
        // 애니메이션 타이밍에 맞춰 대기
        yield return new WaitForSeconds(0.6f);
        
        // 연속 투사체 발사
        for (int i = 0; i < projectileCount; i++)
        {
            FireParticleProjectile();
            yield return new WaitForSeconds(projectileInterval);
        }
        
        isProjectileAttacking = false;
        isAttacking = false; // 메인 공격 플래그 해제
        
        // Behavior Tree 사용 시 공격 상태 해제
        if (useBehaviorTree && houndBehaviorTree != null)
        {
            houndBehaviorTree.SetAttacking(false);
        }
        
        yield return new WaitForSeconds(1f); // 쿨다운
        SafeResumeAgent();
    }

    public void PerformSpiritArrowRain()
    {
        if (isPhaseTransitionTriggered) return; // 페이즈 전환 중에는 실행 방지
        if (isAttacking || attackTimer > 0) return; // 메인 공격 타이머 사용
        if (isFiringSpiritArrows || isProjectileAttacking) return;

        if (useBehaviorTree && houndBehaviorTree != null)
        {
            houndBehaviorTree.SetAttacking(true);
        }

        isAttacking = true; // 메인 공격 플래그 설정
        isFiringSpiritArrows = true;
        attackTimer = spiritArrowCooldown; // 메인 공격 타이머 설정

        StartCoroutine(SpiritArrowRainPattern());
    }

    IEnumerator SpiritArrowRainPattern()
    {
        // isFiringSpiritArrows = true; // PerformSpiritArrowRain에서 이미 설정됨
        spiritArrowTimer = spiritArrowCooldown; // 쿨다운 시작
        SafeStopAgent();

        Debug.Log("영혼 화살비(Spirit Arrow Rain) 시전!");

        // 'FrontPawAttack' 애니메이션을 사용하여 빠르게 발사하는 느낌을 줍니다.
        // 'Roar'는 2페이즈 전환 패턴에서 사용하므로, 다른 애니메이션으로 구분합니다.
        animator.SetTrigger("FrontPawAttack");
        yield return new WaitForSeconds(0.8f); // 기 모으는 시간

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        Quaternion centerRotation = Quaternion.LookRotation(directionToPlayer);
        float angleStep = spiritArrowCount > 1 ? spiritArrowSpreadAngle / (spiritArrowCount - 1) : 0;
        float startAngle = -spiritArrowSpreadAngle / 2f;

        for (int i = 0; i < spiritArrowCount; i++)
        {
            float angle = startAngle + i * angleStep;
            Quaternion rotation = centerRotation * Quaternion.Euler(0, angle, 0);
            Vector3 fireDirection = rotation * Vector3.forward;

            if (spiritArrowPrefab != null)
            {
                GameObject projectile = Instantiate(spiritArrowPrefab, mouthTransform.position, rotation);
                var projectileScript = projectile.GetComponent<ParticleProjectile>() ?? projectile.AddComponent<ParticleProjectile>();
                projectileScript.Initialize(fireDirection, spiritArrowSpeed, spiritArrowDamage * (isPhase2 ? phase2DamageMultiplier : 1f), 5f);
            }
            yield return new WaitForSeconds(0.1f); // 빠른 연사 간격
        }

        isFiringSpiritArrows = false;
        isAttacking = false; // 메인 공격 플래그 해제

        if (useBehaviorTree && houndBehaviorTree != null)
        {
            houndBehaviorTree.SetAttacking(false);
        }
    }
    void FireParticleProjectile()
    {
        if (projectileParticlePrefab != null && mouthTransform != null)
        {
            // 플레이어 방향 계산 (약간의 예측 포함)
            Vector3 playerVelocity = Vector3.zero;
            if (player.GetComponent<Rigidbody>() != null)
            {
                playerVelocity = player.GetComponent<Rigidbody>().linearVelocity;
            }
            
            Vector3 predictedPosition = player.position + playerVelocity * 0.3f + Vector3.up * 1.2f;
            Vector3 direction = (predictedPosition - mouthTransform.position).normalized;
            
            // 파티클 투사체 생성
            GameObject projectile = Instantiate(projectileParticlePrefab, mouthTransform.position, 
                Quaternion.LookRotation(direction));
            
            // 투사체 스크립트 추가
            ParticleProjectile projectileScript = projectile.GetComponent<ParticleProjectile>();
            if (projectileScript == null)
            {
                projectileScript = projectile.AddComponent<ParticleProjectile>();
            }
            
            projectileScript.Initialize(direction, projectileSpeed, projectileDamage, 5f);
            
            Debug.Log("파티클 투사체 발사!");
        }
    }
    void HandleNaturalCollisionAvoidance(float distance)
    {
        if (isJumping || isBackingAway|| isAttacking)
        {
            isAvoiding = false;
            avoidanceDirection = Vector3.Lerp(avoidanceDirection, Vector3.zero, 
                smoothAvoidance * Time.deltaTime);
            return;
        }

        if (distance < 1.0f && !isAggressiveMode) // 1.5f에서 1.0f로 축소
        {
            Vector3 dirToPlayer = (player.position - transform.position);
            if (dirToPlayer.magnitude > 0.01f)
            {
                dirToPlayer = dirToPlayer.normalized;
                Vector3 targetAvoidanceDirection = -dirToPlayer;
                
                avoidanceDirection = Vector3.Slerp(avoidanceDirection, targetAvoidanceDirection, 
                    smoothAvoidance * Time.deltaTime);
                
                isAvoiding = true;
                
                if (distance < minSeparationDistance && distance < 0.8f)
                {
                    if (playerUsesCharacterController && playerCharacterController != null)
                    {
                        if (Random.Range(0f, 1f) < 0.3f)
                        {
                            HandleCharacterControllerAvoidance(dirToPlayer);
                        }
                    }
                    else if (!playerUsesCharacterController && playerRb != null)
                    {
                        Vector3 pushForce = dirToPlayer * avoidanceForce;
                        playerRb.AddForce(pushForce, ForceMode.Force);
                    }
                    else
                    {
                        if (distance < minSeparationDistance * 0.6f && Random.Range(0f, 1f) < 0.2f)
                        {
                            StartBackingAway();
                        }
                    }
                }
            }
        }
        else
        {
            isAvoiding = false;
            avoidanceDirection = Vector3.Lerp(avoidanceDirection, Vector3.zero, 
                smoothAvoidance * Time.deltaTime);
        }
    }

    void UpdateTimers()
    {
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
        }

        if (isRecoveringFromJump)
        {
            jumpRecoveryTimer -= Time.deltaTime;
            if (jumpRecoveryTimer <= 0)
            {
                isRecoveringFromJump = false;
            }
        }

        if (isBackingAway)
        {
            backAwayTimer -= Time.deltaTime;
            if (backAwayTimer <= 0)
            {
                isBackingAway = false;
            }
        }

        if (isRotatingToAttack)
        {
            rotationTimer += Time.deltaTime;
            if (rotationTimer >= maxRotationTime)
            {
                isRotatingToAttack = false;
                rotationTimer = 0f;
                Debug.Log("회전 타임아웃 - 강제 종료");
            }
        }

        if (isJumpingToTarget)
        {
            jumpTimer -= Time.deltaTime;
        }
        if (jumpAttackTimer > 0)
        {
            jumpAttackTimer -= Time.deltaTime;
        }
        if (chargeAttackTimer > 0)
        {
            chargeAttackTimer -= Time.deltaTime;
        }
        if (projectileAttackTimer > 0)
        {
            projectileAttackTimer -= Time.deltaTime;
        }
        if (spiritArrowTimer > 0)
        {
            spiritArrowTimer -= Time.deltaTime;
        }
    }

    void HandleCombatState(float distance)
{
    if (isRecoveringFromJump || isJumping || isCharging || isProjectileAttacking || isFiringSpiritArrows || isPhaseTransitionTriggered || isAttacking)
    {
        return; // 기존 패턴 실행 중에는 다른 행동 방지
    }
        if (!isAttacking && attackTimer <= 0)
        {
            if (isPhase2)
            {
                // 거리에 따른 패턴 선택
                if (distance > attackRange * 1.5f)
                {
                    // 2페이즈 원거리: 점프, 투사체, 영혼 화살비
                    float rangePatternRoll = Random.Range(0f, 1f);
                    if (rangePatternRoll < 0.4f) // 40%
                    {
                        Debug.Log("2페이즈 원거리 - 점프 공격 선택");
                        PerformJumpAttack();
                    }
                    else if (rangePatternRoll < 0.7f) // 30%
                    {
                        Debug.Log("2페이즈 원거리 - 투사체 공격 선택");
                        PerformProjectileAttack();
                    }
                    else // 30%
                    {
                        Debug.Log("2페이즈 원거리 - 영혼 화살비 선택");
                        PerformSpiritArrowRain();
                    }
                }
                else if (distance <= attackRange)
                {
                    // 2페이즈 근거리: 돌진 또는 기본 공격
                    float meleePatternRoll = Random.Range(0f, 1f);
                    
                    if (meleePatternRoll < 0.15f) // 15% 확률로 돌진하여 거리 벌리기
                    {
                        Debug.Log("2페이즈 근거리 - 돌진 공격 선택");
                        PerformChargeAttack();
                    }
                    else // 85% 확률로 일반 근접 공격
                    {
                        Debug.Log("2페이즈 근거리 - 기본 공격 선택");
                        PerformMeleeAttack();
                    }
                }
            }
            else
            {
                if (distance > attackRange * 1.5f)
                {
                    // 원거리에서는 점프 공격 위주
                    float rangePatternRoll = Random.Range(0f, 1f);
                    if (rangePatternRoll < 0.7f)  // 70%
                    {
                        Debug.Log("1페이즈 원거리 - 이동 공격 선택");
                        PerformMovingAttack();

                    }
                    else  // 30%
                    {
                        Debug.Log("1페이즈 원거리 - 점프 공격 선택");
                        PerformJumpAttack();
                    }
                }
                else if (distance <= attackRange)
                {
                    // 근거리에서는 기본 공격
                    Debug.Log("1페이즈 근거리 - 기본 공격 선택");
                    PerformMeleeAttack();
                }
            }
        }
}


    void HandleMovementWithBlendTree(float distance)
    {
        // 페이즈 전환 중에는 모든 이동 및 회전 차단
        if (isPhaseTransitionTriggered)
        {
            SafeStopAgent();
            SetAnimationParameters(0f, 0f, 0f);
            return;
        }

        // 각종 패턴 실행 중 이동 차단
        if (isProjectileAttacking)
        {
            SafeStopAgent();
            SetAnimationParameters(0f, 0f, 0f);
            return;
        }
        if (isFiringSpiritArrows)
        {
            SafeStopAgent();
            SetAnimationParameters(0f, 0f, 0f);
            return;
        }

         // 돌진 중이면 모든 이동 차단 추가
        if (isCharging)
        {
            SafeStopAgent();
            SetAnimationParameters(0f, 0f, 0f);
            return;
        }

        Vector3 dirToPlayer = (player.position - transform.position);
        dirToPlayer.y = 0;
        
        if (!isDead && !isJumping && !isRotatingToAttack)
        {
            if (dirToPlayer.magnitude > 0.01f)
            {
                dirToPlayer = dirToPlayer.normalized;
                
                if (!useNavMeshRotation)
                {
                    targetRotation = Quaternion.LookRotation(dirToPlayer);
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation, 
                        targetRotation, 
                        rotationSpeed * Time.deltaTime
                    );
                }
            }
        }

        if (isJumping || isRotatingToAttack)
        {
            SafeStopAgent();
            SetAnimationParameters(0f, 0f, 0f);
            return;
        }

        if (isBackingAway)
        {
            HandleBackingAway();
        }
        else if (isRecoveringFromJump)
        {
            HandleRecoveryMovement(distance);
        }
        else if (isAvoiding)
        {
            HandleAvoidanceMovement();
        }
        else if (distance > jumpAttackRange && !isAttacking)
        {
            HandleNormalMovement(distance);
        }
        else if (distance > attackRange && distance <= jumpAttackRange && !isAttacking)
        {
            HandleCautiousApproach();
        }
        else if (distance > closeRange && distance <= attackRange && !isAttacking)
    {
        // 정지하지 말고 천천히 움직이도록 변경
        HandleCautiousApproach(); // 기존 메서드 재사용
    }
    else if (!isAttacking)
    {
        // 완전 정지 대신 최소한의 움직임 유지
        HandleIdleMovement(distance); // 새로운 메서드 추가
    }
    }
    void HandleIdleMovement(float distance)
{
    if (distance < 1.0f) // 너무 가까우면
    {
        // 약간 뒤로 이동
        SafeResumeAgent();
        Vector3 backPosition = transform.position - transform.forward * 0.5f;
        SafeSetDestination(backPosition);
        SafeSetSpeed(walkSpeed * 0.3f);
        SetAnimationParameters(0f, -0.2f, 0.2f);
    }
    else if (distance < closeRange)
    {
        // 제자리에서 천천히 회전
        SafeStopAgent();
        RotateTowardsPlayer();
        SetAnimationParameters(0f, 0.1f, 0.1f);
    }
    else
    {
        // 천천히 접근
        SafeResumeAgent();
        SafeSetDestination(player.position);
        SafeSetSpeed(walkSpeed * 0.4f);
        SetAnimationParameters(0f, 0.3f, 0.3f);
    }
}
    void HandleAvoidanceMovement()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            SafeResumeAgent();
            
            Vector3 avoidancePosition = transform.position + avoidanceDirection * 1.5f;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(avoidancePosition, out hit, 2f, NavMesh.AllAreas))
            {
                SafeSetDestination(hit.position);
                SafeSetSpeed(walkSpeed * 0.6f);
                
                Vector3 localAvoidanceDirection = transform.InverseTransformDirection(avoidanceDirection);
                SetAnimationParameters(localAvoidanceDirection.x, localAvoidanceDirection.z, 0.3f);
            }
        }
    }

    void HandleRecoveryMovement(float distance)
    {
        if (distance > attackRange)
        {
            SafeResumeAgent();
            SafeSetDestination(player.position);
            SafeSetSpeed(walkSpeed * 0.5f);
            
            Vector3 localDirection = agent != null ? transform.InverseTransformDirection(agent.velocity.normalized) : Vector3.zero;
            SetAnimationParameters(localDirection.x, localDirection.z, 0.2f);
        }
        else
        {
            SafeStopAgent();
            SetAnimationParameters(0f, 0f, 0f);
        }
    }

    void SetAnimationParameters(float moveX, float moveY, float speed)
    {
        animator.SetFloat("MoveX", moveX);
        animator.SetFloat("MoveY", moveY);
        animator.SetFloat("Speed", speed);
    }

    void HandleCharacterControllerAvoidance(Vector3 dirToPlayer)
    {
        if (!isBackingAway)
        {
            StartBackingAway();
        }
        
        avoidanceForce *= 1.5f;
        
        Vector3 strongAvoidanceDirection = -dirToPlayer;
        Vector3 extraAvoidancePosition = transform.position + strongAvoidanceDirection * 2.5f;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(extraAvoidancePosition, out hit, 3f, NavMesh.AllAreas))
        {
            SafeSetDestination(hit.position);
            SafeSetSpeed(walkSpeed * 1.5f);
        }
        
        Debug.Log("CharacterController 플레이어 감지 - 하운드 강화 회피");
    }

    void StartBackingAway()
{
    if (isBackingAway || isAggressiveMode) return;

    isAttacking = false;
    SafeStopAgent();
    DisableAttackCollider();
    
    isBackingAway = true;
    backAwayTimer = retreatDuration;
    lastRetreatTime = Time.time;
    
    Debug.Log("2초 후퇴 시작 - 후퇴 완료 후 제자리에서 투사체 공격");

    if (auroraEffectPrefab != null && currentAuroraEffect == null)
    {
        currentAuroraEffect = Instantiate(auroraEffectPrefab, transform.position, Quaternion.identity);
        currentAuroraEffect.transform.SetParent(transform);
        Debug.Log("오로라 이펙트 생성 (후퇴 시작 시)!");
    }

    StartCoroutine(RetreatThenProjectileAttack()); // 메서드명 변경
}

IEnumerator RetreatThenProjectileAttack() // 새로운 메서드
{
    Debug.Log("후퇴 후 투사체 공격 패턴 시작!");
    
    yield return new WaitForSeconds(retreatDuration);
    
    isBackingAway = false;
    
    if (currentAuroraEffect != null)
    {
        Destroy(currentAuroraEffect);
        currentAuroraEffect = null;
        Debug.Log("오로라 이펙트 제거!");
    }
    
    StartCoroutine(StationaryProjectileAttack());
}

IEnumerator StationaryProjectileAttack() // 새로운 메서드
{
    isProjectileAttacking = true;
    agent.isStopped = true;
    
    Debug.Log("제자리 투사체 공격 시작!");
    
    animator.SetTrigger("FrontPawAttack");
    
    yield return new WaitForSeconds(0.6f);
    
    float attackDuration = 8f;
    float elapsedTime = 0f;
    
    while (elapsedTime < attackDuration)
    {
        FireParticleProjectile();
        yield return new WaitForSeconds(projectileInterval);
        elapsedTime += projectileInterval;
    }
    
    isProjectileAttacking = false;
    
    // Behavior Tree 사용 시 공격 상태 해제
    if (useBehaviorTree && houndBehaviorTree != null)
    {
        houndBehaviorTree.SetAttacking(false);
    }
    
    agent.isStopped = false;
    
    Debug.Log("제자리 투사체 공격 완료!");
}


    

    void HandleBackingAway()
    {
        SafeStopAgent();
        
        Vector3 awayDirection = (transform.position - player.position);
        awayDirection.y = 0;
        
        if (awayDirection.magnitude > 0.01f)
        {
            awayDirection = awayDirection.normalized;
            
            Vector3 lookDirection = (player.position - transform.position).normalized;
            if (lookDirection.magnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * 2f * Time.deltaTime);
            }
            
            float backDistance = playerUsesCharacterController ? 
                walkSpeed * 2.5f * Time.deltaTime : 
                walkSpeed * 2f * Time.deltaTime;
            
            Vector3 backPosition = transform.position + awayDirection * backDistance;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(backPosition, out hit, 1f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }
            
            SetAnimationParameters(0f, -0.5f, 0.5f);
        }
    }

    void HandleNormalMovement(float distance)
    {
        SafeResumeAgent();
        SafeSetDestination(player.position);
        
        bool shouldRun = distance > runThreshold;
        SafeSetSpeed(shouldRun ? runSpeed : walkSpeed);
        
        if (useNavMeshRotation)
        {
            Vector3 velocity = agent != null ? agent.velocity : Vector3.zero;
            if (velocity.magnitude > 0.1f)
            {
                Vector3 localDirection = transform.InverseTransformDirection(velocity.normalized);
                float speedParameter = shouldRun ? 1f : 0.5f;
                SetAnimationParameters(localDirection.x, localDirection.z, speedParameter);
            }
            else
            {
                SetAnimationParameters(0f, 0f, 0f);
            }
        }
        else
        {
            Vector3 localDirection = agent != null ? transform.InverseTransformDirection(agent.velocity.normalized) : Vector3.zero;
            float speedParameter = shouldRun ? 1f : 0.5f;
            SetAnimationParameters(localDirection.x, localDirection.z, speedParameter);
        }
    }

    void HandleCautiousApproach()
    {
        SafeResumeAgent();
        SafeSetDestination(player.position);
        SafeSetSpeed(walkSpeed * 0.7f);

        if (useNavMeshRotation)
        {
            Vector3 velocity = agent != null ? agent.velocity : Vector3.zero;
            if (velocity.magnitude > 0.1f)
            {
                Vector3 localDirection = transform.InverseTransformDirection(velocity.normalized);
                SetAnimationParameters(localDirection.x, localDirection.z, 0.35f);
            }
            else
            {
                SetAnimationParameters(0f, 0f, 0f);
            }
        }
        else
        {
            Vector3 localDirection = agent != null ? transform.InverseTransformDirection(agent.velocity.normalized) : Vector3.zero;
            SetAnimationParameters(localDirection.x, localDirection.z, 0.35f);
        }
    }

    public void PerformMeleeAttack()
    {
        if (isPhaseTransitionTriggered) return; // 페이즈 전환 중에는 실행 방지

        if (!IsLookingAtPlayer())
    {
        return;
    }

    SafeStopAgent();
    
    // Behavior Tree 사용 시 공격 상태 설정
    if (useBehaviorTree && houndBehaviorTree != null)
    {
        houndBehaviorTree.SetAttacking(true);
    }
    
    // 1페이즈든 2페이즈든 근접 공격만 실행
    int randomAttack = Random.Range(0, 3); // 0~2 (LeftPaw, RightPaw, LickBite)
    currentAttackType = (HoundAttackType)randomAttack;

    switch (currentAttackType)
    {
        case HoundAttackType.LeftPaw:
            animator.SetTrigger("LeftPawAttack");
            PlaySound(leftAttackSound);
            break;
        case HoundAttackType.RightPaw:
            animator.SetTrigger("RightPawAttack");
            PlaySound(rightAttackSound);
            break;
        case HoundAttackType.LickBite:
            animator.SetTrigger("LickBite");
            PlaySound(tongueBiteSound);
            break;
    }

    isAttacking = true;
    attackTimer = attackCooldown;
    }

    public void PerformJumpAttack()
    {
        if (isPhaseTransitionTriggered) return; // 페이즈 전환 중에는 실행 방지

        if (!IsLookingAtPlayer())
        {
            return;
        }
        if (jumpAttackTimer > 0)
        {
            // 점프 공격이 불가능할 경우 다른 공격 선택
            PerformMeleeAttack();
            Debug.Log($"점프 공격 쿨다운 중 (남은 시간: {jumpAttackTimer:F1}초) - 기본 공격으로 대체");
            return;
        }

        // Behavior Tree 사용 시 공격 상태 설정
        if (useBehaviorTree && houndBehaviorTree != null)
        {
            houndBehaviorTree.SetAttacking(true);
        }

        currentAttackType = HoundAttackType.JumpAttack;
        jumpTargetPosition = player.position;
        
        animator.SetTrigger("JumpAttack");
        
        isJumping = true;
        isAttacking = true;
        attackTimer = attackCooldown * 2f;
        jumpTimer = jumpDuration;
        hasFallingTriggered = false;
        jumpAttackTimer = jumpAttackCooldown;
        Debug.Log($"점프 공격 실행 - 다음 점프까지 {jumpAttackCooldown}초 대기");
        Debug.Log($"NavMesh 점프 공격 실행! 목표: {jumpTargetPosition}");
    }

    public void PerformRetreatAndRangedAttack()
    {
        if (isPhaseTransitionTriggered || isRetreatingForAttack) return;

        // 기존 후퇴 쿨다운을 공유합니다.
        if (Time.time - lastRetreatTime < retreatCooldown)
        {
            // 쿨다운 중이면 대신 일반 근접 공격을 합니다.
            PerformMeleeAttack();
            return;
        }

        StartCoroutine(RetreatAndRangedAttackPattern());
    }

    private IEnumerator RetreatAndRangedAttackPattern()
    {
        isRetreatingForAttack = true;
        lastRetreatTime = Time.time;

        if (useBehaviorTree && houndBehaviorTree != null)
        {
            houndBehaviorTree.SetAttacking(true);
        }

        float retreatTimer = 0f;
        float retreatDuration = 1.5f;
        Debug.Log("후퇴 후 원거리 공격 시작!");

        while (retreatTimer < retreatDuration)
        {
            HandleBackingAway(); // 기존 후퇴 로직 재사용
            retreatTimer += Time.deltaTime;
            yield return null;
        }

        isRetreatingForAttack = false;

        // 후퇴 종료 후, 쿨다운을 무시하고 랜덤 원거리 공격을 즉시 실행합니다.
        Debug.Log("후퇴 완료. 쿨다운 무시하고 원거리 공격 선택.");
        float roll = Random.Range(0f, 1f);
        if (roll < 0.4f)
        {
            // 점프 공격은 쿨다운이 있을 경우 다른 공격으로 대체
            if (jumpAttackTimer <= 0) PerformJumpAttack();
            else StartCoroutine(ProjectileAttackPattern()); // 대체 공격
        }
        else if (roll < 0.7f)
        {
            // 투사체 공격 (쿨다운 무시)
            StartCoroutine(ProjectileAttackPattern());
        }
        else
        {
            // 영혼 화살비 (쿨다운 무시)
            StartCoroutine(SpiritArrowRainPattern());
        }
    }

    void PerformMovingAttack()
    {
        if (isPhaseTransitionTriggered) return; // 페이즈 전환 중에는 실행 방지

        if (!IsLookingAtPlayer())
    {
        return;
    }

    Vector3 dirToPlayer = (player.position - transform.position).normalized;
    dirToPlayer.y = 0;
    
    if (dirToPlayer.magnitude > 0.01f)
    {
        transform.rotation = Quaternion.LookRotation(dirToPlayer);
    }

    SafeResumeAgent();
    SafeSetDestination(player.position);
    SafeSetSpeed(runSpeed * 0.8f);

    // 랜덤 공격 선택 (점프 공격 제외)
    int randomAttack = Random.Range(0, 3); // 0~2 사이의 값만
    currentAttackType = (HoundAttackType)randomAttack;

    switch (currentAttackType)
    {
        case HoundAttackType.LeftPaw:
            animator.SetTrigger("WalkLeftPawAttack");
            Debug.Log("이동하면서 왼발 공격");
            EnableAttackCollider(); // 공격 판정 활성화
            break;
        case HoundAttackType.RightPaw:
            animator.SetTrigger("WalkRightPawAttack");
            Debug.Log("이동하면서 오른발 공격");
            EnableAttackCollider(); // 공격 판정 활성화
            break;
        case HoundAttackType.LickBite:
            animator.SetTrigger("WalkLickBite");
            Debug.Log("이동하면서 물기 공격");
            EnableAttackCollider(); // 공격 판정 활성화
            break;
    }

    isAttacking = true;
    attackTimer = attackCooldown * 0.8f;
    }

    void HandleNavMeshJump()
    {
        if (jumpTimer <= 0)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(jumpTargetPosition, out hit, 5f, NavMesh.AllAreas))
            {
                SafeWarpAgent(hit.position);
            }
            else
            {
                SafeWarpAgent(jumpTargetPosition);
            }
            
            isJumpingToTarget = false;
            OnJumpComplete();
            return;
        }

        Vector3 currentPos = transform.position;
        Vector3 targetPos = jumpTargetPosition;
        
        float progress = 1f - (jumpTimer / jumpDuration);
        Vector3 newPosition = Vector3.Lerp(currentPos, targetPos, progress);
        
        if (progress >= 0.5f && !hasFallingTriggered)
        {
            animator.SetTrigger("StartFalling");
            hasFallingTriggered = true;
            Debug.Log("StartFalling 트리거 발동 - Jump_End로 전환");
        }
        
        float height = Mathf.Sin(progress * Mathf.PI) * 2f;
        newPosition.y = currentPos.y + height;
        
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(newPosition, out navHit, 10f, NavMesh.AllAreas))
        {
            SafeWarpAgent(navHit.position);
        }
        else
        {
            SafeWarpAgent(newPosition);
        }
    }

    public void EnableAttackCollider()
    {
        DisableAttackCollider();

        switch (currentAttackType)
        {
            
            case HoundAttackType.LeftPaw:
                if (leftPawCollider) leftPawCollider.gameObject.SetActive(true);
                break;
            case HoundAttackType.RightPaw:
                if (rightPawCollider) rightPawCollider.gameObject.SetActive(true);
                break;
            case HoundAttackType.LickBite:
                if (lickBiteCollider) lickBiteCollider.gameObject.SetActive(true);
                break;
            case HoundAttackType.JumpAttack:
                if (jumpAttackCollider) jumpAttackCollider.gameObject.SetActive(true);
                break;
        }
    }

    public void DisableAttackCollider()
    {
        var colliders = new AttackCollider[] 
        { 
             leftPawCollider, rightPawCollider, 
            lickBiteCollider, jumpAttackCollider 
        };

        foreach (var collider in colliders)
        {
            if (collider) collider.gameObject.SetActive(false);
        }
    }

    public void OnAttackAnimationEnd()
    {
        isAttacking = false;
        
        // Behavior Tree 사용 시 공격 상태 해제
        if (useBehaviorTree && houndBehaviorTree != null)
        {
            houndBehaviorTree.SetAttacking(false);
        }
    }

    public void OnWalkAttackEnd()
    {
        isAttacking = false;
        
        // Behavior Tree 사용 시 공격 상태 해제
        if (useBehaviorTree && houndBehaviorTree != null)
        {
            houndBehaviorTree.SetAttacking(false);
        }
    }

    public void OnJumpStart()
    {
        Debug.Log("NavMesh 점프 시작!");
        SafeStopAgent();
        isJumpingToTarget = true;
    }

    public void OnLanding()
    {
        if (currentAttackType == HoundAttackType.JumpAttack)
        {
            PlaySound(jumpAttackSound);
            SpawnJumpAreaEffect();
        }
    }
    public void SpawnJumpAreaEffect()
{
    Debug.Log("점프 공격 데미지 영역 생성 시도");
    Vector3 spawnPosition = transform.position;
    spawnPosition.y = 0.1f; // 지면에서 살짝 위로
    
    GameObject effect = Instantiate(jumpAreaEffectPrefab, spawnPosition, Quaternion.identity);
    var damageComponent = effect.GetComponent<JumpAttackAreaDamage>();
    
    if (damageComponent != null)
    {
        damageComponent.Initialize(jumpAreaDamage, jumpAreaRadius);
        Debug.Log($"데미지 영역 생성 완료: 데미지 {jumpAreaDamage}, 반경 {jumpAreaRadius}m");
    }
    else
    {
        Debug.LogError("JumpAttackAreaDamage 컴포넌트를 찾을 수 없음! jumpAreaEffectPrefab에 해당 스크립트를 추가해주세요.");
    }
}
    


    public void OnJumpPeak() { }
    void CheckJumpAreaDamageImmediate()
{
    Debug.Log($"=== 즉시 범위 데미지 체크 시작 ===");
    Debug.Log($"체크 위치: {transform.position}, 반경: {jumpAreaRadius}");
    
    Collider[] hitColliders = Physics.OverlapSphere(transform.position, jumpAreaRadius);
    Debug.Log($"반경 내 {hitColliders.Length}개 콜라이더 발견");
    
    for (int i = 0; i < hitColliders.Length; i++)
    {
        Collider hitCollider = hitColliders[i];
        Debug.Log($"콜라이더 {i}: {hitCollider.name}, 태그: {hitCollider.tag}, 거리: {Vector3.Distance(transform.position, hitCollider.transform.position):F2}m");
        
        if (hitCollider.CompareTag("Player"))
        {
            Debug.Log($"★ 플레이어 발견: {hitCollider.name}");
            
            IDamageable damageable = hitCollider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(jumpAreaDamage);
                Debug.Log($"★★★ 즉시 점프 범위 데미지! 플레이어에게 {jumpAreaDamage} 데미지! ★★★");
            }
            else
            {
                Debug.LogError($"플레이어에게 IDamageable 컴포넌트가 없습니다: {hitCollider.name}");
                
                // 대안 방법 시도
                MonoBehaviour[] scripts = hitCollider.GetComponents<MonoBehaviour>();
                foreach (MonoBehaviour script in scripts)
                {
                    if (script.GetType().GetMethod("TakeDamage") != null)
                    {
                        script.GetType().GetMethod("TakeDamage").Invoke(script, new object[] { jumpAreaDamage });
                        Debug.Log($"★★★ 대안 방법으로 즉시 데미지 적용! {script.GetType().Name}.TakeDamage({jumpAreaDamage}) ★★★");
                        break;
                    }
                }
            }
        }
    }
    
    if (hitColliders.Length == 0)
    {
        Debug.Log("반경 내에 아무것도 없음");
    }
}


    public void OnJumpComplete()
    {
        Debug.Log("NavMesh 점프 완료!");
        isJumping = false;
        isAttacking = false;
        isJumpingToTarget = false;

        // Behavior Tree 사용 시 공격 상태 해제
        if (useBehaviorTree && houndBehaviorTree != null)
        {
            houndBehaviorTree.SetAttacking(false);
        }

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
        {
            SafeWarpAgent(hit.position);
            Debug.Log($"지면 착지 확인: {hit.position}");
        }

        isRecoveringFromJump = true;
        jumpRecoveryTimer = jumpAttackRecoveryTime;

        DisableAttackCollider();
    }

    public void OnLeaveGround()
    {
        Debug.Log("지면 이탈!");
    }

    public void OnStartFalling()
    {
        Debug.Log("낙하 시작!");
    }

    public void OnAboutToLand()
    {
        Debug.Log("착지 준비!");
    }

    // 공격을 강제로 중단하는 메서드
    public void CancelAttack()
    {
        if (!isAttacking) return;

        isAttacking = false;
        isJumping = false;
        isCharging = false;
        isProjectileAttacking = false;
        isFiringSpiritArrows = false;
        isRetreatingForAttack = false;

        // Behavior Tree에 공격 종료 상태 전파
        if (useBehaviorTree && houndBehaviorTree != null)
        {
            houndBehaviorTree.SetAttacking(false);
        }
        Debug.Log("공격이 강제로 중단되었습니다.");
    }

    // IsDead() 메서드는 제거 - 상단의 IsDead 프로퍼티 사용

    public void TakeDamage(float damageAmount)
    {
        if (isDead)
        {
            Debug.Log("[하운드] 이미 사망 상태 - 데미지 무시");
            return;
        }
        
        if (isInvincible)
        {
            Debug.Log("[하운드] 무적 상태! 데미지 무시");
            return;
        }

        if (Time.time - lastDamageTime < damageImmunityTime)
        {
            Debug.Log($"[하운드] 데미지 면역 시간 중 (남은 시간: {damageImmunityTime - (Time.time - lastDamageTime):F2}초)");
            return;
        }

        lastDamageTime = Time.time;
        currentHp -= damageAmount;
        
        // 피격 사운드 재생
        PlayHitSound();
        
        // 기본 위치에 히트 이펙트 생성
        if (HitEffectManager.Instance != null)
        {
            HitEffectManager.Instance.PlayHitEffect(transform.position + Vector3.up * 2f, 0);
        }
        
        bossHealthBar?.UpdateHealth(currentHp);
        Debug.Log($"[하운드 피격] {damageAmount} 데미지! 현재 HP: {currentHp}/{maxHp} (페이즈: {(isPhase2 ? "2" : "1")}, 무적: {isInvincible})");
        
        if (!isPhase2 && !isPhaseTransitionTriggered &&
        currentHp <= maxHp * phase2HealthThreshold)
        {
            TriggerPhaseTransition();
            return;
        }

        if (currentHp <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// 충돌 위치 정보와 함께 데미지를 받습니다
    /// </summary>
    public void TakeDamage(float damageAmount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (isDead)
        {
            Debug.Log("[하운드] 이미 사망 상태 - 데미지 무시");
            return;
        }
        
        if (isInvincible)
        {
            Debug.Log("[하운드] 무적 상태! 데미지 무시");
            return;
        }

        if (Time.time - lastDamageTime < damageImmunityTime)
        {
            Debug.Log($"[하운드] 데미지 면역 시간 중 (남은 시간: {damageImmunityTime - (Time.time - lastDamageTime):F2}초)");
            return;
        }

        lastDamageTime = Time.time;
        currentHp -= damageAmount;
        
        // 피격 사운드 재생
        PlayHitSound();
        
        // 충돌 위치에 히트 이펙트 생성
        if (HitEffectManager.Instance != null)
        {
            HitEffectManager.Instance.PlayHitEffect(hitPoint, hitNormal, 0);
        }
        
        bossHealthBar?.UpdateHealth(currentHp);
        Debug.Log($"[하운드 피격] {damageAmount} 데미지 (위치: {hitPoint})! 현재 HP: {currentHp}/{maxHp} (페이즈: {(isPhase2 ? "2" : "1")}, 무적: {isInvincible})");
        
        if (!isPhase2 && !isPhaseTransitionTriggered &&
        currentHp <= maxHp * phase2HealthThreshold)
        {
            TriggerPhaseTransition();
            return;
        }

        if (currentHp <= 0)
        {
            Die();
        }
    }
    
    private void PlayHitSound()
    {
        if (hitSounds != null && hitSounds.Length > 0 && audioSource != null && !isDead)
        {
            AudioClip clip = hitSounds[Random.Range(0, hitSounds.Length)];
            if (clip != null)
            {
                audioSource.PlayOneShot(clip, hitSoundVolume);
            }
        }
    }

    private void PlaySound(AudioClip clip, float volume = 0.7f)
    {
        if (audioSource == null)
        {
            Debug.LogError("AudioSource is missing!");
            return;
        }
        if (clip == null)
        {
            Debug.LogWarning("Attempted to play a sound, but the AudioClip was null. Please assign it in the Inspector.");
            return;
        }
        audioSource.PlayOneShot(clip, volume);
    }

    private IEnumerator PlaySoundWithDelay(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        PlaySound(clip);
    }

    void Die()
    {
        isDead = true;
        PlaySound(deathSound);
        // 체력바 숨기기 추가
        bossHealthBar?.HideBossHealthBar();
        animator.SetTrigger("Death");
        mainCollider.enabled = false;
        SafeStopAgent();
        soundManager?.PlayDeathSound();
        if (upperBodyLayerIndex != -1)
        {
            animator.SetLayerWeight(upperBodyLayerIndex, 0f);
        }

        Destroy(gameObject, 5f);
        playerData.AddGold(dropGold);

        // 스테이지 매니저에 보스 처치 알림
        var hook = GetComponent<StageBossHook>();
        if (hook == null)
        {
            hook = gameObject.AddComponent<StageBossHook>();
            hook.stageManager = StageManager.Instance;
        }
        hook?.OnBossDied();

        // 보스 스테이지 매니저에 보스 사망 알림
        BossStageManager.Instance?.OnBossDeath();
    }
    void TriggerPhaseTransition()
    {
        if (isPhaseTransitionTriggered) return;
        
        isPhaseTransitionTriggered = true;
        
        Debug.Log("=== 페이즈 전환 패턴 시작! ===");
        
        // 모든 다른 행동 정지
        isAttacking = false;
        isJumping = false;
        isJumpingToTarget = false; // 점프 상태 플래그 추가 초기화
        isBackingAway = false;
        isCharging = false;
        isProjectileAttacking = false;
        isFiringSpiritArrows = false;
        SafeStopAgent();
        if (agent.hasPath) agent.ResetPath(); // 이동 경로를 완전히 초기화하여 따라가는 현상을 방지합니다.

        // 새로운 2페이즈 전환 패턴 코루틴을 시작합니다.
        StartCoroutine(Phase2TransitionPattern());
    }

    IEnumerator Phase2TransitionPattern()
    {
        // 1. 무적 상태 및 쉴드 활성화
        // TriggerPhaseTransition에서 이미 NavMeshAgent를 정지하고 경로를 초기화했습니다.
        // 여기서는 루트 모션만 추가로 제어하여 애니메이션으로 인한 움직임을 방지합니다.
        bool originalRootMotion = animator.applyRootMotion;
        animator.applyRootMotion = false; // 애니메이션의 루트 모션으로 인한 움직임을 방지합니다.
        animator.SetTrigger("Roar"); // 포효 애니메이션으로 전환 시작을 알림
        
        isInvincible = true;
        Debug.Log($"=== 페이즈 전환: 무적 활성화! (isInvincible = {isInvincible}) ===");
        
        if (houndShieldPrefab != null)
        {
            // 쉴드를 하운드의 자식으로 생성하고, 제공해주신 정확한 로컬 좌표로 위치를 설정합니다.
            houndShield = Instantiate(houndShieldPrefab, transform);
            houndShield.transform.localPosition = new Vector3(0f, 0.5887311f, -3.59f);
            houndShield.transform.localRotation = Quaternion.identity; // 회전값도 초기화합니다.
            Debug.Log("페이즈 전환: 쉴드 오브젝트 생성 완료");
        }

        // 2. 바닥 물기 패턴 3회 실행
        for (int i = 0; i < floorBiteCount; i++)
        {
            Debug.Log($"바닥 물기 패턴 {i + 1}/{floorBiteCount}");

            // 플레이어의 현재 위치에 즉시 공격 실행
            Vector3 targetPosition = player.position; // 매번 플레이어의 현재 위치를 다시 가져옴

            if (floorBitePrefab != null)
            {
                // 여우 울음소리 (경고) - 장판 생성 시
                PlaySound(foxCrySound);
                // 무는 소리 (실제 공격) - 데미지 딜레이 후
                StartCoroutine(PlaySoundWithDelay(biteSound, floorBiteDamageDelay));

                GameObject biteEffect = Instantiate(floorBitePrefab, targetPosition, Quaternion.identity);
                var damageComponent = biteEffect.GetComponent<JumpAreaDamage>();
                if (damageComponent != null)
                {
                    // 데미지를 지연시키는 새로운 메서드 호출
                    damageComponent.InitializeWithDelay(floorBiteDamage, jumpAreaRadius * 0.7f, floorBiteDamageDelay);
                }
            }
            yield return new WaitForSeconds(floorBiteInterval);
        }

        // 3. 쉴드 제거 및 무적 해제, 2페이즈 활성화
        if (houndShield != null)
        {
            Destroy(houndShield);
            Debug.Log("페이즈 전환: 쉴드 오브젝트 제거됨");
        }
        
        isInvincible = false;
        Debug.Log($"=== 페이즈 전환: 무적 해제! (isInvincible = {isInvincible}) ===");

        // NavMeshAgent의 제어는 Behavior Tree가 다시 시작하므로, 여기서는 별도로 활성화할 필요가 없습니다.
        animator.applyRootMotion = originalRootMotion; // 원래 루트 모션 상태로 복원합니다.
        StartPhase2WithAura(); // 오라와 함께 2페이즈 시작
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, closeRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, jumpAttackRange);
        
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, jumpAreaRadius);
        
        if (player != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Vector3 forward = transform.forward * attackRange;
            Vector3 leftBoundary = Quaternion.Euler(0, -attackAngleThreshold, 0) * forward;
            Vector3 rightBoundary = Quaternion.Euler(0, attackAngleThreshold, 0) * forward;
            
            Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
            Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
            
            Vector3 dirToPlayer = (player.position - transform.position).normalized * attackRange;
            Gizmos.color = IsLookingAtPlayer() ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, transform.position + dirToPlayer);
        }
        
        if (isJumpingToTarget)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(jumpTargetPosition, 0.5f);
            Gizmos.DrawLine(transform.position, jumpTargetPosition);
        }
    }
}
