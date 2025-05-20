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
    [SerializeField] private GameObject elitePortalPrefab;
    [SerializeField] private GameObject bossPortalPrefab;

    [Header("Portal Placement")]
    [SerializeField] private Transform portalParent;
    [SerializeField] private float portalSpacingX = 3f;
    [SerializeField] private float portalSpacingZ = 3f;
    [SerializeField] private Vector3 startPosition = new Vector3(0, 0, 0);

    private Dictionary<int, GameObject> spawnedPortals = new Dictionary<int, GameObject>();
    private MapController mapController;

    private void Start()
    {
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

    public void GeneratePortals()
    {
        // 기존 포털 제거
        ClearPortals();
        
        // 맵 노드 가져오기
        List<MapNode> allNodes = mapController.GetAllNodes();
        
        // 각 층별로 노드 구분
        Dictionary<int, List<MapNode>> nodesByLayer = new Dictionary<int, List<MapNode>>();
        foreach (var node in allNodes)
        {
            if (!nodesByLayer.ContainsKey(node.layer))
            {
                nodesByLayer[node.layer] = new List<MapNode>();
            }
            nodesByLayer[node.layer].Add(node);
        }
        
        // 각 층별로 포털 생성
        foreach (var layerPair in nodesByLayer)
        {
            int layer = layerPair.Key;
            List<MapNode> nodesInLayer = layerPair.Value;
            
            // 층별 배치를 위해 Z 좌표 계산
            float zPos = startPosition.z + (layer * portalSpacingZ);
            
            // 각 층의 노드 수에 따라 X 위치 조절
            int nodeCount = nodesInLayer.Count;
            float startX = startPosition.x - ((nodeCount - 1) * portalSpacingX / 2f);
            
            // 각 노드에 대한 포털 생성
            for (int i = 0; i < nodesInLayer.Count; i++)
            {
                MapNode node = nodesInLayer[i];
                float xPos = startX + (i * portalSpacingX);
                
                // 포털 생성
                CreatePortalForNode(node, new Vector3(xPos, startPosition.y, zPos));
            }
        }
        
        // 접근 가능한 포털만 활성화
        UpdatePortalAccessibility();
        
        // 연결선 생성
        CreatePortalConnections(allNodes);
    }
    
    private void CreatePortalForNode(MapNode node, Vector3 position)
    {
        // 노드 타입별 포털 프리팹 선택
        GameObject prefab = GetPrefabForNodeType(node.nodeType);
        if (prefab == null) return;
        
        // 포털 인스턴스 생성
        GameObject portal = Instantiate(prefab, position, Quaternion.identity, portalParent);
        portal.name = $"Portal_{node.nodeType}_{node.id}";
        
        // 포털에 노드 ID 정보 저장
        PortalInteraction portalInteraction = portal.AddComponent<PortalInteraction>();
        portalInteraction.Initialize(node.id, this);
        
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
            case NodeType.Elite: return elitePortalPrefab;
            case NodeType.Boss: return bossPortalPrefab;
            case NodeType.Start: return battlePortalPrefab; // 시작 노드는 전투 포털 사용
            default: return battlePortalPrefab;
        }
    }
    
    private void CreatePortalConnections(List<MapNode> allNodes)
    {
        // 각 노드 연결에 대해 시각적 연결선 생성
        foreach (var node in allNodes)
        {
            if (!spawnedPortals.ContainsKey(node.id)) continue;
            
            GameObject sourcePortal = spawnedPortals[node.id];
            
            foreach (int childId in node.childNodeIds)
            {
                if (spawnedPortals.ContainsKey(childId))
                {
                    GameObject targetPortal = spawnedPortals[childId];
                    CreateConnectionLine(sourcePortal.transform.position, targetPortal.transform.position);
                }
            }
        }
    }
    
    private void CreateConnectionLine(Vector3 start, Vector3 end)
    {
        // 라인 렌더러로 연결선 생성
        GameObject lineObj = new GameObject("ConnectionLine");
        lineObj.transform.SetParent(portalParent);
        
        LineRenderer line = lineObj.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.SetPosition(0, start + Vector3.up * 0.1f); // 약간 위로 올려서 바닥과 겹치지 않게
        line.SetPosition(1, end + Vector3.up * 0.1f);
        
        line.startWidth = 0.05f;
        line.endWidth = 0.05f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        line.endColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
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
        
        // 연결선 제거
        LineRenderer[] lines = portalParent.GetComponentsInChildren<LineRenderer>();
        foreach (var line in lines)
        {
            Destroy(line.gameObject);
        }
    }
    
    public void OnPortalClicked(int nodeId)
    {
        // 플레이어가 포털을 클릭하면 맵 컨트롤러에 노드 선택 전달
        mapController.SelectNode(nodeId);
    }
}

// 포털 클릭 이벤트를 처리하는 컴포넌트
public class PortalInteraction : MonoBehaviour
{
    private int nodeId;
    private PortalGenerator portalGenerator;
    
    public void Initialize(int id, PortalGenerator generator)
    {
        nodeId = id;
        portalGenerator = generator;
    }
    
    private void OnMouseDown()
    {
        // 포털 클릭 시 이벤트 발생
        portalGenerator.OnPortalClicked(nodeId);
    }
}