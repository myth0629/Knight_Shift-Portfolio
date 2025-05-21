using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MapSystem
{
    // 씬에 배치하는 포탈 스폰 포인트
    public class PortalSpawnPoint : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [Tooltip("이 스폰 포인트에서 생성할 포탈 타입 (없으면 모든 타입 허용)")]
        [SerializeField] private NodeType[] allowedNodeTypes;
        
        [Tooltip("이 스폰 포인트의 고유 ID (같은 씬에서 구분용)")]
        [SerializeField] private string spawnPointId = "default";
        
        [Header("Gizmo Settings")]
        [SerializeField] private Color gizmoColor = Color.cyan;
        [SerializeField] private float gizmoRadius = 0.5f;
        
        // 이 스폰 포인트에 포탈이 이미 생성되었는지 여부
        private bool isOccupied = false;
        
        // 생성된 포탈 인스턴스
        private GameObject spawnedPortal;
        
        // 특정 노드 타입의 포탈을 생성할 수 있는지 확인
        public bool CanSpawnNodeType(NodeType nodeType)
        {
            // 허용된 타입이 없으면 모든 타입 허용
            if (allowedNodeTypes == null || allowedNodeTypes.Length == 0)
                return true;
            
            // 허용된 타입 목록에 있는지 확인
            foreach (NodeType allowedType in allowedNodeTypes)
            {
                if (nodeType == allowedType)
                    return true;
            }
            
            return false;
        }
        
        // 포탈 생성
        public GameObject SpawnPortal(GameObject portalPrefab, MapNode node, MapUIManager manager)
        {
            if (isOccupied || portalPrefab == null)
                return null;
            
            // 노드 타입이 허용되는지 확인
            if (!CanSpawnNodeType(node.nodeType))
                return null;
            
            // 포탈 생성
            spawnedPortal = Instantiate(portalPrefab, transform.position, transform.rotation);
            spawnedPortal.name = $"Portal_{node.nodeType}_{node.id}";
            
            // 포탈 컴포넌트 초기화
            PortalBehavior portalBehavior = spawnedPortal.AddComponent<PortalBehavior>();
            portalBehavior.Initialize(node, manager);
            
            // 점유 상태 변경
            isOccupied = true;
            
            return spawnedPortal;
        }
        
        // 포탈 제거
        public void ClearPortal()
        {
            if (spawnedPortal != null)
            {
                Destroy(spawnedPortal);
                spawnedPortal = null;
            }
            
            isOccupied = false;
        }
        
        // 스폰 포인트 ID 반환
        public string GetSpawnPointId()
        {
            return spawnPointId;
        }
        
        // 점유 상태 확인
        public bool IsOccupied()
        {
            return isOccupied;
        }
        
        // 기즈모 표시
        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position, gizmoRadius);
            
            // 허용된 노드 타입 표시
            if (allowedNodeTypes != null && allowedNodeTypes.Length > 0)
            {
                string typeText = "";
                foreach (NodeType type in allowedNodeTypes)
                {
                    typeText += type.ToString() + " ";
                }
                
                // 텍스트 표시 (Scene 뷰에서만 보임)
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position + Vector3.up * gizmoRadius, 
                    $"ID: {spawnPointId}\nTypes: {typeText}");
                #endif
            }
        }
    }
}
