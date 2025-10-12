using UnityEngine;
using UnityEngine.AI;

namespace EnemyAI
{
    [RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
    public class EnemyBehaviorTree : MonoBehaviour
    {
        [Header("Ranges")] public float SightRange = 12f; public float SightAngle = 120f; public float HearRange = 6f; public float AttackRange = 2.2f; public float ChaseLeashRange = 25f; public float SearchRadius = 5f; public float SearchDuration = 5f; public float AlertDuration = 1.5f;
                [Header("Patrol")] public bool UsePatrol = false; public Vector3[] PatrolPoints; public float PatrolWait = 1f; public float PatrolArriveThreshold = 0.25f;
        [Header("Dynamic Patrol")] public bool GenerateDynamicPatrol = true; public int DynamicPatrolPointsCount = 3; public float DynamicPatrolRadius = 7f;
        [Header("Speeds")] public float PatrolSpeed = 2f; public float ChaseSpeed = 3f;

        private NavMeshAgent agent; private Animator anim; private EnemyBlackboard bb; private BTNode root;
        private int patrolIndex = 0; private float patrolWaitTimer = 0f; private EnemyHealth health;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            anim = GetComponent<Animator>();
            health = GetComponent<EnemyHealth>();
            bb = new EnemyBlackboard
            {
                Player = GameObject.FindWithTag("Player")?.transform,
                SpawnPosition = transform.position,
                LastKnownPlayerPosition = Vector3.zero,
                TimeSinceLostPlayer = 0f,
                CanSeePlayer = false,
                InAttackRange = false,
                AlertTimer = 0f
            };

            if (UsePatrol && (PatrolPoints == null || PatrolPoints.Length == 0) && GenerateDynamicPatrol)
            {
                GeneratePatrolPoints();
            }
        }

        private void GeneratePatrolPoints()
        {
            var points = new System.Collections.Generic.List<Vector3>();
            for (int i = 0; i < DynamicPatrolPointsCount; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * DynamicPatrolRadius;
                Vector3 randomPoint = bb.SpawnPosition + new Vector3(randomCircle.x, 0, randomCircle.y);

                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, DynamicPatrolRadius, NavMesh.AllAreas))
                {
                    points.Add(hit.position);
                }
            }
            if (points.Count > 0)
            {
                PatrolPoints = points.ToArray();
            }
            else
            {
                // If we failed to find any points, disable patrol for this instance
                UsePatrol = false;
            }
        }

        private void Start()
        {
            BuildTree();
        }

        private void BuildTree()
        {
            // Leaf Nodes implemented as local classes for brevity
            BTNode IdleOrPatrol() => new IdlePatrolNode(bb, this);
            BTNode Alert() => new AlertNode(bb, this);
            BTNode Chase() => new ChaseNode(bb, this);
            BTNode Combat() => new CombatNode(bb, this);
            BTNode Search() => new SearchNode(bb, this);
            BTNode Return() => new ReturnNode(bb, this);

            // NOTE: 패트롤 중(Return)과 충돌하여 왕복하는 현상 방지 위해 Return 조건에서 UsePatrol 제외
            root = new Selector(bb,
                // Combat chain
                new Sequence(bb, new ConditionNode(bb, () => bb.CanSeePlayer && bb.InAttackRange), Combat()),
                // Chase if see player
                new Sequence(bb, new ConditionNode(bb, () => bb.CanSeePlayer), Chase()),
                // Alert state (just saw player recently)
                new Sequence(bb, new ConditionNode(bb, () => bb.AlertTimer > 0f), Alert()),
                // Search lost player
                new Sequence(bb, new ConditionNode(bb, () => bb.TimeSinceLostPlayer > 0 && bb.TimeSinceLostPlayer < SearchDuration), Search()),
                // Return to spawn if far (패트롤 사용하는 경우 제외)
                new Sequence(bb, new ConditionNode(bb, () => !UsePatrol && !bb.CanSeePlayer && Vector3.Distance(transform.position, bb.SpawnPosition) > 0.5f), Return()),
                // Idle / Patrol fallback
                IdleOrPatrol()
            );
        }

        private void Update()
        {
            if (health != null && health.IsDead)
            {
                agent.isStopped = true; anim.SetBool("isWalking", false); return;
            }
            Sense();
            root?.Evaluate();
            Animate();
        }

        // Animator Event 혹은 StateMachineBehaviour 에서 호출할 수 있는 훅
        public void SetAttacking(bool attacking)
        {
            bb.IsAttacking = attacking;
            if (attacking)
            {
                // 공격 시작 시 이동 즉시 정지
                agent.isStopped = true;
                if (agent.hasPath) agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
        }

        private void Sense()
        {
            if (bb.Player == null) return;
            Vector3 toPlayer = bb.Player.position - transform.position; float distance = toPlayer.magnitude;
            bb.InAttackRange = distance <= AttackRange;

            // Vision
            if (distance <= SightRange)
            {
                float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
                bool ignoreAngle = bb.InAttackRange; // 전투(공격 사거리 내)일 때는 시야각 무시
                if ((ignoreAngle || angle <= SightAngle * 0.5f) && HasLineOfSight())
                {
                    bb.CanSeePlayer = true;
                    bb.LastKnownPlayerPosition = bb.Player.position;
                    bb.TimeSinceLostPlayer = 0f;
                    bb.AlertTimer = AlertDuration; // refresh alert
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

            if (bb.AlertTimer > 0f) bb.AlertTimer -= Time.deltaTime; else bb.AlertTimer = 0f;
        }

        private void LoseSightStep(float dt)
        {
            if (bb.CanSeePlayer)
            {
                bb.CanSeePlayer = false;
                bb.TimeSinceLostPlayer = 0.01f; // start search timer
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

        private float speedSmoothVel = 0f; // 내부 스무딩용
        private void Animate()
        {
            // 실속도(velocity)와 의도속도(desiredVelocity) 중 큰 값 사용해서 정지 직전 떨림/0 고정 문제 감소
            float rawVel = new Vector3(agent.velocity.x, 0f, agent.velocity.z).magnitude;
            float desired = new Vector3(agent.desiredVelocity.x, 0f, agent.desiredVelocity.z).magnitude;
            float moveSpeed = Mathf.Max(rawVel, desired);

            // 이동 제한 중(isStopped)인데 desiredVelocity 는 남아있을 수 있으므로 isStopped 면 강제로 0
            if (agent.isStopped) moveSpeed = 0f;

            // 스무딩 (애니메이터 파라미터 DampTime 과 별개로 부드러움 강화)
            float current = anim.GetFloat("Speed");
            float target = moveSpeed;
            float smoothed = Mathf.SmoothDamp(current, target, ref speedSmoothVel, 0.08f); // 80ms 정도 반응
            anim.SetFloat("Speed", smoothed);

            // 기타 상태 플래그 (필요시 유지)
            anim.SetBool("isAlert", bb.AlertTimer > 0f && !bb.CanSeePlayer);
            anim.SetBool("isChasing", bb.CanSeePlayer && !bb.InAttackRange);
            anim.SetBool("isSearching", bb.TimeSinceLostPlayer > 0f && bb.TimeSinceLostPlayer < SearchDuration);
        }

        #region Leaf Node Implementations
        private class ConditionNode : BTNode
        {
            private System.Func<bool> predicate; public ConditionNode(EnemyBlackboard bb, System.Func<bool> pred) : base(bb) { predicate = pred; }
            public override NodeState Evaluate() => predicate() ? NodeState.Success : NodeState.Failure;
        }

        private class IdlePatrolNode : BTNode
        {
            private EnemyBehaviorTree ctx; private bool initialized = false; private float nextSetDestCooldown = 0f; public IdlePatrolNode(EnemyBlackboard bb, EnemyBehaviorTree c) : base(bb) { ctx = c; }
            public override NodeState Evaluate()
            {
                if (ctx.UsePatrol && ctx.PatrolPoints != null && ctx.PatrolPoints.Length > 0)
                {
                    ctx.agent.isStopped = false;
                    if (ctx.agent.speed != ctx.PatrolSpeed) ctx.agent.speed = ctx.PatrolSpeed;

                    if (nextSetDestCooldown > 0f) nextSetDestCooldown -= Time.deltaTime;

                    // 초기 목적지 설정 혹은 경로 잃은 경우
                    if ((!initialized || !ctx.agent.hasPath || ctx.agent.destination == ctx.transform.position) && nextSetDestCooldown <= 0f)
                    {
                        initialized = true;
                        ctx.agent.SetDestination(ctx.PatrolPoints[ctx.patrolIndex]);
                        nextSetDestCooldown = 0.1f; // 과도한 재설정 방지
                    }

                    if (ctx.agent.pathPending)
                    {
                        return NodeState.Running; // 경로 계산 중
                    }

                    // 실제 목적지까지의 평면 거리 (remainingDistance가 순간 0 되는 플리커 방지)
                    Vector3 dest = ctx.PatrolPoints[ctx.patrolIndex];
                    float planarDist = Vector3.Distance(new Vector3(dest.x, 0f, dest.z), new Vector3(ctx.transform.position.x, 0f, ctx.transform.position.z));
                    float speed = new Vector3(ctx.agent.velocity.x, 0f, ctx.agent.velocity.z).magnitude;

                    bool arrived = planarDist <= Mathf.Max(ctx.PatrolArriveThreshold, ctx.agent.stoppingDistance + 0.01f) && speed < 0.1f;
                    if (arrived)
                    {
                        ctx.patrolWaitTimer += Time.deltaTime;
                        if (ctx.patrolWaitTimer >= ctx.PatrolWait)
                        {
                            ctx.patrolIndex = (ctx.patrolIndex + 1) % ctx.PatrolPoints.Length;
                            ctx.agent.SetDestination(ctx.PatrolPoints[ctx.patrolIndex]);
                            ctx.patrolWaitTimer = 0f;
                            nextSetDestCooldown = 0.1f;
                        }
                    }
                }
                return NodeState.Running;
            }
        }

        private class AlertNode : BTNode
        {
            private EnemyBehaviorTree ctx; public AlertNode(EnemyBlackboard bb, EnemyBehaviorTree c) : base(bb) { ctx = c; }
            public override NodeState Evaluate()
            {
                // Face last known player position
                Vector3 dir = (bb.LastKnownPlayerPosition - ctx.transform.position); dir.y = 0f;
                if (dir.sqrMagnitude > 0.2f)
                {
                    Quaternion look = Quaternion.LookRotation(dir.normalized);
                    ctx.transform.rotation = Quaternion.Slerp(ctx.transform.rotation, look, Time.deltaTime * 5f);
                }
                return bb.AlertTimer > 0f ? NodeState.Running : NodeState.Success;
            }
        }

        private class ChaseNode : BTNode
        {
            private EnemyBehaviorTree ctx; public ChaseNode(EnemyBlackboard bb, EnemyBehaviorTree c) : base(bb) { ctx = c; }
            public override NodeState Evaluate()
            {
                if (!bb.CanSeePlayer) return NodeState.Failure;
                if (bb.IsAttacking) return NodeState.Running; // 공격 중이면 추격 로직 중단 (제자리 유지)
                float distFromSpawn = Vector3.Distance(ctx.transform.position, bb.SpawnPosition);
                if (distFromSpawn > ctx.ChaseLeashRange)
                {
                    // break chase if too far
                    bb.CanSeePlayer = false; bb.TimeSinceLostPlayer = 0.01f; return NodeState.Failure;
                }
                ctx.agent.isStopped = false;
                if (ctx.agent.speed != ctx.ChaseSpeed) ctx.agent.speed = ctx.ChaseSpeed;
                ctx.agent.SetDestination(bb.Player.position);
                bb.LastKnownPlayerPosition = bb.Player.position;
                return NodeState.Running;
            }
        }

        private class CombatNode : BTNode
        {
            private EnemyBehaviorTree ctx; private float attackCooldown = 0f; private float attackInterval = 2f;
            public CombatNode(EnemyBlackboard bb, EnemyBehaviorTree c) : base(bb) { ctx = c; }
            public override NodeState Evaluate()
            {
                if (!bb.CanSeePlayer || !bb.InAttackRange) return NodeState.Failure;
                ctx.agent.isStopped = true;
                if (ctx.agent.hasPath) ctx.agent.ResetPath();
                // rotate toward player
                Vector3 dir = (bb.Player.position - ctx.transform.position); dir.y = 0f;
                if (dir.sqrMagnitude > 0.1f)
                {
                    Quaternion look = Quaternion.LookRotation(dir.normalized);
                    ctx.transform.rotation = Quaternion.Slerp(ctx.transform.rotation, look, Time.deltaTime * 10f);
                }
                if (!bb.IsAttacking)
                {
                    if (attackCooldown <= 0f)
                    {
                        ctx.anim.SetTrigger("Attack");
                        attackCooldown = attackInterval;
                    }
                    else
                    {
                        attackCooldown -= Time.deltaTime;
                    }
                }
                return NodeState.Running;
            }
        }

        private class SearchNode : BTNode
        {
            private EnemyBehaviorTree ctx; private float localTimer = 0f; private Vector3 wanderTarget;
            public SearchNode(EnemyBlackboard bb, EnemyBehaviorTree c) : base(bb) { ctx = c; }
            public override NodeState Evaluate()
            {
                if (bb.TimeSinceLostPlayer <= 0f || bb.TimeSinceLostPlayer >= ctx.SearchDuration) return NodeState.Failure;
                ctx.agent.isStopped = false;
                if (ctx.agent.speed != ctx.PatrolSpeed) ctx.agent.speed = ctx.PatrolSpeed; // 수색은 패트롤 속도 사용
                localTimer += Time.deltaTime;
                if (localTimer > 1.5f || ctx.agent.remainingDistance <= ctx.agent.stoppingDistance)
                {
                    // pick random point near last known
                    Vector2 rnd = Random.insideUnitCircle * ctx.SearchRadius;
                    wanderTarget = bb.LastKnownPlayerPosition + new Vector3(rnd.x, 0f, rnd.y);
                    ctx.agent.SetDestination(wanderTarget);
                    localTimer = 0f;
                }
                return NodeState.Running;
            }
        }

        private class ReturnNode : BTNode
        {
            private EnemyBehaviorTree ctx; public ReturnNode(EnemyBlackboard bb, EnemyBehaviorTree c) : base(bb) { ctx = c; }
            public override NodeState Evaluate()
            {
                ctx.agent.isStopped = false;
                if (ctx.agent.speed != ctx.PatrolSpeed) ctx.agent.speed = ctx.PatrolSpeed; // 귀환은 패트롤 속도
                ctx.agent.SetDestination(bb.SpawnPosition);
                if (Vector3.Distance(ctx.transform.position, bb.SpawnPosition) <= 0.5f)
                {
                    // reset memory
                    bb.TimeSinceLostPlayer = 0f; bb.LastKnownPlayerPosition = Vector3.zero; bb.AlertTimer = 0f;
                    return NodeState.Success;
                }
                return NodeState.Running;
            }
        }
        #endregion
    }
}
