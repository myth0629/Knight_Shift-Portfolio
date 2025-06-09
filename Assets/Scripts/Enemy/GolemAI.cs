    using UnityEngine;
    using UnityEngine.AI;
    using System.Collections;

    public class GolemAI : MonoBehaviour, IDamageable
    {
        [Header("기본 설정")]
        //골렘의 최대 체력
        [SerializeField] public float maxHp = 150f;
        // 현재 체력 
        public float currentHp;
        private bool isDead = false;
        [SerializeField] int dropGold = 3000; // 골렘 처치 시 드랍되는 골드 양

        [Header("페이즈 설정")]
        [SerializeField] float phase2HpThreshold = 0.5f; // HP 50% 이하에서 2페이즈
        private bool isPhase2 = false;
        private bool hasTriggeredPhase2 = false;

        [Header("이동 설정")]
        [SerializeField] float walkSpeed = 1.5f;
        [SerializeField] float phase2WalkSpeed = 2.2f; // 2페이즈 이동속도 증가
        [SerializeField] float rotationSpeed = 2f;

        [Header("공격 설정")]
        [SerializeField] float attackRange = 4f;
        [SerializeField] float closeRange = 2.5f;
        [SerializeField] float leftPunchDamage = 30f;
        [SerializeField] float rightPunchDamage = 30f;
        [SerializeField] float groundSlamDamage = 45f;
        [SerializeField] float attackCooldown = 2f;
        
        // 2페이즈 데미지 증가
        [SerializeField] float phase2DamageMultiplier = 1.5f;
        [SerializeField] float phase2AttackCooldown = 1.5f;

        [Header("바닥치기 공격 설정")]
        [SerializeField] GameObject rockSpikePrefab;
        [SerializeField] float spikeDistance = 6f;
        [SerializeField] int spikeCount = 5;
        [SerializeField] float spikeDelay = 0.2f;

        [Header("쉴드 패턴 설정 (2페이즈 전용)")]
        [SerializeField] GameObject golemShieldPrefab;
        [SerializeField] GameObject smallEarthquakePrefab;
        [SerializeField] GameObject largeEarthquakePrefab;
        [SerializeField] float smallEarthquakeRadius = 8f;
        [SerializeField] float largeEarthquakeRadius = 15f;
        [SerializeField] float smallEarthquakeDamage = 25f;
        [SerializeField] float largeEarthquakeDamage = 50f;

        private bool isShieldPatternActive = false;
        private GameObject golemShield;
        private GameObject smallEarthquakeEffect;
        private GameObject largeEarthquakeEffect;

        [Header("연계 공격 설정")]
        [SerializeField] float comboAttackProbability = 0.3f; // 2페이즈에서 연계 공격 확률
        private bool isComboAttacking = false;

        [Header("행동 확률 설정")]
        [SerializeField] float walkAttackProbability = 0.4f;
        [SerializeField] float stationaryAttackProbability = 0.5f;
        [SerializeField] float shieldPatternProbability = 0.1f;
        
        // 2페이즈 확률 (쉴드 패턴 추가)
        [SerializeField] float phase2WalkAttackProbability = 0.3f;
        [SerializeField] float phase2StationaryAttackProbability = 0.4f;
        [SerializeField] float phase2ShieldPatternProbability = 0.3f;

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
        [SerializeField] private string bossName = "고대의 수호자"; // 한국어 이름
        private bool isVisible = false; // 체력바 표시 여부 추가

        [Header("공격 판정 콜라이더")]
        [SerializeField] AttackCollider leftPunchCollider;
        [SerializeField] AttackCollider rightPunchCollider;

        private enum GolemAttackType
        {
            LeftPunch, RightPunch, GroundSlam, ComboAttack
        }
        private GolemAttackType currentAttackType;
        PlayerDataManager playerData;

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
            InitializeAttackColliders(); // 공격 판정 콜라이더 초기화
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
                    colliders[i].damageAmount = damages[i];
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
            bossHealthBar = FindObjectOfType<SimpleBossHealthBar>();
        if (bossHealthBar != null)
        {
            bossHealthBar.ShowBossHealthBar(bossName, maxHp, currentHp);
            isVisible = true;
            Debug.Log("골렘 씬 로드 완료! 체력바 즉시 표시");
        }
        // 사운드 매니저 초기화
        soundManager = GetComponent<GolemSoundManager>();
        if (soundManager == null)
            soundManager = gameObject.AddComponent<GolemSoundManager>();
           
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

            // 2페이즈 전환 체크
            CheckPhase2Transition();

            // 테스트 키들
            if (Input.GetKeyDown(KeyCode.G))
            {
                Debug.Log("G키 - 강제 쉴드 패턴 실행!");
                PerformShieldPattern();
            }

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

        void CheckPhase2Transition()
        {
            if (!hasTriggeredPhase2 && currentHp <= maxHp * phase2HpThreshold)
            {
                TriggerPhase2();
            }
        }

    void TriggerPhase2()
    {
        hasTriggeredPhase2 = true;
        isPhase2 = true;

        Debug.Log("=== 골렘 2페이즈 돌입! ===");

        // 이동속도 증가
        walkSpeed = phase2WalkSpeed;

        // 공격 쿨다운 감소
        attackCooldown = phase2AttackCooldown;

        // 데미지 증가 (콜라이더 업데이트)
        UpdatePhase2Damage();

        // 2페이즈 진입 효과 (파티클 등)
        StartCoroutine(Phase2TransitionEffect());
            
        }

        void UpdatePhase2Damage()
        {
            if (leftPunchCollider != null)
                leftPunchCollider.damageAmount = leftPunchDamage * phase2DamageMultiplier;
            
            if (rightPunchCollider != null)
                rightPunchCollider.damageAmount = rightPunchDamage * phase2DamageMultiplier;
        }

        IEnumerator Phase2TransitionEffect()
        {
            // 잠시 멈추고 2페이즈 진입 연출
            StopMovement();
            isAttacking = true; // 진입 중에는 다른 행동 금지
            
            // 진입 효과 (예: 쉴드 잠깐 생성)
            if (golemShieldPrefab != null)
            {
                Vector3 shieldPosition = transform.position + Vector3.down * 1.5f;
                GameObject transitionShield = Instantiate(golemShieldPrefab, shieldPosition, Quaternion.identity);
                transitionShield.transform.SetParent(transform);
                
                yield return new WaitForSeconds(2f);
                
                
            }
            
            isAttacking = false;
            yield return new WaitForSeconds(0.2f);
            
            PerformShieldPattern();
            Debug.Log("2페이즈 진입 완료!");
        }

        void TestGroundSlam()
        {
            StopMovement();
            isAttacking = true;
            currentAttackType = GolemAttackType.GroundSlam;
            
            animator.SetTrigger("GroundSlam");
            
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
            if (isShieldPatternActive) return;
            
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
            Debug.Log($"전투 상태 체크 - 페이즈: {(isPhase2 ? "2" : "1")}, 거리: {distance:F2}, 공격중: {isAttacking}, 쉴드패턴: {isShieldPatternActive}, 연계공격: {isComboAttacking}, 공격타이머: {attackTimer:F2}");
            
            if (isShieldPatternActive || isAttacking || isComboAttacking) return;

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
        // 1페이즈에서는 쉴드 패턴 없음
        
        }

        void HandlePhase2Combat(float actionRoll)
        {
            // 2페이즈에서는 연계 공격 추가
            if (actionRoll < comboAttackProbability)
            {
                PerformComboAttack();
            }
            else if (actionRoll < comboAttackProbability + phase2WalkAttackProbability)
            {
                PerformWalkAttack();
            }
            else if (actionRoll < comboAttackProbability + phase2WalkAttackProbability + phase2StationaryAttackProbability)
            {
                PerformStationaryAttack();
            }
            else
            {
                PerformShieldPattern(); // 2페이즈에서만 쉴드 패턴 사용
            }
        }

        void HandleMovement(float distance)
        {
            if (isShieldPatternActive || isComboAttacking)
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
            agent.speed = walkSpeed; // 2페이즈에서는 자동으로 빨라짐

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

        void PerformWalkAttack()
        {
            isWalkAttacking = true;
            isAttacking = true;
            
            int randomAttack = Random.Range(0, 2);
            if (randomAttack == 0)
            {
                currentAttackType = GolemAttackType.LeftPunch;
                animator.SetBool("WalkPunch1", true);
                animator.SetTrigger("LeftPunch");
            }
            else
            {
                currentAttackType = GolemAttackType.RightPunch;
                animator.SetBool("WalkPunch2", true);
                animator.SetTrigger("RightPunch");
            }

            attackTimer = isPhase2 ? phase2AttackCooldown : attackCooldown;
            Debug.Log($"이동하면서 공격: {currentAttackType}");
        }

        void PerformStationaryAttack()
        {
            StopMovement();
            isAttacking = true;
            
            int randomAttack = Random.Range(0, 3);
            currentAttackType = (GolemAttackType)randomAttack;

            switch (currentAttackType)
            {
                case GolemAttackType.LeftPunch:
                    animator.SetTrigger("LeftPunch");
                    
                    break;
                case GolemAttackType.RightPunch:
                    animator.SetTrigger("RightPunch");
                    
                    break;
                case GolemAttackType.GroundSlam:
                    animator.SetTrigger("GroundSlam");
                    
                    break;
            }

            attackTimer = isPhase2 ? phase2AttackCooldown : attackCooldown;
            Debug.Log($"제자리 공격: {currentAttackType}");
        }

        // 새로운 연계 공격 (Punch1 → Punch2)
        void PerformComboAttack()
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

        // 연계 공격 시작 표시
        animator.SetBool("ComboAttack", true);
        animator.SetInteger("ComboStep", 1);

        // 1단계: 왼손 펀치
        currentAttackType = GolemAttackType.LeftPunch;
        animator.SetTrigger("LeftPunch");
        Debug.Log("연계 공격 1단계: 왼손 펀치");

        // 첫 번째 공격이 끝날 때까지 대기
        yield return new WaitForSeconds(1.2f);

        // 2단계로 전환
        animator.SetInteger("ComboStep", 2);

        // 2단계: 오른손 펀치 (연계)
        currentAttackType = GolemAttackType.RightPunch;
        animator.SetTrigger("RightPunch");
        Debug.Log("연계 공격 2단계: 오른손 펀치");

        // 두 번째 공격이 끝날 때까지 대기
        yield return new WaitForSeconds(1.2f);

        // 연계 공격 완료
        animator.SetBool("ComboAttack", false);
        animator.SetInteger("ComboStep", 0);
        
        isComboAttacking = false;
        isAttacking = false;
        attackTimer = isPhase2 ? phase2AttackCooldown : attackCooldown;
        
        Debug.Log("연계 공격 완료!");
        
        // 연계 공격 후 이동 재개
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > attackRange)
        {
            MoveTowardsPlayer();
        }
    }


        void PerformShieldPattern()
        {
            if (isShieldPatternActive || !isPhase2) return; // 2페이즈에서만 사용 가능
            animator.SetBool("ShieldPattern", true);
            Debug.Log("골렘 쉴드 패턴 시작! (2페이즈 전용)");
            
            StartCoroutine(ShieldEarthquakePattern());
        }

    IEnumerator ShieldEarthquakePattern()
    {
        isShieldPatternActive = true;
        animator.SetBool("ShieldPattern", true);
        StopMovement();

        // 쉴드 생성
        if (golemShieldPrefab != null)
        {
            Vector3 shieldPosition = transform.position + Vector3.down * 1.5f;
            golemShield = Instantiate(golemShieldPrefab, shieldPosition, Quaternion.identity);
            golemShield.transform.SetParent(transform);
            Debug.Log("골렘 쉴드 생성!");
        }

        // === 1단계: 작은 지진 ===
        if (smallEarthquakePrefab != null)
        {
            smallEarthquakeEffect = Instantiate(smallEarthquakePrefab, transform.position, Quaternion.identity);
            Debug.Log("작은 지진 파티클 생성!");
        }

        yield return new WaitForSeconds(0.3f);
        DealEarthquakeDamage(smallEarthquakeRadius, smallEarthquakeDamage * (isPhase2 ? phase2DamageMultiplier : 1f), "작은 지진");

        yield return new WaitForSeconds(0.5f);
        if (smallEarthquakeEffect != null) Destroy(smallEarthquakeEffect);

        // === 2단계: 큰 지진 ===
        yield return new WaitForSeconds(0.5f);

        if (largeEarthquakePrefab != null)
        {
            largeEarthquakeEffect = Instantiate(largeEarthquakePrefab, transform.position, Quaternion.identity);
            Debug.Log("큰 지진 파티클 생성!");
        }

        yield return new WaitForSeconds(0.3f);
        DealEarthquakeDamage(largeEarthquakeRadius, largeEarthquakeDamage * (isPhase2 ? phase2DamageMultiplier : 1f), "큰 지진");
        
        // === 패턴 즉시 종료 ===
        yield return new WaitForSeconds(0.5f);

        // 모든 이펙트 즉시 정리
        if (golemShield != null)
        {
            Destroy(golemShield);
            Debug.Log("쉴드 파티클 삭제!");
        }
        if (largeEarthquakeEffect != null)
        {
            Destroy(largeEarthquakeEffect);
            Debug.Log("큰 지진 파티클 삭제!");
        }
        // 패턴 완료 직전에 2페이즈 효과음 재생
        soundManager?.PlayShieldPatternEndSound(); // 2페이즈 완료 효과음

        animator.SetBool("ShieldPattern", false);
        isShieldPatternActive = false;
        attackTimer = isPhase2 ? phase2AttackCooldown : attackCooldown;
        Debug.Log("쉴드 패턴 완료!");

        // 패턴 종료 후 즉시 이동 시작
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > attackRange)
        {
            MoveTowardsPlayer();
            Debug.Log("쉴드 패턴 종료 - 골렘 이동 시작!");
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
            Debug.Log("바닥치기 임팩트! 바위 송곳 생성 시작!");
            StartCoroutine(CreateRockSpikes());
        }

        IEnumerator CreateRockSpikes()
        {
            Vector3 golemForward = transform.forward;
            Vector3 startPosition = transform.position + golemForward * 1f;
            
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
                    // 2페이즈에서는 바닥치기 데미지도 증가
                    float finalDamage = groundSlamDamage * (isPhase2 ? phase2DamageMultiplier : 1f);
                    spikeScript.Initialize(finalDamage);
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
            // 연계 공격 중이면 개별적으로 처리하지 않음
            if (isComboAttacking) return;
            
            Debug.Log("공격 애니메이션 종료!");
            isAttacking = false;
            isWalkAttacking = false;
            
            DisableAttackCollider();
            
            animator.ResetTrigger("LeftPunch");
            animator.ResetTrigger("RightPunch");
            animator.ResetTrigger("GroundSlam");
            
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
            
            // 체력바 업데이트 (페이즈 파라미터 제거)
            bossHealthBar?.UpdateHealth(currentHp);
            
            Debug.Log($"골렘이 {damageAmount} 데미지를 받았습니다. 현재 HP: {currentHp}/{maxHp} (페이즈: {(isPhase2 ? "2" : "1")})");

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
        agent.isStopped = true;

        if (upperBodyLayerIndex != -1)
        {
            animator.SetLayerWeight(upperBodyLayerIndex, 0f);
        }
        soundManager?.PlayDeathSound();
        Destroy(gameObject, 5f);
        Debug.Log("골렘 사망!");
        playerData.AddGold(dropGold);
    }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, closeRange);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, smallEarthquakeRadius);

            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(transform.position, largeEarthquakeRadius);
            
            // 2페이즈 표시
            if (isPhase2)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireCube(transform.position + Vector3.up * 3f, Vector3.one);
            }
        }
    }
