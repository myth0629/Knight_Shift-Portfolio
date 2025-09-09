using UnityEngine;
using Unity.Cinemachine;
using System.Linq;
using System.Collections;

public class LockOnSystem : MonoBehaviour
{
    [Header("락온 설정")]
    [SerializeField] private float lockOnDistance = 20f;
    [SerializeField] private LayerMask enemyLayer;
    [Tooltip("락온 해제 시 카메라 전환 시간")]
    [SerializeField] private float transitionTime = 0.3f;

    [Header("필수 컴포넌트 연결")]
    [SerializeField] private Transform playerTransform;
    [Tooltip("락온 시 활성화될 전용 가상 카메라")]
    [SerializeField] private CinemachineCamera lockOnVirtualCamera;
    [Tooltip("일반 팔로우 카메라")]
    [SerializeField] private CinemachineCamera followCamera;
    [Tooltip("카메라 타겟 오브젝트 (Cinemachine Camera Target)")]
    [SerializeField] private Transform cameraTarget;

    // ThirdPersonController 참조 (카메라 회전 값 동기화용)
    private StarterAssets.ThirdPersonController thirdPersonController;
    
    // 전환 관련 변수
    private bool isTransitioning = false;
    private Coroutine transitionCoroutine;

    public Transform CurrentTarget { get; private set; }

    // 평소 카메라의 기본 우선순위
    private const int NORMAL_PRIORITY = 10;
    // 락온 카메라가 활성화될 때의 우선순위
    private const int LOCKON_PRIORITY = 20;


    private void Awake()
    {
        // 필수 컴포넌트 확인
        if (playerTransform == null || lockOnVirtualCamera == null || followCamera == null || cameraTarget == null)
        {
            Debug.LogError("LockOnSystem에 필요한 컴포넌트가 연결되지 않았습니다!");
            enabled = false;
            return;
        }

        // ThirdPersonController 컴포넌트 찾기
        thirdPersonController = playerTransform.GetComponent<StarterAssets.ThirdPersonController>();
        if (thirdPersonController == null)
        {
            Debug.LogWarning("ThirdPersonController를 찾을 수 없습니다. 카메라 회전 동기화가 작동하지 않을 수 있습니다.");
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

        // 락온 거리를 벗어나면 자동 언록
        if (CurrentTarget != null)
        {
            float distanceToTarget = Vector3.Distance(playerTransform.position, CurrentTarget.position);
            if (distanceToTarget > lockOnDistance)
            {
                Unlock();
            }
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
            
            // 블렌드 시작 전에 두 카메라 위치/회전 일치 -> 초기 전환 시 시점 튐 방지
            lockOnVirtualCamera.transform.position = followCamera.transform.position;
            lockOnVirtualCamera.transform.rotation = followCamera.transform.rotation;
            lockOnVirtualCamera.Priority = LOCKON_PRIORITY;
        }
    }

    public void Unlock()
    {
        // 락온 해제 전에 현재 락온 카메라의 회전을 일반 카메라에 동기화
        if (CurrentTarget != null)
        {
            SyncCameraRotation();
        }
        
        CurrentTarget = null;
        
        // 진행 중인 전환 코루틴이 있다면 중지
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        
        // 부드러운 전환으로 락온 카메라 비활성화
        transitionCoroutine = StartCoroutine(SmoothUnlock());
    }
    
    private System.Collections.IEnumerator SmoothUnlock()
    {
        isTransitioning = true;
        float elapsedTime = 0f;
        
        // 락온 카메라의 초기 우선순위
        int initialPriority = lockOnVirtualCamera.Priority;
        
        while (elapsedTime < transitionTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionTime;
            
            // 우선순위를 점진적으로 감소 (더 부드러운 전환을 위해)
            lockOnVirtualCamera.Priority = Mathf.RoundToInt(Mathf.Lerp(initialPriority, NORMAL_PRIORITY - 5, t));
            
            yield return null;
        }
        
        // 완전히 비활성화
        lockOnVirtualCamera.LookAt = null;
        lockOnVirtualCamera.Priority = NORMAL_PRIORITY - 5;
        isTransitioning = false;
        transitionCoroutine = null;
    }

    private void SyncCameraRotation()
    {
        if (thirdPersonController == null || CurrentTarget == null) return;

        // 현재 락온 카메라의 Transform을 가져옴
        Transform lockOnCamTransform = lockOnVirtualCamera.transform;
        
        // 락온 카메라에서 타겟을 바라보는 방향 계산
        Vector3 directionToTarget = CurrentTarget.position - lockOnCamTransform.position;
        
        // 월드 좌표에서의 Yaw 계산 (Y축 회전)
        float targetYaw = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg;
        
        // 월드 좌표에서의 Pitch 계산 (X축 회전)
        float horizontalDistance = Mathf.Sqrt(directionToTarget.x * directionToTarget.x + directionToTarget.z * directionToTarget.z);
        float targetPitch = -Mathf.Atan2(directionToTarget.y, horizontalDistance) * Mathf.Rad2Deg;
        
        // ThirdPersonController의 카메라 회전 값을 동기화
        thirdPersonController.SetCameraRotation(targetYaw, targetPitch);
        
        Debug.Log($"카메라 회전 동기화: Yaw={targetYaw:F1}, Pitch={targetPitch:F1}");
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