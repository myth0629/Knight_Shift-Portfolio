using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class HoundAI : MonoBehaviour, IDamageable
{
    [Header("기본 설정")]
    [SerializeField] public float maxHp = 100f;
    public float currentHp;
    private bool isDead = false;

    [SerializeField] int dropGold = 3000; // 골렘 처치 시 드랍되는 골드 양

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
    [SerializeField] float leftPawDamage = 20f;
    [SerializeField] float rightPawDamage = 20f;
    [SerializeField] float lickBiteDamage = 35f;
    [SerializeField] float jumpAttackDamage = 40f;
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
    [SerializeField] float projectileDamage = 25f; // 기존 frontPawDamage와 동일

    bool isProjectileAttacking = false;

    [Header("돌진 공격 설정")]
    [SerializeField] float chargeSpeed = 20f;
    [SerializeField] float chargeDamage = 25f;
    [SerializeField] float chargeDistance = 8f;
    [SerializeField] GameObject chargeParticlesPrefab;

    bool isCharging = false;



    [Header("공격 방향 체크 설정")]
    [SerializeField] float attackAngleThreshold = 30f;
    [SerializeField] float rotationBeforeAttack = 4f;
    [SerializeField] float maxRotationTime = 2f;
    
    private bool isRotatingToAttack = false;
    private float rotationTimer = 0f;

    [Header("파티클 쉴드 패턴 설정")]
    [SerializeField] GameObject houndShieldPrefab;
    [SerializeField] GameObject safeZonePrefab;
    [SerializeField] GameObject dangerAreaPrefab;
    [SerializeField] float safeZoneRadius = 1f; // 안전장판 반경
    [SerializeField] float dangerAreaSize = 100f; // 위험지대 크기 (새로 추가)
    [SerializeField] float maxSafeZoneDistance = 15f; // 최대 안전장판 거리
    [SerializeField] float minSafeZoneDistance = 8f; // 최소 안전장판 거리
    [SerializeField] float patternDuration = 5f; // 패턴 지속 시간
    [SerializeField] float massiveDamage = 50f; // 안전장판 밖에 있을 때 플레이어에게 주는 데미지

    private bool isShieldPatternActive = false;
    private GameObject houndShield;
    private GameObject safeZoneEffect;
    private GameObject dangerAreaEffect;
    private Vector3 safeZonePosition;

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
    [SerializeField] float phase2SpeedMultiplier = 1.5f;
    [SerializeField] float phase2AttackSpeedMultiplier = 0.7f;

    [Header("페이즈 전환 설정")]
    [SerializeField] bool isPhaseTransitionTriggered = false; // 전환 트리거 여부
    [SerializeField] GameObject phase2AuraPrefab; // 오라 파티클 프리팹
    [SerializeField] float auraEffectDuration = 3f; // 오라 지속 시간

    private bool hasTriggeredTransitionBarrier = false; // 전환용 배리어 실행 여부
    private float originalWalkSpeed;
    private float originalRunSpeed;
    private float originalAttackCooldown;

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
    
    private Quaternion targetRotation;

    [Header("후퇴 설정")]
    [SerializeField] float backAwayDuration = 1.5f;

    [Header("후퇴 시 오로라 블라스트 공격 설정")]
    [SerializeField] GameObject auroraEffectPrefab;
        [SerializeField] Transform mouthTransform;
    
    [SerializeField] float retreatDuration = 2f;

    
    private GameObject currentAuroraEffect;

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
    void PerformChargeAttack()
{
    if (isCharging) return;
    
    StartCoroutine(ChargeAttackPattern());
}

IEnumerator ChargeAttackPattern()
{
    isCharging = true;
    
    Vector3 chargeDirection = (player.position - transform.position).normalized;
    chargeDirection.y = 0;
    
    transform.rotation = Quaternion.LookRotation(chargeDirection);
    
    GameObject chargeParticles = null;
    if (chargeParticlesPrefab != null)
    {
        chargeParticles = Instantiate(chargeParticlesPrefab, transform.position, transform.rotation);
        chargeParticles.transform.SetParent(transform);
    }
    
    SafeStopAgent();
    yield return new WaitForSeconds(1f);
    
    Debug.Log("돌진 시작!");
    
    float chargedDistance = 0f;
    
    while (chargedDistance < chargeDistance && isCharging)
    {
        float moveStep = chargeSpeed * Time.deltaTime;
        transform.position += chargeDirection * moveStep;
        chargedDistance += moveStep;
        
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 1.5f);
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(chargeDamage);
                    Debug.Log($"돌진 데미지 적용! {chargeDamage} 데미지");
                }
                break;
            }
        }
        
        yield return null;
    }
    
    isCharging = false;
    SafeResumeAgent();
    
    if (chargeParticles != null)
    {
        Destroy(chargeParticles, 2f);
    }
    
    yield return new WaitForSeconds(2f);
    Debug.Log("돌진 공격 완료!");
}

    void PerformShieldPattern()
    {
        
        if (isShieldPatternActive) return;
        isShieldPatternActive = true;
        Debug.Log("하운드 쉴드 + 안전장판 패턴 시작!");
        StartCoroutine(ShieldAndSafeZonePattern());
        
    }

    IEnumerator ShieldAndSafeZonePattern()
    {
        isShieldPatternActive = true;
        // 쉴드 생성
        if (houndShieldPrefab != null)
        {
            houndShield = Instantiate(houndShieldPrefab, transform.position, Quaternion.identity);
            houndShield.transform.SetParent(transform);
            Debug.Log("하운드 쉴드 생성!");
        }

        SafeStopAgent();
        agent.isStopped = true;
        Debug.Log("하운드 이동 금지!");

        safeZonePosition = GetRandomSafeZonePosition();
        if (safeZonePrefab != null)
        {
            safeZoneEffect = Instantiate(safeZonePrefab, safeZonePosition, Quaternion.identity);
            AdjustSafeZoneParticleSize(safeZoneEffect, safeZoneRadius);
            Debug.Log($"안전장판 생성! 위치: {safeZonePosition}, 반경: {safeZoneRadius}m");
        }

        if (dangerAreaPrefab != null)
        {
            dangerAreaEffect = Instantiate(dangerAreaPrefab, transform.position, Quaternion.identity);
            Debug.Log("데미지장판 생성 (5초 후 활성화)");
            ParticleSystem[] dangerParticles = dangerAreaEffect.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in dangerParticles)
            {
                if (ps != null)
                {
                    var shape = ps.shape;

                    // 검색 결과 5번 적용: Shape 타입에 따른 크기 조정
                    if (shape.shapeType == ParticleSystemShapeType.Box)
                    {
                        shape.scale = new Vector3(dangerAreaSize, 2f, dangerAreaSize); // 100x100 크기
                    }
                    else if (shape.shapeType == ParticleSystemShapeType.Circle)
                    {
                        shape.radius = dangerAreaSize * 0.5f; // 반경 50m
                    }
                    else if (shape.shapeType == ParticleSystemShapeType.Sphere)
                    {
                        shape.radius = dangerAreaSize * 0.5f; // 반경 50m
                    }

                    Debug.Log($"위험지대 파티클 크기 조정: {dangerAreaSize}x{dangerAreaSize}");
                }
                
            }
            
    
        }
        //전체 이펙트 크기도 조정
        dangerAreaEffect.transform.localScale = Vector3.one * (dangerAreaSize / 50f); // 기본 50 기준으로 스케일링
        
        Debug.Log($"대형 위험지대 생성! 크기: {dangerAreaSize}x{dangerAreaSize}");
    

        

        yield return new WaitForSeconds(patternDuration);

        CheckPlayerSafetyAndDamage();

        yield return new WaitForSeconds(1f);

            if (isPhaseTransitionTriggered && !isPhase2)
        {
            StartPhase2WithAura();
        }
        if (houndShield != null)
        {
            Destroy(houndShield);
            Debug.Log("하운드 쉴드 제거!");
        }

        if (safeZoneEffect != null)
        {
            Destroy(safeZoneEffect);
            Debug.Log("안전장판 제거!");
        }

        if (dangerAreaEffect != null)
        {
            Destroy(dangerAreaEffect);
            Debug.Log("데미지장판 제거!");
        }

        agent.isStopped = false;
        Debug.Log("하운드 이동 재개!");

        isShieldPatternActive = false;
        attackTimer = attackCooldown * 2f;
        Debug.Log("쉴드 패턴 완료!");
    }
    void AdjustSafeZoneParticleSize(GameObject safeZoneEffect, float radius)
{
    ParticleSystem[] particles = safeZoneEffect.GetComponentsInChildren<ParticleSystem>();
    
    foreach (ParticleSystem ps in particles)
    {
        if (ps != null)
        {
            // Scaling Mode를 Local로 설정
            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Local;
            
            // Shape 크기 조정
            var shape = ps.shape;
            
            if (shape.shapeType == ParticleSystemShapeType.Circle)
            {
                shape.radius = radius; // 원형이면 반지름 설정
            }
            else if (shape.shapeType == ParticleSystemShapeType.Box)
            {
                shape.scale = new Vector3(radius * 2f, shape.scale.y, radius * 2f); // 박스면 가로세로 설정
            }
            else if (shape.shapeType == ParticleSystemShapeType.Sphere)
            {
                shape.radius = radius; // 구형이면 반지름 설정
            }
            
            Debug.Log($"안전지대 파티클 Shape 크기 조정: {radius}");
        }
    }
}
    void CheckPlayerSafetyAndDamage()
    {
        if (player == null) return;
        
        float distanceFromSafeZone = Vector3.Distance(player.position, safeZonePosition);
        
        if (distanceFromSafeZone <= safeZoneRadius)
        {
            Debug.Log($"플레이어가 안전장판 내부에 있음! (거리: {distanceFromSafeZone:F1}m <= {safeZoneRadius}m) - 데미지 없음");
        }
        else
        {
            IDamageable damageable = player.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(massiveDamage);
                Debug.Log($"플레이어가 안전장판 밖에 있음! (거리: {distanceFromSafeZone:F1}m > {safeZoneRadius}m) - {massiveDamage} 데미지!");
            }
        }
    }
    
    Vector3 GetRandomSafeZonePosition()
    {
        Vector3 randomPosition = Vector3.zero;
        int attempts = 0;
        int maxAttempts = 10;
        
        while (attempts < maxAttempts)
        {
            Vector3 randomDirection = Random.insideUnitSphere;
            randomDirection.y = 0;
            
            float randomDistance = Random.Range(minSafeZoneDistance, maxSafeZoneDistance);
            Vector3 candidatePosition = transform.position + (randomDirection.normalized * randomDistance);
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(candidatePosition, out hit, 5f, NavMesh.AllAreas))
            {
                randomPosition = hit.position;
                Debug.Log($"안전장판 위치 결정: {randomPosition} (하운드로부터 {randomDistance:F1}m)");
                break;
            }
            
            attempts++;
        }
        
        if (attempts >= maxAttempts)
        {
            randomPosition = transform.position + Vector3.forward * 10f;
            Debug.LogWarning("안전장판 위치를 찾지 못함 - 기본 위치 사용");
        }
        
        return randomPosition;
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
        
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        dirToPlayer.y = 0;
        
        if (dirToPlayer.magnitude < 0.01f) return false;
        
        float angle = Vector3.Angle(transform.forward, dirToPlayer);
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
                colliders[i].damageAmount = damages[i];
                colliders[i].SetDamageSource(this);
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
            bossHealthBar = FindObjectOfType<SimpleBossHealthBar>();
        if (bossHealthBar != null)
        {
            bossHealthBar.ShowBossHealthBar(bossName, maxHp, currentHp);
            isVisible = true;
            Debug.Log("하운드 씬 로드 완료! 체력바 즉시 표시");
        }
        soundManager = GetComponent<HoundSoundManager>();
        DisableAttackCollider();
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


    void Update()
    {
        if (isDead || player == null) return;

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
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("T키 눌림 - 강제 쉴드 패턴 실행!");
            PerformShieldPattern();
        }

        if (Input.GetKeyDown(KeyCode.P)) // P키로 페이즈 2 강제 전환
    {
        if (!isPhase2 && !isPhaseTransitionTriggered)
        {
            Debug.Log("P키 - 강제 페이즈 2 전환!");
            TriggerPhaseTransitionBarrier();
        }
    }

        float distance = Vector3.Distance(transform.position, player.position);
        
        UpdateTimers();
        HandleNaturalCollisionAvoidance(distance);
        HandleCombatState(distance);
        HandleMovementWithBlendTree(distance);
        
        if (isJumpingToTarget)
        {
            HandleNavMeshJump();
        }
    }
    void PerformProjectileAttack()
    {
        if (isProjectileAttacking) return;
        
        StartCoroutine(ProjectileAttackPattern());
    }
    IEnumerator ProjectileAttackPattern()
    {
        isProjectileAttacking = true;
        
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
        yield return new WaitForSeconds(1f); // 쿨다운
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
        if (isJumping || isBackingAway|| isAttacking || isShieldPatternActive)
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
    }

    void HandleCombatState(float distance)
{
    if (isRecoveringFromJump || isJumping)
    {
        return;
    }

    if (!isAggressiveMode && Random.Range(0f, 1f) < 0.4f)
    {
        isAggressiveMode = true;
        consecutiveAttacks = 0;
        Debug.Log("공격적 모드 활성화!");
    }

    // 근거리 - 버퍼 적용
    if (distance <= closeRange - rangeBuffer && !isAttacking && !isBackingAway)
    {
        bool canRetreat = Time.time - lastRetreatTime > 8f;
        
        if (Random.Range(0f, 1f) < 0.05f && !isAggressiveMode && canRetreat)
        {
            StartBackingAway();
            lastRetreatTime = Time.time;
            Debug.Log("근거리 후퇴 선택 (5% 확률)");
        }
        else
        {
            if (attackTimer <= 0 && IsLookingAtPlayer())
            {
                PerformMeleeAttack();
                Debug.Log("근거리 공격 실행");
            }
        }
        return;
    }
    else if (isBackingAway)
    {
        return;
    }
    // 중거리 - 버퍼 적용
    else if (distance > closeRange + rangeBuffer && distance <= attackRange - rangeBuffer)
    {
        if (attackTimer <= 0 && !isAttacking)
        {
            if (IsLookingAtPlayer())
            {
                if (isAggressiveMode && consecutiveAttacks < maxConsecutiveAttacks)
                {
                    if (Random.Range(0f, 1f) < 0.9f)
                    {
                        PerformMeleeAttack();
                        consecutiveAttacks++;
                        attackTimer = attackCooldown * 0.5f;
                        return;
                    }
                    else
                    {
                        isAggressiveMode = false;
                        consecutiveAttacks = 0;
                        Debug.Log("공격적 모드 종료");
                    }
                }
                
                float actionRoll = Random.Range(0f, 1f);
                    if (!isPhase2) // 1페이즈에서는 기본 공격만
                    {
                        PerformMeleeAttack();
                    }
                    else 
                    {
                        if (actionRoll < 0.8f)
                        {
                            PerformMeleeAttack();
                        }
                        else
                        {
                            PerformChargeAttack();
                        }
                        
                        
                        }
                    }
                
                isRotatingToAttack = false;
                rotationTimer = 0f;
            }
            else
            {
                isRotatingToAttack = true;
                RotateTowardsPlayer();
                SafeStopAgent();
                SetAnimationParameters(0f, 0f, 0f);
            }
        
    }
    // 원거리 - 버퍼 적용
    else if (distance > attackRange + rangeBuffer && distance <= jumpAttackRange - rangeBuffer)
    {
        if (attackTimer <= 0 && !isAttacking)
        {
            if (IsLookingAtPlayer())
            {
                float attackChoice = Random.Range(0f, 1f);
                if (attackChoice < 0.5f)
                {
                    PerformJumpAttack();
                }
                else
                {
                    PerformMovingAttack();
                }
                
                isRotatingToAttack = false;
                rotationTimer = 0f;
            }
            else
            {
                isRotatingToAttack = true;
                RotateTowardsPlayer();
                SafeStopAgent();
                SetAnimationParameters(0f, 0f, 0f);
            }
        }
    }
    else
    {
        isRotatingToAttack = false;
        rotationTimer = 0f;
    }
}


    void HandleMovementWithBlendTree(float distance)
    {
        // 쉴드 패턴 중이면 모든 이동 차단
        if (isShieldPatternActive)
        {
            SafeStopAgent();
            SetAnimationParameters(0f, 0f, 0f);
            return;
        }

         // 투사체 공격 중이면 모든 이동 차단 추가
        if (isProjectileAttacking)
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
        if (distance > 1.5f) // 너무 가깝지 않으면 천천히 접근
        {
            SafeResumeAgent();
            SafeSetDestination(player.position);
            SafeSetSpeed(walkSpeed * 0.3f); // 매우 천천히
            SetAnimationParameters(0f, 0.1f, 0.1f); // 최소한의 애니메이션
        }
        else
        {
            // 너무 가까우면 제자리에서 회전만
            SafeStopAgent();
            RotateTowardsPlayer();
            SetAnimationParameters(0f, 0f, 0f);
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

    void PerformMeleeAttack()
    {
        if (!IsLookingAtPlayer())
        {
            return;
        }

        SafeStopAgent();
        
        if (!isPhase2) // 1페이즈에서는 투사체 공격 제거
        {
            int randomAttack = Random.Range(0, 3); // 0~2만 (투사체 제외)
            currentAttackType = (HoundAttackType)randomAttack;
        }
        else // 2페이즈에서는 투사체 포함
        {
            int randomAttack = Random.Range(0, 4);
            if (randomAttack == 0)
            {
                PerformProjectileAttack();
                return;
            }
            currentAttackType = (HoundAttackType)(randomAttack - 1);
        }

        switch (currentAttackType)
        {
           
            case HoundAttackType.LeftPaw:
                animator.SetTrigger("LeftPawAttack");
                break;
            case HoundAttackType.RightPaw:
                animator.SetTrigger("RightPawAttack");
                break;
            case HoundAttackType.LickBite:
                animator.SetTrigger("LickBite");
                break;
        }

        isAttacking = true;
        attackTimer = attackCooldown;
    }

    void PerformJumpAttack()
    {
        if (!IsLookingAtPlayer())
        {
            return;
        }

        currentAttackType = HoundAttackType.JumpAttack;
        jumpTargetPosition = player.position;
        
        animator.SetTrigger("JumpAttack");
        
        isJumping = true;
        isAttacking = true;
        attackTimer = attackCooldown * 2f;
        jumpTimer = jumpDuration;
        hasFallingTriggered = false;
        
        Debug.Log($"NavMesh 점프 공격 실행! 목표: {jumpTargetPosition}");
    }

    void PerformMovingAttack()
    {
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

        int randomAttack = Random.Range(0, 4);
        currentAttackType = (HoundAttackType)randomAttack;

        switch (currentAttackType)
        {
            
            case HoundAttackType.LeftPaw:
                animator.SetTrigger("WalkLeftPawAttack");
                break;
            case HoundAttackType.RightPaw:
                animator.SetTrigger("WalkRightPawAttack");
                break;
            case HoundAttackType.LickBite:
                animator.SetTrigger("WalkLickBite");
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
    }

    public void OnWalkAttackEnd()
    {
        isAttacking = false;
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
            PerformJumpAreaDamage();
        }
    }

    void PerformJumpAreaDamage()
    {
        Debug.Log("=== 점프 착지 - 범위 데미지 실행! ===");

        Vector3 landingPosition = transform.position;
        landingPosition.y = 0.1f;

        // 즉시 데미지 체크 제거 - 파티클에서만 처리하도록 변경
        // CheckJumpAreaDamageImmediate(); // 이 줄 주석 처리

        if (jumpAreaEffectPrefab != null)
        {
            Debug.Log($"파티클 프리팹 생성 시작: {jumpAreaEffectPrefab.name}");

            GameObject areaEffect = Instantiate(jumpAreaEffectPrefab, landingPosition, Quaternion.identity);
            areaEffect.transform.localScale = Vector3.one * jumpAreaRadius;

            // JumpAreaDamage 컴포넌트 확인/추가
            JumpAreaDamage damageComponent = areaEffect.GetComponent<JumpAreaDamage>();
            if (damageComponent == null)
            {
                damageComponent = areaEffect.AddComponent<JumpAreaDamage>();
            }

            // 컴포넌트 초기화 - 약간의 지연 후 실행
            StartCoroutine(InitializeDamageComponent(damageComponent));

            // 파티클 시스템 활성화
            ParticleSystem[] particles = areaEffect.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in particles)
            {
                if (ps != null)
                {
                    ps.Play();
                }
            }

            Destroy(areaEffect, jumpAreaEffectDuration);
        }
        else
        {
            // 파티클이 없는 경우에만 즉시 데미지
            CheckJumpAreaDamageImmediate();
        }
    }
IEnumerator InitializeDamageComponent(JumpAreaDamage damageComponent)
{
    yield return new WaitForFixedUpdate(); // 물리 업데이트 대기
    
    if (damageComponent != null)
    {
        damageComponent.Initialize(jumpAreaDamage, jumpAreaRadius, jumpAreaEffectDuration);
        Debug.Log($"JumpAreaDamage 초기화 완료 - 데미지: {jumpAreaDamage}");
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

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        if (Time.time - lastDamageTime < damageImmunityTime)
        {
            return;
        }

        lastDamageTime = Time.time;
        currentHp -= damageAmount;
        bossHealthBar?.UpdateHealth(currentHp);
        Debug.Log($"하운드가 {damageAmount} 데미지를 받았습니다. 현재 HP: {currentHp}/{maxHp} (페이즈: {(isPhase2 ? "2" : "1")})");
        if (!isPhase2 && !hasTriggeredTransitionBarrier &&
        currentHp <= maxHp * phase2HealthThreshold)
        {
            TriggerPhaseTransitionBarrier();
            return;
        }

        if (currentHp <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
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

        Destroy(gameObject, 3f);
        playerData.AddGold(dropGold);
    }
    void TriggerPhaseTransitionBarrier()
{
    if (hasTriggeredTransitionBarrier) return;
    
    hasTriggeredTransitionBarrier = true;
    isPhaseTransitionTriggered = true;
    
    Debug.Log("=== 페이즈 전환 배리어 패턴 시작! ===");
    
    // 강제로 배리어 패턴 실행
    ForcePerformShieldPattern();
}

    void ForcePerformShieldPattern() //  강제 배리어 패턴 실행
    {
        
        if (isShieldPatternActive)
    {
        Debug.Log("이미 배리어 패턴이 활성화되어 있음");
        return;
    }
        Debug.Log("배리어 패턴 시작 시도!");
        // 애니메이션 트리거 확인
    if (animator != null)
    {
        animator.SetTrigger("Shield");
        Debug.Log("Shield 애니메이션 트리거 실행");
    }   
        // 모든 다른 행동 정지
        isAttacking = false;
        isJumping = false;
        isBackingAway = false;
        soundManager?.PlayShieldPatternSound();
        // 배리어 패턴 실행
        PerformShieldPattern();
        
        Debug.Log("페이즈 전환을 위한 배리어 패턴 강제 실행");
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
