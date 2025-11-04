using UnityEngine;
using UnityEngine.AI;

namespace EnemyAI
{
    [RequireComponent(typeof(NavMeshAgent), typeof(Animator), typeof(GolemAI))]
    public class GolemBehaviorTree : MonoBehaviour
    {
        [Header("Sensing")]
        public float SightRange = 20f;
        public float SightAngle = 120f;
        public float AttackRange = 4f;
        public float AlertDuration = 2f;

        [Header("Movement Speeds")]
        public float WalkSpeed = 1.5f;
        public float Phase2WalkSpeed = 2.2f;

        [Header("Attack Cooldowns")]
        public float AttackCooldown = 2f;
        public float Phase2AttackCooldown = 1.5f;

        private NavMeshAgent agent;
        private Animator anim;
        private EnemyBlackboard bb;
        private BTNode root;
        private GolemAI golemAI;

        // Timers
        private float attackTimer = 0f;

        // Animation Smoothing
        private float moveX_vel;
        private float moveY_vel;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            anim = GetComponent<Animator>();
            golemAI = GetComponent<GolemAI>();

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
            root = new Selector(bb,
                new Sequence(bb,
                    new ConditionNode(bb, () => bb.CanSeePlayer && bb.InAttackRange),
                    new Selector(bb,
                        new Sequence(bb, new ConditionNode(bb, () => golemAI.IsPhase2()), new Phase2CombatNode(bb, this)),
                        new Phase1CombatNode(bb, this)
                    )
                ),
                new Sequence(bb,
                    new ConditionNode(bb, () => bb.CanSeePlayer && !bb.InAttackRange),
                    new ChaseNode(bb, this)
                ),
                new Sequence(bb,
                    new ConditionNode(bb, () => bb.AlertTimer > 0f),
                    new AlertNode(bb, this)
                ),
                new IdleNode(bb, this)
            );
        }

        private void Update()
        {
            if (golemAI.IsDead)
            {
                if (agent.enabled) agent.isStopped = true;
                anim.SetBool("IsMoving", false);
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
            if (bb.AlertTimer > 0) bb.AlertTimer -= Time.deltaTime;
        }

        private void Sense()
        {
            if (bb.Player == null) return;

            Vector3 toPlayer = bb.Player.position - transform.position;
            float distance = toPlayer.magnitude;

            bb.InAttackRange = distance <= AttackRange;

            bool canCurrentlySee = false;
            if (distance <= SightRange)
            {
                float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
                if ((bb.InAttackRange || angle <= SightAngle * 0.5f) && HasLineOfSight())
                {
                    canCurrentlySee = true;
                }
            }

            // 공격 중에는 시야 상태를 잃지 않도록 처리합니다.
            // 이렇게 하면 플레이어가 공격 애니메이션 중에 뒤로 돌아가도 골렘이 '멍청하게' 서 있는 현상을 방지합니다.
            if (bb.IsAttacking)
            {
                if (canCurrentlySee)
                {
                    bb.LastKnownPlayerPosition = bb.Player.position;
                    bb.AlertTimer = AlertDuration; // 경계 타이머를 계속 갱신
                }
                return; // 시야 상태(CanSeePlayer)를 바꾸지 않고 종료
            }

            if (canCurrentlySee)
            {
                bb.CanSeePlayer = true;
                bb.LastKnownPlayerPosition = bb.Player.position;
                bb.TimeSinceLostPlayer = 0f;
                bb.AlertTimer = AlertDuration;
            }
            else
            {
                if (bb.CanSeePlayer) bb.TimeSinceLostPlayer = 0.01f; // 시야를 잃은 첫 프레임
                bb.CanSeePlayer = false;
            }

            // 시야를 잃었을 때, 타이머 증가
            if (!bb.CanSeePlayer && bb.TimeSinceLostPlayer > 0f)
            {
                bb.TimeSinceLostPlayer += Time.deltaTime;
            }
        }

        private bool HasLineOfSight()
        {
            Vector3 origin = transform.position + Vector3.up * 1.5f;
            Vector3 dest = bb.Player.position + Vector3.up * 1.0f;
            if (Physics.Raycast(origin, (dest - origin).normalized, out RaycastHit hit, SightRange))
            {
                return hit.transform == bb.Player;
            }
            return false;
        }

        private void Animate()
        {
            if (agent.isStopped)
            {
                anim.SetBool("IsMoving", false);
                anim.SetFloat("MoveX", 0f);
                anim.SetFloat("MoveY", 0f);
                return;
            }

            Vector3 localVel = transform.InverseTransformDirection(agent.velocity.normalized);
            float smooth = Time.deltaTime * 8f;
            float smoothedX = Mathf.SmoothDamp(anim.GetFloat("MoveX"), localVel.x, ref moveX_vel, smooth);
            float smoothedY = Mathf.SmoothDamp(anim.GetFloat("MoveY"), localVel.z, ref moveY_vel, smooth);

            anim.SetFloat("MoveX", smoothedX);
            anim.SetFloat("MoveY", smoothedY);
            anim.SetBool("IsMoving", agent.velocity.magnitude > 0.1f);
        }

        public void SetAttacking(bool attacking)
        {
            bb.IsAttacking = attacking;
            if (attacking)
            {
                agent.isStopped = true;
                if (agent.hasPath) agent.ResetPath();
            }
        }

        #region Behavior Tree Nodes

        private class ConditionNode : BTNode
        {
            private System.Func<bool> predicate;
            public ConditionNode(EnemyBlackboard bb, System.Func<bool> pred) : base(bb) { predicate = pred; }
            public override NodeState Evaluate() => predicate() ? NodeState.Success : NodeState.Failure;
        }

        private abstract class GolemCombatNode : BTNode
        {
            protected GolemBehaviorTree ctx;
            public GolemCombatNode(EnemyBlackboard bb, GolemBehaviorTree c) : base(bb) { ctx = c; }

            protected void RotateTowardsPlayer()
            {
                Vector3 dirToPlayer = (bb.Player.position - ctx.transform.position).normalized;
                dirToPlayer.y = 0;
                Quaternion targetRotation = Quaternion.LookRotation(dirToPlayer);
                ctx.transform.rotation = Quaternion.Slerp(ctx.transform.rotation, targetRotation, Time.deltaTime * ctx.golemAI.GetRotationSpeed() * 2f); // 회전 속도 증가
            }

            protected NodeState BaseCombatEvaluate()
            {
                if (bb.IsAttacking) return NodeState.Running;
                if (ctx.attackTimer > 0) return NodeState.Running;

                // 공격 전, 플레이어를 향해 완전히 회전했는지 확인합니다.
                RotateTowardsPlayer();

                Vector3 dirToPlayer = (bb.Player.position - ctx.transform.position).normalized;
                dirToPlayer.y = 0;
                float angle = Vector3.Angle(ctx.transform.forward, dirToPlayer);

                // 아직 플레이어를 바라보고 있지 않다면(각도가 15도 이상이면), 계속 회전만 하고 공격은 하지 않습니다.
                if (angle > 15f)
                    return NodeState.Running;

                return NodeState.Success;
            }
        }

        private class Phase1CombatNode : GolemCombatNode
        {
            public Phase1CombatNode(EnemyBlackboard bb, GolemBehaviorTree c) : base(bb, c) { }
            public override NodeState Evaluate()
            {
                var result = BaseCombatEvaluate();
                if (result != NodeState.Success) return result;

                // 공격 실행 직전, 다시 한번 사거리 체크
                if (!bb.InAttackRange) return NodeState.Failure;

                float roll = Random.Range(0f, 1f);
                if (roll < 0.4f) ctx.golemAI.PerformWalkAttack();
                else ctx.golemAI.PerformStationaryAttack();

                ctx.attackTimer = ctx.AttackCooldown;
                return NodeState.Running;
            }
        }

        private class Phase2CombatNode : GolemCombatNode
        {
            public Phase2CombatNode(EnemyBlackboard bb, GolemBehaviorTree c) : base(bb, c) { }
            public override NodeState Evaluate()
            {
                var result = BaseCombatEvaluate();
                if (result != NodeState.Success) return result;

                // 공격 실행 직전, 다시 한번 사거리 체크
                if (!bb.InAttackRange) return NodeState.Failure;

                float roll = Random.Range(0f, 1f);
                if (roll < 0.4f) ctx.golemAI.PerformComboAttack();
                else if (roll < 0.7f) ctx.golemAI.PerformWalkAttack();
                else ctx.golemAI.PerformStationaryAttack();

                ctx.attackTimer = ctx.Phase2AttackCooldown;
                return NodeState.Running;
            }
        }

        private class ChaseNode : BTNode
        {
            private GolemBehaviorTree ctx;
            public ChaseNode(EnemyBlackboard bb, GolemBehaviorTree c) : base(bb) { ctx = c; }
            public override NodeState Evaluate()
            {
                if (bb.IsAttacking) return NodeState.Running;
                ctx.agent.isStopped = false;
                ctx.agent.speed = ctx.golemAI.IsPhase2() ? ctx.Phase2WalkSpeed : ctx.WalkSpeed;
                ctx.agent.SetDestination(bb.Player.position);

                // 추격 중에도 부드럽게 회전하도록 추가
                Vector3 dirToPlayer = (bb.Player.position - ctx.transform.position).normalized;
                dirToPlayer.y = 0;
                Quaternion targetRotation = Quaternion.LookRotation(dirToPlayer);
                ctx.transform.rotation = Quaternion.Slerp(ctx.transform.rotation, targetRotation, Time.deltaTime * ctx.golemAI.GetRotationSpeed());

                return NodeState.Running;
            }
        }

        private class AlertNode : BTNode
        {
            private GolemBehaviorTree ctx;
            public AlertNode(EnemyBlackboard bb, GolemBehaviorTree c) : base(bb) { ctx = c; }
            public override NodeState Evaluate()
            {
                if (bb.IsAttacking) return NodeState.Running;
                ctx.agent.isStopped = true;

                Vector3 dir = (bb.LastKnownPlayerPosition - ctx.transform.position);
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.1f)
                {
                    Quaternion look = Quaternion.LookRotation(dir.normalized);
                    ctx.transform.rotation = Quaternion.Slerp(ctx.transform.rotation, look, Time.deltaTime * ctx.golemAI.GetRotationSpeed());
                }
                return NodeState.Running;
            }
        }

        private class IdleNode : BTNode
        {
            private GolemBehaviorTree ctx;
            public IdleNode(EnemyBlackboard bb, GolemBehaviorTree c) : base(bb) { ctx = c; }
            public override NodeState Evaluate()
            {
                if (bb.IsAttacking) return NodeState.Running;
                ctx.agent.isStopped = true;
                return NodeState.Running;
            }
        }

        #endregion
    }
}