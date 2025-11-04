using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using EnemyAI;

public class GolemAI : MonoBehaviour, IDamageable
{
    [Header("기본 설정")]
    [SerializeField] public float maxHp = 150f;
    public float currentHp;
    private bool isDead = false;
    [SerializeField] int dropGold = 3000;
    
    // LockOnSystem이 사망 상태를 감지할 수 있도록 public 프로퍼티 제공
    public float CurrentHealth => currentHp;
    public bool IsDead => isDead;

    [Header("피격 사운드")]
    [SerializeField] private AudioClip[] hitSounds;
    [SerializeField] private float hitSoundVolume = 0.8f;

    [Header("공격 & 조우 사운드")]
    [SerializeField] private AudioClip leftPunchSound;
    [SerializeField] private AudioClip rightPunchSound;
    [SerializeField] private AudioClip groundSlamSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip encounterSound;
    [SerializeField] private float encounterRange = 25f;
    private bool hasPlayedEncounterSound = false;

    private AudioSource audioSource;

    [Header("페이즈 설정")]
    [SerializeField] float phase2HpThreshold = 0.5f;
    private bool isPhase2 = false;
    private bool hasTriggeredPhase2 = false;
    public bool isPhase2Transitioning = false;

    [Header("이동 설정")]
    [SerializeField] float walkSpeed = 1.5f;
    [SerializeField] float phase2WalkSpeed = 2.2f;
    [SerializeField] float rotationSpeed = 2f;

    [Header("공격 설정")]
    [SerializeField] float attackRange = 4f;
    [SerializeField] float closeRange = 2.5f;
    [SerializeField] float leftPunchDamage = 3f;
    [SerializeField] float rightPunchDamage = 4f;
    [SerializeField] float groundSlamDamage = 8f;
    [SerializeField] float attackCooldown = 2f;

    [SerializeField] float phase2DamageMultiplier = 1.1f;
    [SerializeField] float phase2AttackCooldown = 1.5f;

    [Header("바닥치기 공격 설정")]
    [SerializeField] GameObject rockSpikePrefab;
    [SerializeField] float spikeDistance = 6f;
    [SerializeField] int spikeCount = 5;
    [SerializeField] float spikeDelay = 0.2f;

    [Header("2페이즈 이펙트 설정")]
    [SerializeField] GameObject largeEarthquakePrefab;
    [SerializeField] float largeEarthquakeRadius = 15f;
    [SerializeField] float largeEarthquakeDamage = 10f;

    private GameObject earthQuakeEffect;
    private bool phase2EntryLock = false;
    

    [Header("연계 공격 설정")]
    [SerializeField] float comboAttackProbability = 0.3f;
    private bool isComboAttacking = false;

    [Header("행동 확률 설정")]
    [SerializeField] float walkAttackProbability = 0.4f;
    [SerializeField] float stationaryAttackProbability = 0.6f;

    [SerializeField] float phase2WalkAttackProbability = 0.3f;
    [SerializeField] float phase2StationaryAttackProbability = 0.7f;

    private float attackTimer = 0f;
    private bool isAttacking = false;
    private bool isWalkAttacking = false;

    [Header("레이어 관리")]
    [SerializeField] float upperBodyLayerWeight = 1f;
    private int upperBodyLayerIndex;

    private Transform player;
    private NavMeshAgent agent;
    private Animator animator;
    private Collider mainCollider;
    [Header("사운드 매니저")]
    private GolemSoundManager soundManager;

    [Header("보스 UI")]
    [SerializeField] private SimpleBossHealthBar bossHealthBar;
    [SerializeField] private string bossName = "고대의 수호자";
    private bool isVisible = false;

    [Header("공격 판정 콜라이더")]
    [SerializeField] AttackCollider leftPunchCollider;
    [SerializeField] AttackCollider rightPunchCollider;

    private enum GolemAttackType
    {
        LeftPunch, RightPunch, GroundSlam, ComboAttack, ShieldSlam
    }
    private GolemAttackType currentAttackType;
    PlayerDataManager playerData;

    [Header("Behavior Tree Mode")]
    [SerializeField] private bool useBehaviorTree = true;
    private GolemBehaviorTree golemBehaviorTree;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        animator = GetComponent<Animator>();
        mainCollider = GetComponent<Collider>();

        upperBodyLayerIndex = animator.GetLayerIndex("Upper Body Layer");
        if (upperBodyLayerIndex == -1)
        {
            Debug.LogError("Upper Body Layer를 찾을 수 없습니다!");
        }
        playerData = FindFirstObjectByType<PlayerDataManager>();
        InitializeAttackColliders();
    }

    void InitializeAttackColliders()
    {
        var colliders = new AttackCollider[] { leftPunchCollider, rightPunchCollider };
        var damages = new float[] { leftPunchDamage, rightPunchDamage };

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].gameObject.SetActive(false);
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

        DisableAttackCollider();

        if (bossHealthBar == null)
            bossHealthBar = FindFirstObjectByType<SimpleBossHealthBar>();
        if (bossHealthBar != null)
        {
            bossHealthBar.ShowBossHealthBar(bossName, maxHp, currentHp);
            isVisible = true;
            Debug.Log("골렘 씬 로드 완료! 체력바 즉시 표시");
        }

        soundManager = GetComponent<GolemSoundManager>();
        if (soundManager == null)
            soundManager = gameObject.AddComponent<GolemSoundManager>();

        if (useBehaviorTree)
        {
            golemBehaviorTree = GetComponent<GolemBehaviorTree>();
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.maxDistance = 40f;
        }
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else Debug.LogError("Player 태그를 찾을 수 없습니다.");
    }

    void Update()
    {
        if (isDead || player == null) return;

        if (!hasPlayedEncounterSound && player != null)
        {
            if (Vector3.Distance(transform.position, player.position) < encounterRange)
            {
                PlaySound(encounterSound);
                hasPlayedEncounterSound = true;
                Debug.Log("플레이어 발견! 골렘 조우 사운드를 재생합니다.");
            }
        }

        // Phase2 체크는 Behavior Tree 모드에서도 필요합니다
        CheckPhase2Transition();

        // Behavior Tree 모드일 때는 여기서 종료 (수동 입력 처리 스킵)
        if (useBehaviorTree) return;

        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("H키 - 강제 바닥치기 공격 실행!");
            TestGroundSlam();
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            Debug.Log("J키 - 강제 연계 공격 실행!");
            PerformComboAttack();
        }

        float distance = Vector3.Distance(transform.position, player.position);

        UpdateTimers();
        RotateTowardsPlayer();
        HandleCombatState(distance);
        HandleMovement(distance);
    }

    private void PlaySound(AudioClip clip, float volume = 0.7f)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    void CheckPhase2Transition()
    {
        if (!isPhase2 && currentHp <= maxHp * phase2HpThreshold)
        {
            TriggerPhase2();
        }
    }

    [Header("2페이즈 아우라 설정")]
    [SerializeField] private GameObject phase2AuraEffect;  // Inspector에서 할당할 아우라 이펙트
    private GameObject currentAuraEffect;

    void TriggerPhase2()
    {
        // 이미 2페이즈 전환 중이거나 완료된 경우 무시
        if (isPhase2Transitioning || hasTriggeredPhase2) return;

        // 진행 중인 모든 코루틴과 상태 초기화
        StopAllCoroutines();
        StopMovement();
        DisableAttackCollider();

        // 2페이즈 상태로 변경
        hasTriggeredPhase2 = true;
        isPhase2 = true;
        isPhase2Transitioning = true;
        isAttacking = true;  // 다른 공격을 막기 위해 추가

        Debug.Log("=== 골렘 2페이즈 돌입! ===");

        // 2페이즈 스탯 업데이트
        walkSpeed = phase2WalkSpeed;
        attackCooldown = phase2AttackCooldown;
        UpdatePhase2Damage();

        // 2페이즈 아우라 생성
        if (phase2AuraEffect != null && currentAuraEffect == null)
        {
            currentAuraEffect = Instantiate(phase2AuraEffect, transform.position, Quaternion.identity);
            currentAuraEffect.transform.SetParent(transform);
            Debug.Log("2페이즈 아우라 생성!");
        }
        
        // 2페이즈 전환 이펙트 시작
        StartCoroutine(Phase2TransitionEffect());
    }

    IEnumerator ReleasePhase2EntryLock()
    {
        // keep the lock for a short period to avoid duplicate post-transition effects
        yield return new WaitForSeconds(4f);
        phase2EntryLock = false;
    }

    void UpdatePhase2Damage()
    {
        if (leftPunchCollider != null && leftPunchCollider.damageAmount > 0)
            leftPunchCollider.damageAmount *= phase2DamageMultiplier;
        if (rightPunchCollider != null && rightPunchCollider.damageAmount > 0)
            rightPunchCollider.damageAmount *= phase2DamageMultiplier;
    }

    IEnumerator Phase2TransitionEffect()
    {
        // 이미 실행 중인지 확인
        if (!isPhase2Transitioning)
        {
            Debug.LogWarning("Phase2TransitionEffect가 isPhase2Transitioning=false 상태에서 호출됨!");
            yield break;
        }

        Debug.Log("Phase2TransitionEffect 시작!");
        
        // 모든 상태 초기화 및 락 설정
        isAttacking = true;
        phase2EntryLock = true;
        isComboAttacking = false;
        isWalkAttacking = false;

        // 진행 중인 모든 애니메이션 리셋
        animator.Rebind();
        animator.Update(0f);

        // 애니메이터 초기화 및 전환 애니메이션 설정
        animator.SetTrigger("Phase2GroundSlam");
        animator.ResetTrigger("GroundSlam");
        animator.SetBool("WalkPunch1", false);
        animator.SetBool("WalkPunch2", false);

        PlaySound(groundSlamSound);
        Debug.Log("2페이즈 전환 애니메이션 시작");

        // 첫 번째 임팩트 타이밍까지 대기
        yield return new WaitForSeconds(1.2f);

        // 임팩트 생성
        if (largeEarthquakePrefab != null)
        {
            GameObject quake = Instantiate(largeEarthquakePrefab, transform.position, Quaternion.identity);
            DealEarthquakeDamage(largeEarthquakeRadius, largeEarthquakeDamage * phase2DamageMultiplier, "큰 지진");
            Debug.Log("2페이즈 진입: 큰 지진 발생");
            Destroy(quake, 3.0f); // 이펙트가 완전히 사라질때까지 충분한 시간
        }

        // 모든 애니메이션과 이펙트가 완료될 때까지 대기
        yield return new WaitForSeconds(3.0f);
        Debug.Log("2페이즈 진입 완료!");
        
        // 모든 상태 초기화
        isPhase2Transitioning = false;
        isAttacking = false;
        isComboAttacking = false;
        isWalkAttacking = false;

        // 락 해제 타이머 시작
        StartCoroutine(ReleasePhase2EntryLock());
    }

    void TestGroundSlam()
    {
        StopMovement();
        isAttacking = true;
        currentAttackType = GolemAttackType.GroundSlam;

        animator.SetTrigger("GroundSlam");
        PlaySound(groundSlamSound);

        attackTimer = isPhase2 ? phase2AttackCooldown : attackCooldown;
        Debug.Log("테스트: 바닥치기 공격 실행!");
    }

    void UpdateTimers()
    {
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
        }
    }

    void RotateTowardsPlayer()
    {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        dirToPlayer.y = 0;

        if (dirToPlayer.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dirToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void HandleCombatState(float distance)
    {
        Debug.Log($"전투 상태 체크 - 페이즈: {(isPhase2 ? "2" : "1")}, 거리: {distance:F2}, 공격중: {isAttacking}, 연계공격: {isComboAttacking}, 공격타이머: {attackTimer:F2}");

        if (isAttacking || isComboAttacking) return;

        if (distance <= attackRange && attackTimer <= 0)
        {
            float actionRoll = Random.Range(0f, 1f);
            Debug.Log($"공격 결정 - 랜덤값: {actionRoll:F2}");

            if (isPhase2)
            {
                HandlePhase2Combat(actionRoll);
            }
            else
            {
                HandlePhase1Combat(actionRoll);
            }
        }
    }

    void HandlePhase1Combat(float actionRoll)
    {
        if (actionRoll < walkAttackProbability)
        {
            PerformWalkAttack();
        }
        else if (actionRoll < walkAttackProbability + stationaryAttackProbability)
        {
            PerformStationaryAttack();
        }
    }

    void HandlePhase2Combat(float actionRoll)
    {
        // 쉴드 패턴 제거 후 공격 패턴 조정
        float totalProbability = comboAttackProbability + phase2WalkAttackProbability + phase2StationaryAttackProbability;
        float normalizedRoll = actionRoll * totalProbability;
        
        if (normalizedRoll < comboAttackProbability)
        {
            PerformComboAttack();
        }
        else if (normalizedRoll < comboAttackProbability + phase2WalkAttackProbability)
        {
            PerformWalkAttack();
        }
        else
        {
            PerformStationaryAttack();
        }
    }

    public void PerformShieldSlam()
    {
        Debug.Log("쉴드 슬램 패턴 시작!");
        StartCoroutine(ShieldSlamPattern());
    }

    IEnumerator ShieldSlamPattern()
    {
        isAttacking = true;
        StopMovement();
        currentAttackType = GolemAttackType.ShieldSlam;

        // 일반 바닥치기 공격
        animator.SetTrigger("GroundSlam");
        PlaySound(groundSlamSound);

        yield return new WaitForSeconds(1.2f);

        // 큰 지진만 생성
        if (largeEarthquakePrefab != null)
        {
            GameObject quake = Instantiate(largeEarthquakePrefab, transform.position, Quaternion.identity);
            DealEarthquakeDamage(largeEarthquakeRadius, largeEarthquakeDamage * (isPhase2 ? phase2DamageMultiplier : 1f), "큰 지진");
            Debug.Log("쉴드 슬램: 큰 지진 발생");
            Destroy(quake, 2.0f);
        }

        yield return new WaitForSeconds(2.0f); // 이펙트가 완전히 사라질 때까지 대기

        isAttacking = false;
        attackTimer = isPhase2 ? phase2AttackCooldown : attackCooldown;
        Debug.Log("쉴드 슬램 패턴 완료!");
    }

    void HandleMovement(float distance)
    {
        if (isComboAttacking)
        {
            StopMovement();
            return;
        }

        if (isWalkAttacking)
        {
            ContinueWalkAttack();
        }
        else if (!isAttacking && distance > attackRange)
        {
            MoveTowardsPlayer();
        }
        else if (!isAttacking && distance <= attackRange)
        {
            StopMovement();
        }
    }

    void MoveTowardsPlayer()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);
        agent.speed = walkSpeed;

        Vector3 localDirection = transform.InverseTransformDirection(agent.velocity.normalized);
        animator.SetFloat("MoveX", localDirection.x);
        animator.SetFloat("MoveY", localDirection.z);

        bool isMoving = agent.velocity.magnitude > 0.1f;
        animator.SetBool("IsMoving", isMoving);
    }

    void ContinueWalkAttack()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);
        agent.speed = walkSpeed * 0.8f;

        Vector3 localDirection = transform.InverseTransformDirection(agent.velocity.normalized);
        animator.SetFloat("MoveX", localDirection.x);
        animator.SetFloat("MoveY", localDirection.z);

        bool isMoving = agent.velocity.magnitude > 0.1f;
        animator.SetBool("IsMoving", isMoving);
    }

    void StopMovement()
    {
        agent.isStopped = true;
        animator.SetFloat("MoveX", 0f);
        animator.SetFloat("MoveY", 0f);
        animator.SetBool("IsMoving", false);
    }

    public void PerformWalkAttack()
    {
        isWalkAttacking = true;
        isAttacking = true;

        int randomAttack = Random.Range(0, 2);
        if (randomAttack == 0)
        {
            currentAttackType = GolemAttackType.LeftPunch;
            animator.SetBool("WalkPunch1", true);
            animator.SetTrigger("LeftPunch");
            PlaySound(leftPunchSound);
        }
        else
        {
            currentAttackType = GolemAttackType.RightPunch;
            animator.SetBool("WalkPunch2", true);
            animator.SetTrigger("RightPunch");
            PlaySound(rightPunchSound);
        }

        if (useBehaviorTree) golemBehaviorTree.SetAttacking(true);
        attackTimer = isPhase2 ? phase2AttackCooldown : attackCooldown;
        Debug.Log($"이동하면서 공격: {currentAttackType}");
    }

    public void PerformStationaryAttack()
    {
        StopMovement();
        isAttacking = true;

        int randomAttack = Random.Range(0, 3);
        currentAttackType = (GolemAttackType)randomAttack;

        switch (currentAttackType)
        {
            case GolemAttackType.LeftPunch:
                animator.SetTrigger("LeftPunch");
                PlaySound(leftPunchSound);
                break;
            case GolemAttackType.RightPunch:
                animator.SetTrigger("RightPunch");
                PlaySound(rightPunchSound);
                break;
            case GolemAttackType.GroundSlam:
                animator.SetTrigger("GroundSlam");
                PlaySound(groundSlamSound);
                break;
        }

        if (useBehaviorTree) golemBehaviorTree.SetAttacking(true);
        attackTimer = isPhase2 ? phase2AttackCooldown : attackCooldown;
        Debug.Log($"제자리 공격: {currentAttackType}");
    }

    public void PerformComboAttack()
    {
        if (isComboAttacking) return;

        Debug.Log("연계 공격 시작: LeftPunch → RightPunch");
        StartCoroutine(ComboAttackSequence());
    }

    IEnumerator ComboAttackSequence()
    {
        isComboAttacking = true;
        isAttacking = true;
        StopMovement();
        if (useBehaviorTree) golemBehaviorTree.SetAttacking(true);

        animator.SetBool("ComboAttack", true);
        animator.SetInteger("ComboStep", 1);

        currentAttackType = GolemAttackType.LeftPunch;
        animator.SetTrigger("LeftPunch");
        PlaySound(leftPunchSound);
        Debug.Log("연계 공격 1단계: 왼손 펀치");

        yield return new WaitForSeconds(1.2f);

        animator.SetInteger("ComboStep", 2);

        currentAttackType = GolemAttackType.RightPunch;
        animator.SetTrigger("RightPunch");
        PlaySound(rightPunchSound);
        Debug.Log("연계 공격 2단계: 오른손 펀치");

        yield return new WaitForSeconds(1.2f);

        animator.SetBool("ComboAttack", false);
        animator.SetInteger("ComboStep", 0);

        isComboAttacking = false;
        isAttacking = false;
        if (useBehaviorTree) golemBehaviorTree.SetAttacking(false);
        attackTimer = isPhase2 ? phase2AttackCooldown : attackCooldown;

        Debug.Log("연계 공격 완료!");

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > attackRange)
        {
            MoveTowardsPlayer();
        }
    }



    void DealEarthquakeDamage(float radius, float damage, string earthquakeType)
    {
        soundManager?.PlayEarthquakeSound();
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);

        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damage);
                    Debug.Log($"{earthquakeType} 데미지! 플레이어에게 {damage} 데미지! (2페이즈: {isPhase2})");
                }
            }
        }
    }

    public void OnGroundSlamHit()
    {
        // 2페이즈 전환 중일 때는 아무것도 하지 않음
        if (isPhase2Transitioning)
        {
            return;
        }

        // 일반 바닥치기 공격일 때만 바위 송곳 생성
        Debug.Log("바닥치기 임팩트! 바위 송곳 생성 시작!");
        StartCoroutine(CreateRockSpikes());
    }

    IEnumerator CreateRockSpikes()
    {
        Vector3 golemForward = transform.forward;
        Vector3 startPosition = transform.position + golemForward * 1f;

        var hitTargets = new System.Collections.Generic.List<Collider>();

        for (int i = 0; i < spikeCount; i++)
        {
            Vector3 spikePosition = startPosition + golemForward * i * (spikeDistance / spikeCount);

            if (rockSpikePrefab != null)
            {
                GameObject spike = Instantiate(rockSpikePrefab, spikePosition, Quaternion.LookRotation(golemForward));

                ParticleSystem spikeParticle = spike.GetComponent<ParticleSystem>();
                if (spikeParticle != null)
                {
                    var shape = spikeParticle.shape;
                    shape.rotation = new Vector3(0, 0, 0);
                }

                RockSpike spikeScript = spike.GetComponent<RockSpike>();
                if (spikeScript == null)
                {
                    spikeScript = spike.AddComponent<RockSpike>();
                }

                float finalDamage = groundSlamDamage * (isPhase2 ? phase2DamageMultiplier : 1f);

                spikeScript.Initialize(finalDamage, hitTargets);
            }

            yield return new WaitForSeconds(spikeDelay);
        }
    }

    public void EnableAttackCollider()
    {
        DisableAttackCollider();

        switch (currentAttackType)
        {
            case GolemAttackType.LeftPunch:
                if (leftPunchCollider) leftPunchCollider.gameObject.SetActive(true);
                break;
            case GolemAttackType.RightPunch:
                if (rightPunchCollider) rightPunchCollider.gameObject.SetActive(true);
                break;
            case GolemAttackType.GroundSlam:
                Debug.Log("바닥치기는 파티클 데미지로 처리됩니다.");
                break;
        }
    }

    public void DisableAttackCollider()
    {
        var colliders = new AttackCollider[] { leftPunchCollider, rightPunchCollider };

        foreach (var collider in colliders)
        {
            if (collider) collider.gameObject.SetActive(false);
        }
    }

    public void OnAttackAnimationEnd()
    {
        if (isComboAttacking) return;

        Debug.Log("공격 애니메이션 종료!");
        isAttacking = false;
        isWalkAttacking = false;

        if (useBehaviorTree) golemBehaviorTree.SetAttacking(false);
        DisableAttackCollider();

        animator.ResetTrigger("LeftPunch");
        animator.ResetTrigger("RightPunch");
        animator.ResetTrigger("GroundSlam");
    // Reset the phase2-specific slam trigger as well in case it was used
    animator.ResetTrigger("Phase2GroundSlam");

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > attackRange)
        {
            MoveTowardsPlayer();
        }
    }

    public void OnWalkAttackAnimationEnd()
    {
        Debug.Log("이동 공격 애니메이션 종료!");
        isWalkAttacking = false;
        isAttacking = false;

        if (useBehaviorTree) golemBehaviorTree.SetAttacking(false);
        DisableAttackCollider();

        animator.SetBool("WalkPunch1", false);
        animator.SetBool("WalkPunch2", false);

        animator.ResetTrigger("LeftPunch");
        animator.ResetTrigger("RightPunch");

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > attackRange)
        {
            MoveTowardsPlayer();
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHp -= damageAmount;

        PlayHitSound();

        bossHealthBar?.UpdateHealth(currentHp);

        Debug.Log($"골렘이 {damageAmount} 데미지를 받았습니다. 현재 HP: {currentHp}/{maxHp} (페이즈: {(isPhase2 ? "2" : "1")})");

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

    void Die()
    {
        isDead = true;
        PlaySound(deathSound);

        bossHealthBar?.HideBossHealthBar();

        animator.SetTrigger("Death");
        mainCollider.enabled = false;
        agent.isStopped = true;

        // 2페이즈 아우라 제거
        if (currentAuraEffect != null)
        {
            Destroy(currentAuraEffect);
            currentAuraEffect = null;
        }

        if (upperBodyLayerIndex != -1)
        {
            animator.SetLayerWeight(upperBodyLayerIndex, 0f);
        }
        soundManager?.PlayDeathSound();
        Destroy(gameObject, 5f);
        Debug.Log("골렘 사망!");
        playerData.AddGold(dropGold);

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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, closeRange);

        if (isPhase2)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 3f, Vector3.one);
            
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(transform.position, largeEarthquakeRadius);
        }
    }

    public bool IsPhase2() => isPhase2;
    public float GetRotationSpeed() => rotationSpeed;
}
