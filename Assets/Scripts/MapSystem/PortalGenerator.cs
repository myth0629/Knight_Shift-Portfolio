using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MapSystem;

public class PortalGenerator : MonoBehaviour
{
    [Header("Portal Prefabs")]
    [SerializeField] private GameObject battlePortalPrefab;
    [SerializeField] private GameObject shopPortalPrefab;
    [SerializeField] private GameObject campPortalPrefab;
    [SerializeField] private GameObject bossPortalPrefab;

    [Header("Portal Placement")]
    [SerializeField] private Transform portalParent;
    [SerializeField] private float portalSpacingX = 3f;
    [SerializeField] private float portalSpacingZ = 3f;
    [SerializeField] private Vector3 startPosition = new Vector3(0, 0, 0);
    [SerializeField] private Vector3 startRotation = new Vector3(0, 0, 0);

    private Dictionary<int, GameObject> spawnedPortals = new Dictionary<int, GameObject>();
    private MapController mapController;
    
    private void Awake()
    {
        if (portalParent == null)
        {
            var found = GameObject.Find("Portal Parent");
            if (found != null)
            {
                portalParent = found.transform;
                Debug.Log("portalParent assigned via Awake.");
            }
            else
            {
                Debug.LogWarning("Portal Parent object not found in scene during Awake.");
            }
        }
    }
    
    private void Start()
    {
        //MapController.Instance.OnNodeSelected += HandleNodeSelected;
        // 초기 포탈 생성도 여기서
        //HandleNodeSelected(MapController.Instance.GetAllNodes().Find(n => n.isCurrent));
        
        mapController = MapController.Instance;
        if (mapController == null)
        {
            Debug.LogError("MapController not found!");
            return;
        }

        // 맵 컨트롤러의 노드 선택 이벤트 구독
        mapController.OnNodeSelected += OnNodeSelected;
        
        // 1초 후에 포털 생성 (맵이 먼저 초기화되도록)
        Invoke("GeneratePortals", 1f);
    }
    
    private void HandleNodeSelected(MapNode node)
    {
        // 포탈 전부 삭제
        ClearPortals();
        // 현재 노드의 childNodeIds 기준으로 새 포탈 생성
        var allNodes = MapController.Instance.GetAllNodes();
        foreach (int childId in node.childNodeIds)
        {
            MapNode childNode = allNodes.Find(n => n.id == childId);
            if (childNode != null)
                CreatePortalForNode(childNode, childNode.position);
        }
    }

    public void GeneratePortals()
    {
        ClearPortals();

        List<MapNode> allNodes = mapController.GetAllNodes();
        
        MapNode currentNode = allNodes.Find(n => n.isCurrent);
        if (currentNode == null)
        {
            Debug.LogWarning("No current node found.");
            return;
        }

        List<MapNode> nodesToSpawn = new List<MapNode>();

        foreach (int childId in currentNode.childNodeIds)
        {
            MapNode childNode = allNodes.Find(n => n.id == childId);
            if (childNode != null)
            {
                nodesToSpawn.Add(childNode);
            }
        }

        float startX = startPosition.x - ((nodesToSpawn.Count - 1) * portalSpacingX / 2f);
        float startZ = startPosition.z - ((nodesToSpawn.Count - 1) * portalSpacingZ / 2f);
            
        Debug.Log($"currentNode: {currentNode.id}, 자식 개수: {currentNode.childNodeIds.Count}");
        Debug.Log($"생성할 포탈 개수: {nodesToSpawn.Count}");

        for (int i = 0; i < nodesToSpawn.Count; i++)
        {
            MapNode node = nodesToSpawn[i];
            float xPos = startX + (i * portalSpacingX);
            float zPos = startZ + (i * portalSpacingZ);
            CreatePortalForNode(node, new Vector3(xPos, startPosition.y, zPos));
        }

        UpdatePortalAccessibility();
    }
    
    private void CreatePortalForNode(MapNode node, Vector3 position)
    {
        Debug.Log($"Creating portal for node: {node.id} at position: {position}");
        // 노드 타입별 포털 프리팹 선택
        GameObject prefab = GetPrefabForNodeType(node.nodeType);
        if (prefab == null) return;
        
        // 포털 인스턴스 생성: prefab을 portalParent의 로컬 공간에서 Vector3.zero에 인스턴스화 한 뒤, localPosition을 position으로 설정
        GameObject portal = Instantiate(prefab, Vector3.zero, Quaternion.identity, portalParent);
        portal.transform.localPosition = position;
        portal.transform.localRotation = Quaternion.Euler(startRotation);
        
        portal.name = $"Portal_{node.nodeType}_{node.id}";
        
        // 포털 컴포넌트에 노드 ID 설정
        Portal portalComponent = portal.GetComponent<Portal>();
        if (portalComponent != null)
        {
            portalComponent.SetNodeId(node.id);
            Debug.Log($"Set nodeId {node.id} to Portal component");
        }
        else
        {
            Debug.LogWarning("Portal component not found on prefab!");
        }
        
        // 포털 라벨 설정
        PortalLabel portalLabel = portal.GetComponent<PortalLabel>();
        if (portalLabel != null)
        {
            string portalName = GetPortalNameForNode(node);
            portalLabel.SetPortalName(portalName);
            Debug.Log($"Set portal name '{portalName}' for node {node.id}");
        }
        else
        {
            Debug.LogWarning($"PortalLabel component not found on portal prefab for node {node.id}!");
        }
        
        // 딕셔너리에 저장
        spawnedPortals[node.id] = portal;
        
        // 현재 접근 불가 상태로 시작
        portal.SetActive(node.isAccessible || node.isCurrent);
    }
    
    private GameObject GetPrefabForNodeType(NodeType type)
    {
        switch (type)
        {
            case NodeType.Battle: return battlePortalPrefab;
            case NodeType.Shop: return shopPortalPrefab;
            case NodeType.Camp: return campPortalPrefab;
            case NodeType.Boss: return bossPortalPrefab;
            default: 
                Debug.LogWarning($"No portal prefab defined for node type: {type}");
                return null; // 기본 예비 프리팹은 사용하지 않음
        }
    }
    
    /// <summary>
    /// 노드 타입과 ID에 따라 포털 이름을 생성합니다
    /// </summary>
    /// <param name="node">맵 노드</param>
    /// <returns>포털 이름</returns>
    private string GetPortalNameForNode(MapNode node)
    {
        string baseName = GetNodeTypeName(node.nodeType);
        
        // 노드 ID 또는 추가 정보를 포함하여 이름 생성
        if (node.nodeType == NodeType.Boss)
        {
            return $"{baseName}";  // 보스는 단순하게
        }
        else
        {
            return $"{baseName} #{node.id}";  // 다른 타입들은 ID 포함
        }
    }
    
    /// <summary>
    /// 노드 타입에 따른 한국어 이름을 반환합니다
    /// </summary>
    /// <param name="type">노드 타입</param>
    /// <returns>한국어 이름</returns>
    private string GetNodeTypeName(NodeType type)
    {
        switch (type)
        {
            case NodeType.Battle: return "전투";
            case NodeType.Shop: return "상점";
            case NodeType.Camp: return "휴식지";
            case NodeType.Boss: return "보스";
            default: return "포털";
        }
    }
    
    public void OnNodeSelected(MapNode node)
    {
        // 노드가 선택되면 접근 가능한 포털 업데이트
        UpdatePortalAccessibility();
    }
    
    private void UpdatePortalAccessibility()
    {
        // 맵 노드 가져오기
        List<MapNode> allNodes = mapController.GetAllNodes();
        
        // 모든 포털의 접근성 업데이트
        foreach (var node in allNodes)
        {
            if (spawnedPortals.TryGetValue(node.id, out GameObject portal))
            {
                // 접근 가능 또는 현재 노드인 경우만 활성화
                portal.SetActive(node.isAccessible || node.isCurrent);
                
                // 현재 노드는 특별한 효과 추가 (예: 색상 변경)
                if (node.isCurrent)
                {
                    // 현재 선택된 포털 표시
                    Renderer renderer = portal.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        // 원래 색상 저장 후 하이라이트 색상으로 변경
                        renderer.material.SetColor("_EmissionColor", Color.yellow * 2f);
                        renderer.material.EnableKeyword("_EMISSION");
                    }
                }
            }
        }
    }
    
    private void ClearPortals()
    {
        // 기존 포털 및 연결선 제거
        foreach (var portal in spawnedPortals.Values)
        {
            if (portal != null)
            {
                Destroy(portal);
            }
        }
        spawnedPortals.Clear();
    }
}