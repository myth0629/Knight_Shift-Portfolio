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
        public float ProjectileAttackCooldown = 4f;

        [Header("Phase Settings")]
        public bool IsPhase2 = false;
        public float Phase2HealthThreshold = 0.5f;
        public float Phase2SpeedMultiplier = 1.3f;
        public float Phase2AttackSpeedMultiplier = 0.8f;

        private NavMeshAgent agent;
        private Animator anim;
        private EnemyBlackboard bb;
        private BTNode root;
        private HoundAI houndAI;
        private EnemyHealth health;

        // Timers
        private float attackTimer = 0f;
        private float jumpAttackTimer = 0f;
        private float chargeAttackTimer = 0f;
        private float projectileAttackTimer = 0f;

        // Animation
        private float speedSmoothVel = 0f;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            anim = GetComponent<Animator>();
            houndAI = GetComponent<HoundAI>();
            health = GetComponent<EnemyHealth>();

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
            // Leaf node creation functions
            BTNode Combat() => new CombatNode(bb, this);
            BTNode Phase2Combat() => new Phase2CombatNode(bb, this);
            BTNode Chase() => new ChaseNode(bb, this);
            BTNode Retreat() => new RetreatNode(bb, this);
            BTNode Patrol() => new PatrolNode(bb, this);

            // Main behavior tree structure
            root = new Selector(bb,
                // Phase-specific combat
                new Sequence(bb, 
                    new ConditionNode(bb, () => bb.CanSeePlayer && bb.InAttackRange),
                    IsPhase2 ? Phase2Combat() : Combat()
                ),
                // Chase if see player
                new Sequence(bb, 
                    new ConditionNode(bb, () => bb.CanSeePlayer && !bb.InAttackRange), 
                    Chase()
                ),
                // Alert state (just saw player recently)
                new Sequence(bb, 
                    new ConditionNode(bb, () => bb.AlertTimer > 0f), 
                    Retreat()
                ),
                // Default patrol/idle
                Patrol()
            );
        }

        private void Update()
        {
            if (health != null && health.IsDead)
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
                default: return true;
            }
        }

        public void StartAttackCooldown(string attackType)
        {
            attackTimer = AttackCooldown;
            
            switch (attackType.ToLower())
            {
                case "jump": jumpAttackTimer = JumpAttackCooldown; break;
                case "charge": chargeAttackTimer = ChargeAttackCooldown; break;
                case "projectile": projectileAttackTimer = ProjectileAttackCooldown; break;
            }
        }

        #region Behavior Tree Nodes

        private class ConditionNode : BTNode
        {
            private System.Func<bool> predicate;
            public ConditionNode(EnemyBlackboard bb, System.Func<bool> pred) : base(bb) { predicate = pred; }
            public override NodeState Evaluate() => predicate() ? NodeState.Success : NodeState.Failure;
        }

        private class CombatNode : BTNode
        {
            private HoundBehaviorTree ctx;
            public CombatNode(EnemyBlackboard bb, HoundBehaviorTree c) : base(bb) { ctx = c; }

            public override NodeState Evaluate()
            {
                if (!bb.CanSeePlayer || !bb.InAttackRange) return NodeState.Failure;

                // 방향 계산 및 각도 체크
                Vector3 toPlayer = bb.Player.position - ctx.transform.position;
                toPlayer.y = 0f;
                float sqrMag = toPlayer.sqrMagnitude;
                if (sqrMag <= 0.01f) return NodeState.Running;

                Vector3 dirNorm = toPlayer.normalized;
                float angle = Vector3.Angle(ctx.transform.forward, dirNorm);
                bool facingEnough = angle <= 30f; // HoundAI의 IsLookingAtPlayer 기준과 동일하게 맞춤

                // 각도 미정렬이면 이동을 유지해 측면 멈춤 방지
                if (!facingEnough)
                {
                    ctx.agent.isStopped = false;
                    ctx.agent.speed = ctx.WalkSpeed;

                    // 측면 스트레이프 목적지 계산 (좌/우로 번갈아 움직임)
                    Vector3 right = Vector3.Cross(Vector3.up, dirNorm).normalized;
                    float side = (Time.frameCount % 120 < 60) ? 1f : -1f; // 1초 간격으로 좌/우 전환
                    Vector3 sideTarget = bb.Player.position - dirNorm * Mathf.Min(ctx.AttackRange * 0.8f, 2.0f) + right * side * 1.5f;
                    ctx.agent.SetDestination(sideTarget);

                    // 회전 보조
                    Quaternion look = Quaternion.LookRotation(dirNorm);
                    ctx.transform.rotation = Quaternion.Slerp(ctx.transform.rotation, look, Time.deltaTime * 10f);
                    return NodeState.Running;
                }

                // 각도 정렬되면 정지 후 공격 판단
                ctx.agent.isStopped = true;
                if (ctx.agent.hasPath) ctx.agent.ResetPath();
                ctx.agent.velocity = Vector3.zero;
                // 정지 상태에서도 계속 회전해 각도를 더 맞춰줌
                {
                    Quaternion lookPrecise = Quaternion.LookRotation(dirNorm);
                    ctx.transform.rotation = Quaternion.Slerp(ctx.transform.rotation, lookPrecise, Time.deltaTime * 10f);
                }

                // Perform attack if ready and not already attacking
                if (!bb.IsAttacking && ctx.attackTimer <= 0)
                {
                    ctx.PerformMeleeAttack();
                }

                return NodeState.Running;
            }
        }

        private class Phase2CombatNode : BTNode
        {
            private HoundBehaviorTree ctx;
            public Phase2CombatNode(EnemyBlackboard bb, HoundBehaviorTree c) : base(bb) { ctx = c; }

            public override NodeState Evaluate()
            {
                if (!bb.CanSeePlayer || !bb.InAttackRange) return NodeState.Failure;

                // 방향 계산 및 각도 체크
                Vector3 toPlayer = bb.Player.position - ctx.transform.position;
                toPlayer.y = 0f;
                float sqrMag = toPlayer.sqrMagnitude;
                if (sqrMag <= 0.01f) return NodeState.Running;

                Vector3 dirNorm = toPlayer.normalized;
                float angle = Vector3.Angle(ctx.transform.forward, dirNorm);
                bool facingEnough = angle <= 30f; // HoundAI의 IsLookingAtPlayer 기준과 동일하게 맞춤

                // 각도 미정렬이면 이동 유지
                if (!facingEnough)
                {
                    ctx.agent.isStopped = false;
                    ctx.agent.speed = ctx.WalkSpeed;

                    // 측면 스트레이프 목적지 계산 (좌/우로 번갈아 움직임)
                    Vector3 right = Vector3.Cross(Vector3.up, dirNorm).normalized;
                    float side = (Time.frameCount % 120 < 60) ? 1f : -1f;
                    Vector3 sideTarget = bb.Player.position - dirNorm * Mathf.Min(ctx.AttackRange * 0.8f, 2.0f) + right * side * 1.5f;
                    ctx.agent.SetDestination(sideTarget);

                    Quaternion look = Quaternion.LookRotation(dirNorm);
                    ctx.transform.rotation = Quaternion.Slerp(ctx.transform.rotation, look, Time.deltaTime * 10f);
                    return NodeState.Running;
                }

                // 각도 정렬되면 정지 후 공격 판단
                ctx.agent.isStopped = true;
                if (ctx.agent.hasPath) ctx.agent.ResetPath();
                ctx.agent.velocity = Vector3.zero;
                // 정지 상태에서도 계속 회전해 각도를 더 맞춰줌
                {
                    Quaternion lookPrecise = Quaternion.LookRotation(dirNorm);
                    ctx.transform.rotation = Quaternion.Slerp(ctx.transform.rotation, lookPrecise, Time.deltaTime * 10f);
                }

                // Phase 2 attack selection - only if not attacking
                if (!bb.IsAttacking && ctx.attackTimer <= 0)
                {
                    float attackChoice = Random.Range(0f, 1f);
                    if (attackChoice < 0.4f) // 40% melee
                    {
                        ctx.PerformMeleeAttack();
                    }
                    else if (attackChoice < 0.7f) // 30% projectile
                    {
                        ctx.PerformProjectileAttack();
                    }
                    else // 30% charge
                    {
                        ctx.PerformChargeAttack();
                    }
                }

                return NodeState.Running;
            }
        }

        private class ChaseNode : BTNode
        {
            private HoundBehaviorTree ctx;
            public ChaseNode(EnemyBlackboard bb, HoundBehaviorTree c) : base(bb) { ctx = c; }

            public override NodeState Evaluate()
            {
                if (!bb.CanSeePlayer) return NodeState.Failure;
                if (bb.IsAttacking) return NodeState.Running; // 공격 중이면 추격 로직 중단 (제자리 유지)

                ctx.agent.isStopped = false;
                float distance = Vector3.Distance(ctx.transform.position, bb.Player.position);
                
                // Set appropriate speed
                bool shouldRun = distance > ctx.RunThreshold;
                ctx.agent.speed = shouldRun ? ctx.RunSpeed : ctx.WalkSpeed;
                ctx.agent.SetDestination(bb.Player.position);

                // Consider jump attack at medium range
                if (distance > ctx.AttackRange && distance <= ctx.JumpAttackRange && ctx.jumpAttackTimer <= 0)
                {
                    if (Random.Range(0f, 1f) < 0.3f) // 30% chance
                    {
                        ctx.PerformJumpAttack();
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

        #endregion
    }
}