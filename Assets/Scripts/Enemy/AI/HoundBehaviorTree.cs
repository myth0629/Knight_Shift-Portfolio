using UnityEngine;
using UnityEngine.AI;

namespace EnemyAI
{
    [RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
    public class HoundBehaviorTree : MonoBehaviour
    {
        [Header("Sensing")]
        public float SightRange = 12f;
        public float SightAngle = 120f;
        public float HearRange = 6f;
        public float AttackRange = 3.2f;
        public float CloseRange = 1.8f;
        public float JumpAttackRange = 4.5f;
        public float AlertDuration = 1.5f;

        [Header("Movement Speeds")]
        public float WalkSpeed = 2f;
        public float RunSpeed = 5f;
        public float RunThreshold = 6f;

        [Header("Attack Cooldowns")]
        public float AttackCooldown = 1.5f;
        public float JumpAttackCooldown = 4f;
        public float ChargeAttackCooldown = 5f;
        public float ProjectileAttackCooldown = 6f;
        public float SpiritArrowCooldown = 12f;
        public float RetreatAttackCooldown = 8f;

        [Header("Phase Settings")]
        public float Phase2HealthThreshold = 0.5f;
        public float Phase2SpeedMultiplier = 1.3f;
        public float Phase2AttackSpeedMultiplier = 0.8f;

        private NavMeshAgent agent;
        private Animator anim;
        private EnemyBlackboard bb;
        private BTNode root;
        private HoundAI houndAI;

        // Timers
        private float attackTimer = 0f;
        private float jumpAttackTimer = 0f;
        private float chargeAttackTimer = 0f;
        private float projectileAttackTimer = 0f;
        private float spiritArrowTimer = 0f;
        private float retreatAttackTimer = 0f;

        // Animation
        private float speedSmoothVel = 0f;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            anim = GetComponent<Animator>();
            houndAI = GetComponent<HoundAI>();

            bb = new EnemyBlackboard
            {
                Player = GameObject.FindWithTag("Player")?.transform,
                SpawnPosition = transform.position,
                LastKnownPlayerPosition = Vector3.zero,
                TimeSinceLostPlayer = 0f,
                CanSeePlayer = false,
                InAttackRange = false,
                AlertTimer = 0f,
                IsAttacking = false
            };
        }

        private void Start()
        {
            BuildTree();
        }

        private void BuildTree()
        {
            // Main behavior tree structure
            root = new Selector(bb,
                // 플레이어를 보면 교전 시작
                new Sequence(bb,
                    new ConditionNode(bb, () => bb.CanSeePlayer),
                    new EngageNode(bb, this) // 모든 전투/추격 로직을 담당
                ),
                // Alert state (just saw player recently)
                new Sequence(bb,
                    new ConditionNode(bb, () => bb.AlertTimer > 0f),
                    new RetreatNode(bb, this)
                ),
                // Default patrol/idle
                new PatrolNode(bb, this)
            );
        }

        private void Update()
        {
            // 하운드가 죽었거나, 페이즈 전환 패턴 중에는 모든 행동을 중지합니다.
            if (houndAI != null && (houndAI.IsDead || houndAI.isPhaseTransitionTriggered))
            {
                agent.isStopped = true;
                anim.SetFloat("Speed", 0f);
                return;
            }

            UpdateTimers();
            Sense();
            root?.Evaluate();
            Animate();
        }

        private void UpdateTimers()
        {
            if (attackTimer > 0) attackTimer -= Time.deltaTime;
            if (jumpAttackTimer > 0) jumpAttackTimer -= Time.deltaTime;
            if (chargeAttackTimer > 0) chargeAttackTimer -= Time.deltaTime;
            if (projectileAttackTimer > 0) projectileAttackTimer -= Time.deltaTime;
            if (spiritArrowTimer > 0) spiritArrowTimer -= Time.deltaTime;
            if (retreatAttackTimer > 0) retreatAttackTimer -= Time.deltaTime;
            if (bb.AlertTimer > 0) bb.AlertTimer -= Time.deltaTime;
        }

        private void Sense()
        {
            if (bb.Player == null) return;

            Vector3 toPlayer = bb.Player.position - transform.position;
            float distance = toPlayer.magnitude;
            
            bb.InAttackRange = distance <= AttackRange;
            bool inCloseRange = distance <= CloseRange;
            bool inJumpRange = distance <= JumpAttackRange;

            // Vision check
            if (distance <= SightRange)
            {
                float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
                bool ignoreAngle = bb.InAttackRange; // Ignore angle in close combat
                
                if ((ignoreAngle || angle <= SightAngle * 0.5f) && HasLineOfSight())
                {
                    bb.CanSeePlayer = true;
                    bb.LastKnownPlayerPosition = bb.Player.position;
                    bb.TimeSinceLostPlayer = 0f;
                    bb.AlertTimer = AlertDuration;
                }
                else
                {
                    LoseSightStep(Time.deltaTime);
                }
            }
            else if (distance <= HearRange)
            {
                // Heard player
                bb.CanSeePlayer = false;
                bb.LastKnownPlayerPosition = bb.Player.position;
                bb.TimeSinceLostPlayer += Time.deltaTime;
                if (bb.AlertTimer <= 0f) bb.AlertTimer = AlertDuration * 0.5f;
            }
            else
            {
                LoseSightStep(Time.deltaTime);
            }
        }

        private void LoseSightStep(float dt)
        {
            if (bb.CanSeePlayer)
            {
                bb.CanSeePlayer = false;
                bb.TimeSinceLostPlayer = 0.01f;
            }
            else if (bb.TimeSinceLostPlayer > 0f)
            {
                bb.TimeSinceLostPlayer += dt;
            }
        }

        private bool HasLineOfSight()
        {
            Vector3 origin = transform.position + Vector3.up * 1.7f;
            Vector3 dest = bb.Player.position + Vector3.up * 1.3f;
            
            if (Physics.Raycast(origin, (dest - origin).normalized, out RaycastHit hit, SightRange))
            {
                return hit.transform == bb.Player;
            }
            return false;
        }

        private void Animate()
        {
            // Calculate movement speed for animation
            float rawVel = new Vector3(agent.velocity.x, 0f, agent.velocity.z).magnitude;
            float desired = new Vector3(agent.desiredVelocity.x, 0f, agent.desiredVelocity.z).magnitude;
            float moveSpeed = Mathf.Max(rawVel, desired);

            if (agent.isStopped) moveSpeed = 0f;

            // Smooth animation parameter
            float current = anim.GetFloat("Speed");
            float smoothed = Mathf.SmoothDamp(current, moveSpeed, ref speedSmoothVel, 0.08f);
            anim.SetFloat("Speed", smoothed);

            // Additional animation parameters can be set here
            anim.SetBool("isAlert", bb.AlertTimer > 0f && !bb.CanSeePlayer);
            anim.SetBool("isChasing", bb.CanSeePlayer && !bb.InAttackRange);
        }

        // Public methods for HoundAI integration
        public void SetAttacking(bool attacking)
        {
            bb.IsAttacking = attacking;
            if (attacking)
            {
                agent.isStopped = true;
                if (agent.hasPath) agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
        }

        public bool CanPerformAttack(string attackType)
        {
            if (bb.IsAttacking || attackTimer > 0) return false;

            switch (attackType.ToLower())
            {
                case "jump": return jumpAttackTimer <= 0;
                case "charge": return chargeAttackTimer <= 0;
                case "projectile": return projectileAttackTimer <= 0;
                case "spiritarrow": return spiritArrowTimer <= 0;
                case "retreat": return retreatAttackTimer <= 0;
                default: return true;
            }
        }

        public void StartAttackCooldown(string attackType)
        {
            attackTimer = AttackCooldown * (houndAI.isPhase2 ? Phase2AttackSpeedMultiplier : 1f);
            
            switch (attackType.ToLower())
            {
                case "jump": jumpAttackTimer = JumpAttackCooldown; break;
                case "charge": chargeAttackTimer = ChargeAttackCooldown; break;
                case "projectile": projectileAttackTimer = ProjectileAttackCooldown; break;
                case "spiritarrow": spiritArrowTimer = SpiritArrowCooldown; break;
                case "retreat": retreatAttackTimer = RetreatAttackCooldown; break;
            }
        }

        #region Behavior Tree Nodes

        private class ConditionNode : BTNode
        {
            private System.Func<bool> predicate;
            public ConditionNode(EnemyBlackboard bb, System.Func<bool> pred) : base(bb) { predicate = pred; }
            public override NodeState Evaluate() => predicate() ? NodeState.Success : NodeState.Failure;
        }

        private class EngageNode : BTNode
        {
            private HoundBehaviorTree ctx;
            public EngageNode(EnemyBlackboard bb, HoundBehaviorTree c) : base(bb) { ctx = c; }

            public override NodeState Evaluate()
            {
                float distance = Vector3.Distance(ctx.transform.position, bb.Player.position);

                // 공격 중이라도 플레이어가 너무 멀어지면 공격을 취소하고 추격 상태로 전환합니다.
                if (bb.IsAttacking)
                {
                    if (distance > ctx.AttackRange * 1.2f) // 공격 범위의 120% 이상 멀어지면
                    {
                        ctx.houndAI.CancelAttack(); // 공격 강제 중단
                    }
                    return NodeState.Running; // 공격 중(또는 방금 취소됨)이므로 다른 행동은 하지 않음
                }

                if (ctx.houndAI.isPhase2)
                {
                    // === PHASE 2 LOGIC ===
                    // 1. 원거리 공격이 가능한지 먼저 확인합니다.
                    if (distance > ctx.AttackRange * 1.5f && ctx.attackTimer <= 0)
                    {
                        // Ranged Combat - 멈춰서 원거리 공격
                        ctx.agent.isStopped = true;
                        if (ctx.agent.hasPath) ctx.agent.ResetPath();

                        Vector3 toPlayer = bb.Player.position - ctx.transform.position;
                        float angle = Vector3.Angle(ctx.transform.forward, toPlayer.normalized);
                        if (angle <= 30f)
                        {
                            float roll = Random.Range(0f, 1f);
                            if (roll < 0.4f && ctx.CanPerformAttack("jump")) { ctx.PerformJumpAttack(); }
                            else if (roll < 0.7f && ctx.CanPerformAttack("projectile")) { ctx.PerformProjectileAttack(); }
                            else if (ctx.CanPerformAttack("spiritarrow")) { ctx.PerformSpiritArrowRain(); }
                        }
                        else // 아직 플레이어를 보지 못했다면 회전만 합니다.
                        {
                            toPlayer.y = 0f;
                            Quaternion look = Quaternion.LookRotation(toPlayer.normalized);
                            ctx.transform.rotation = Quaternion.Slerp(ctx.transform.rotation, look, Time.deltaTime * 10f);
                        }
                    }
                    // 2. 원거리 공격 조건이 아니면서, 근접 공격 범위 밖에 있다면 추격합니다.
                    else if (distance > ctx.AttackRange)
                    {
                        // Chase Logic
                        ctx.agent.isStopped = false;
                        ctx.agent.speed = ctx.RunSpeed * ctx.Phase2SpeedMultiplier;
                        ctx.agent.SetDestination(bb.Player.position);
                    }
                    else
                    {
                        // 3. 근접 공격 범위 안일 때만 근접 공격을 시도합니다.
                        ctx.agent.isStopped = true;
                        if (ctx.agent.hasPath) ctx.agent.ResetPath();

                        Vector3 toPlayer = bb.Player.position - ctx.transform.position;
                        toPlayer.y = 0f;
                        Quaternion look = Quaternion.LookRotation(toPlayer.normalized);
                        ctx.transform.rotation = Quaternion.Slerp(ctx.transform.rotation, look, Time.deltaTime * 10f);

                        if (ctx.attackTimer <= 0)
                        {
                            // 공격을 실행하기 직전에, 플레이어가 여전히 공격 범위 내에 있는지 다시 한번 확인합니다.
                            // 이렇게 하면 공격 애니메이션이 시작되기 전에 플레이어가 범위를 벗어나는 경우,
                            // 헛된 공격을 하지 않고 즉시 추격을 시작할 수 있습니다.
                            if (Vector3.Distance(ctx.transform.position, bb.Player.position) <= ctx.AttackRange)
                            {
                                float angle = Vector3.Angle(ctx.transform.forward, toPlayer.normalized);
                                if (angle <= 30f)
                                {
                                    // 20% 확률로 후퇴 공격, 15% 확률로 돌진, 나머지 65%는 일반 근접 공격
                                    float roll = Random.Range(0f, 1f);
                                    if (roll < 0.2f && ctx.CanPerformAttack("retreat"))
                                    {
                                        ctx.PerformRetreatAttack();
                                    }
                                    else if (roll < 0.35f && ctx.CanPerformAttack("charge"))
                                    {
                                        ctx.PerformChargeAttack();
                                    }
                                    else { ctx.PerformMeleeAttack(); }
                                }
                            }
                        }
                    }
                }
                else
                {
                    // === PHASE 1 LOGIC ===
                    if (distance > ctx.AttackRange)
                    {
                        // Chase
                        ctx.agent.isStopped = false;
                        ctx.agent.speed = distance > ctx.RunThreshold ? ctx.RunSpeed : ctx.WalkSpeed;
                        ctx.agent.SetDestination(bb.Player.position);

                        if (distance <= ctx.JumpAttackRange && ctx.CanPerformAttack("jump"))
                        {
                            // HoundAI의 1페이즈 원거리 로직은 이동공격/점프공격이지만, BT에서는 점프만 구현
                            if (Random.Range(0f, 1f) < 0.3f) { ctx.PerformJumpAttack(); }
                        }
                    }
                    else
                    {
                        // Close Combat
                        ctx.agent.isStopped = true;
                        if (ctx.agent.hasPath) ctx.agent.ResetPath();

                        Vector3 toPlayer = bb.Player.position - ctx.transform.position;
                        toPlayer.y = 0f;
                        Quaternion look = Quaternion.LookRotation(toPlayer.normalized);
                        ctx.transform.rotation = Quaternion.Slerp(ctx.transform.rotation, look, Time.deltaTime * 10f);

                        if (ctx.attackTimer <= 0)
                        {
                            float angle = Vector3.Angle(ctx.transform.forward, toPlayer.normalized);
                            if (angle <= 30f)
                            {
                                ctx.PerformMeleeAttack();
                            }
                        }
                    }
                }
                return NodeState.Running;
            }
        }

        private class RetreatNode : BTNode
        {
            private HoundBehaviorTree ctx;
            public RetreatNode(EnemyBlackboard bb, HoundBehaviorTree c) : base(bb) { ctx = c; }

            public override NodeState Evaluate()
            {
                if (bb.AlertTimer <= 0f) return NodeState.Success;

                // Face last known player position
                Vector3 dir = (bb.LastKnownPlayerPosition - ctx.transform.position);
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.2f)
                {
                    Quaternion look = Quaternion.LookRotation(dir.normalized);
                    ctx.transform.rotation = Quaternion.Slerp(ctx.transform.rotation, look, Time.deltaTime * 5f);
                }

                return NodeState.Running;
            }
        }

        private class PatrolNode : BTNode
        {
            private HoundBehaviorTree ctx;
            public PatrolNode(EnemyBlackboard bb, HoundBehaviorTree c) : base(bb) { ctx = c; }

            public override NodeState Evaluate()
            {
                // Return to spawn position if far away
                float distanceFromSpawn = Vector3.Distance(ctx.transform.position, bb.SpawnPosition);
                if (distanceFromSpawn > 1f)
                {
                    ctx.agent.isStopped = false;
                    ctx.agent.speed = ctx.WalkSpeed;
                    ctx.agent.SetDestination(bb.SpawnPosition);
                }
                else
                {
                    ctx.agent.isStopped = true;
                }

                return NodeState.Running;
            }
        }

        #endregion

        #region Attack Methods (delegated to HoundAI)

        private void PerformMeleeAttack()
        {
            if (houndAI != null)
            {
                houndAI.PerformMeleeAttack();
                StartAttackCooldown("melee");
            }
        }

        private void PerformJumpAttack()
        {
            if (houndAI != null && CanPerformAttack("jump"))
            {
                houndAI.PerformJumpAttack();
                StartAttackCooldown("jump");
            }
        }

        private void PerformChargeAttack()
        {
            if (houndAI != null && CanPerformAttack("charge"))
            {
                houndAI.PerformChargeAttack();
                StartAttackCooldown("charge");
            }
        }

        private void PerformProjectileAttack()
        {
            if (houndAI != null && CanPerformAttack("projectile"))
            {
                houndAI.PerformProjectileAttack();
                StartAttackCooldown("projectile");
            }
        }

        private void PerformSpiritArrowRain()
        {
            if (houndAI != null && CanPerformAttack("spiritarrow"))
            {
                houndAI.PerformSpiritArrowRain();
                StartAttackCooldown("spiritarrow");
            }
        }

        private void PerformRetreatAttack()
        {
            if (houndAI != null && CanPerformAttack("retreat"))
            {
                houndAI.PerformRetreatAndRangedAttack();
                StartAttackCooldown("retreat");
            }
        }

        #endregion
    }
}