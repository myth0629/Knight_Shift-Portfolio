using UnityEngine;
using Unity.Cinemachine;
using System.Linq;

public class LockOnSystem : MonoBehaviour
{
    [Header("락온 설정")]
    [SerializeField] private float lockOnDistance = 20f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("필수 컴포넌트 연결")]
    [SerializeField] private Transform playerTransform;
    [Tooltip("락온 시 활성화될 전용 가상 카메라")]
    [SerializeField] private CinemachineCamera lockOnVirtualCamera;

    public Transform CurrentTarget { get; private set; }

    // 평소 카메라의 기본 우선순위
    private const int NORMAL_PRIORITY = 10;
    // 락온 카메라가 활성화될 때의 우선순위
    private const int LOCKON_PRIORITY = 20;


    private void Awake()
    {
        // 필수 컴포넌트 확인
        if (playerTransform == null || lockOnVirtualCamera == null)
        {
            Debug.LogError("LockOnSystem에 필요한 컴포넌트가 연결되지 않았습니다!");
            enabled = false;
            return;
        }

        // 시작 시 락온 카메라 비활성화 (LookAt을 비우고 우선순위를 낮춤)
        Unlock();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (CurrentTarget == null)
                LockOn();
            else
                Unlock();
        }

        if (CurrentTarget != null && !CurrentTarget.gameObject.activeInHierarchy)
        {
            Unlock();
        }
    }

    private void LockOn()
    {
        Transform nearestTarget = FindNearestTarget(); // 이 부분은 원래의 '가까운 적 찾기'로 되돌렸습니다.
        if (nearestTarget != null)
        {
            CurrentTarget = nearestTarget; // CurrentTarget은 적의 루트 Transform을 유지
            
            // CurrentTarget의 자식 중에 "AimTarget"이라는 이름의 오브젝트를 찾습니다.
            Transform aimPoint = CurrentTarget.Find("LockOnTarget");

            // 만약 'AimTarget'을 찾았다면 그것을 조준점으로 사용하고,
            // 찾지 못했다면 그냥 원래의 타겟(CurrentTarget)을 조준합니다.
            Transform finalLookAtTarget = (aimPoint != null) ? aimPoint : CurrentTarget;
            // --- 여기까지 ---

            // 최종 조준점을 LookAt 타겟으로 설정합니다.
            lockOnVirtualCamera.LookAt = finalLookAtTarget;
            lockOnVirtualCamera.Priority = LOCKON_PRIORITY;
        }
    }

    public void Unlock()
    {
        CurrentTarget = null;
        // 락온 카메라의 LookAt을 비우고
        lockOnVirtualCamera.LookAt = null;
        // 우선순위를 낮춰 평소 카메라로 돌아가게 합니다.
        lockOnVirtualCamera.Priority = NORMAL_PRIORITY - 5; // 확실하게 비활성화
    }

    private Transform FindNearestTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(playerTransform.position, lockOnDistance, enemyLayer);
        if (colliders.Length == 0) return null;
        return colliders.OrderBy(c => Vector3.Distance(playerTransform.position, c.transform.position)).First().transform;
    }

    public Transform GetCurrentTarget()
    {
        return CurrentTarget;
    }
}