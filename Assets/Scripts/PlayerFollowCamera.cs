using UnityEngine;
using Unity.Cinemachine;
using Unity.VisualScripting;

public class CinemachineAutoTarget : MonoBehaviour
{
    [SerializeField] private string targetName = "PlayerCameraRoot";
    private CinemachineCamera vcam;

    private void Awake()
    {
        vcam = GetComponent<CinemachineCamera>();
        if (vcam == null)
        {
            Debug.LogError("CinemachineVirtualCamera 컴포넌트를 찾을 수 없습니다.");
            return;
        }
    }
    
    private void Start()
    {
        // 씬 시작 시 타겟 탐색
        GameObject target = GameObject.Find(targetName);
        if (target != null)
        {
            vcam.Follow = target.transform;
            vcam.LookAt = target.transform;
            Debug.Log("Cinemachine 타겟을 PlayerCameraRoot로 설정했습니다.");
        }
        else
        {
            Debug.LogWarning("PlayerCameraRoot 오브젝트를 찾을 수 없습니다.");
        }
    }
}