using System;
using UnityEngine;
using Unity.Cinemachine;

public class LockOnSystem : MonoBehaviour
{
    [SerializeField] float lockOnDistance = 10f;
    [SerializeField] LayerMask enemyLayer;
    [SerializeField] private CinemachineTargetGroup targetGroup;
    [SerializeField] private Transform lookAtProxy;
    public Transform currentTarget;
    private CinemachineCamera vcam;
    private GameObject playerCameraRoot;

    private CinemachineRotationComposer rotationComposer;
    private CinemachineGroupFraming groupComposer;

    private void Start()
    {
        vcam = FindFirstObjectByType<CinemachineCamera>();
        playerCameraRoot = GameObject.Find("PlayerCameraRoot");

        rotationComposer = vcam.GetComponent<CinemachineRotationComposer>();
        groupComposer = vcam.GetComponent<CinemachineGroupFraming>();

        if (rotationComposer != null) rotationComposer.enabled = false;
        if (groupComposer != null) groupComposer.enabled = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (currentTarget == null)
            {
                Transform target = FindNearestTarget(lockOnDistance);
                if (target != null)
                    LockOn(target);
            }
            else
            {
                Unlock();
            }
        }
    }
    
    void LateUpdate()
    {
        if (currentTarget != null)
        {
            lookAtProxy.position = Vector3.Lerp(lookAtProxy.position, currentTarget.position, Time.deltaTime * 5f);
            vcam.Follow = playerCameraRoot.transform;
            vcam.LookAt = lookAtProxy;

            if (!currentTarget.gameObject.activeInHierarchy)
            {
                currentTarget = null;
                vcam.LookAt = null;
                return;
            }
        }
        else
        {
            vcam.LookAt = playerCameraRoot.transform;
        }
    }

    private void LockOn(Transform target)
    {
        Debug.Log("락온");

        // 구성 요소가 null이 아닌지 확인
        if (targetGroup == null || playerCameraRoot == null || target == null)
            return;

        Transform lockOnTarget = target.Find("LockOnTarget");
        if (lockOnTarget == null)
        {
            Debug.LogWarning("LockOnTarget not found on the target object.");
            lockOnTarget = target;
        }

        currentTarget = lockOnTarget;

        targetGroup.Targets.Clear();
        targetGroup.Targets.Add(new CinemachineTargetGroup.Target
        {
            Object = playerCameraRoot.transform,
            Weight = 1,
            Radius = 0.5f
        });
        targetGroup.Targets.Add(new CinemachineTargetGroup.Target
        {
            Object = lockOnTarget,
            Weight = 1,
            Radius = 0.5f
        });

        if (rotationComposer != null) rotationComposer.enabled = true;
        if (groupComposer != null) groupComposer.enabled = true;

        // vcam.LookAt = targetGroup.transform;
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

        if (targetGroup == null || playerCameraRoot == null)
            return;

        if (rotationComposer != null) rotationComposer.enabled = false;
        if (groupComposer != null) groupComposer.enabled = false;

        targetGroup.Targets.Clear();
        targetGroup.Targets.Add(new CinemachineTargetGroup.Target
        {
            Object = playerCameraRoot.transform,
            Weight = 1,
            Radius = 0.5f
        });

        vcam.LookAt = playerCameraRoot.transform;
    }

    public Transform GetCurrentTarget()
    {
        return currentTarget;
    }
}
