using System;
using UnityEngine;
using Unity.Cinemachine;

public class LockOnSystem : MonoBehaviour
{
    [SerializeField] float lockOnDistance = 10f;
    [SerializeField] LayerMask enemyLayer;
    public Transform currentTarget;
    private CinemachineCamera vcam;
    private GameObject playerCameraRoot;

    private void Start()
    {
        vcam = FindFirstObjectByType<CinemachineCamera>();
        playerCameraRoot = GameObject.Find("PlayerCameraRoot");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (currentTarget == null)
                currentTarget = FindNearestTarget(lockOnDistance); // 락온
            else
                currentTarget = null; // 락온 해제
        }
    }
    
    void LateUpdate()
    {
        if (currentTarget != null)
        {
            vcam.Follow = playerCameraRoot.transform;
            vcam.LookAt = currentTarget != null ? currentTarget : playerCameraRoot.transform;
            
            if (!currentTarget.gameObject.activeInHierarchy)
            {
                currentTarget = null;
                vcam.LookAt = null;
                return;
            }
        }
    }
    
    public Transform FindNearestTarget(float radius)
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius, enemyLayer);
        Transform nearestTarget = null;
        float minDistance = Mathf.Infinity;

        foreach (var collider in colliders)
        {
            float dist = Vector3.Distance(transform.position, collider.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearestTarget = collider.transform;
            }
        }

        return nearestTarget;
    }
    
    public void Unlock()
    {
        currentTarget = null;
        vcam.LookAt = playerCameraRoot.transform;
    }

    public Transform GetCurrentTarget()
    {
        return currentTarget;
    }
}
