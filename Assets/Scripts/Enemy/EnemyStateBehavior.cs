using UnityEngine;
using UnityEngine.AI;

// 공격/특정 애니메이션(예: Roll, HeavyAttack 등) 재생 구간에서
// 무기 콜라이더 활성/비활성 및 NavMeshAgent 정지/재개 등을 제어하는 범용 StateMachineBehaviour
// Animator Attack(또는 원하는) State에 이 클래스를 붙여 Inspector 옵션 조정
public class EnemyStateBehavior : StateMachineBehaviour
{
    [Header("동작 옵션")]
    [Tooltip("State 진입 시 NavMeshAgent 이동을 멈출지")] public bool stopAgentOnEnter = true;
    [Tooltip("State 종료 시 NavMeshAgent 이동을 재개할지")] public bool resumeAgentOnExit = true;
    [Tooltip("State 진입 시 무기 콜라이더 활성화")] public bool enableWeaponOnEnter = true;
    [Tooltip("State 종료 시 무기 콜라이더 비활성화")] public bool disableWeaponOnExit = true;
    [Tooltip("애니메이션 동안 Yaw 회전을 고정 (루트모션/공격 방향 유지)")] public bool lockRotation = true;

    private NavMeshAgent _agent;
    private EnemyController _controller; // 체력/무기 관리용 슬림 컴포넌트
    private Weapon _weapon;
    private bool _prevAgentStopped;
    private Quaternion _lockedRotation;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _controller = animator.GetComponent<EnemyController>();
        _agent = animator.GetComponent<NavMeshAgent>();
        if (_weapon == null && _controller != null)
        {
            // 무기 캐시 (EnemyController 내부에서 사용 중인 것과 동일하게 자식에서 가져옴)
            _weapon = animator.GetComponentInChildren<Weapon>();
        }

        if (stopAgentOnEnter && _agent != null)
        {
            _prevAgentStopped = _agent.isStopped;
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }

        if (enableWeaponOnEnter && _weapon != null)
        {
            _weapon.EnableDamageCollider();
        }

        // 공격 시작 알림 (공격 전용 state일 때)
        var bt = animator.GetComponent<EnemyAI.EnemyBehaviorTree>();
        if (bt != null)
        {
            bt.SetAttacking(true);
        }

        if (lockRotation)
        {
            _lockedRotation = animator.transform.rotation;
        }
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (lockRotation)
        {
            // 공격 도중 다른 시스템이 회전을 바꾸지 못하게 고정 (필요 시 Pitch/ Roll 제외 가능)
            var e = _lockedRotation.eulerAngles;
            animator.transform.rotation = Quaternion.Euler(0f, e.y, 0f);
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (disableWeaponOnExit && _weapon != null)
        {
            _weapon.DisableDamageCollider();
        }

        if (resumeAgentOnExit && _agent != null)
        {
            _agent.isStopped = _prevAgentStopped == false ? false : _prevAgentStopped; // 원래 상태 복원
        }

        // 공격 종료 알림
        var bt = animator.GetComponent<EnemyAI.EnemyBehaviorTree>();
        if (bt != null)
        {
            bt.SetAttacking(false);
        }
    }
}
