using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace MapSystem
{
    public class MapUIManager : MonoBehaviour
    {
        [Header("Map UI References")]
        [SerializeField] private RectTransform mapContainer;
        [SerializeField] private GameObject nodeUIPrefab;
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private TextMeshProUGUI tooltipTitle;
        [SerializeField] private TextMeshProUGUI tooltipDescription;
        
        [Header("Portal References")]
        [SerializeField] private GameObject portalPrefab; // 실제 게임 월드에 생성될 포탈 프리팹
        [SerializeField] private Transform portalContainer; // 포탈들을 담을 부모 오브젝트
        
        [Header("Map Appearance")]
        [SerializeField] private float nodeSpacingX = 150f;
        [SerializeField] private float nodeSpacingY = 100f;
        [SerializeField] private float layerSpacingY = 300f;
        
        private Dictionary<int, MapNodeUI> nodeUIElements = new Dictionary<int, MapNodeUI>();
        private Dictionary<int, GameObject> portalInstances = new Dictionary<int, GameObject>(); // 생성된 포탈 인스턴스 관리
        private MapController mapController;
        
        // Singleton instance
        public static MapUIManager Instance { get; private set; }
        
        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            
            // 씬 전환 시에도 유지
            DontDestroyOnLoad(gameObject);
            
            // Hide tooltip initially
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }
        
        private void Start()
        {
            // Find map controller
            mapController = MapController.Instance;
            
            if (mapController == null)
            {
                Debug.LogError("MapController not found!");
                return;
            }
            
            // Initialize the map UI
            Invoke("InitializeMapUI", 0.3f);
        }

        void Update()
        {
            ToggleMapUI();
        }
        
        public void InitializeMapUI()
        {
            Debug.Log("Initializing map UI");
            // Clear existing nodes
            ClearMapUI();
            
            // Get all nodes from controller
            List<MapNode> allNodes = mapController.GetAllNodes();
            Debug.Log("생성된 노드: " + allNodes.Count);
            
            // Create UI elements for each node
            foreach (var node in allNodes)
            {
                Debug.Log("노드 생성: " + node.id);
                CreateNodeUI(node);
                
                // 실제 게임 월드에 포탈 인스턴스 생성
                CreatePortalInstance(node);
            }
            
            // Create connections between nodes
            CreateNodeConnections(allNodes);
            
            // Update node accessibility
            UpdateNodeAccessibility();

            Debug.Log("맵 UI 초기화 완료");
        }
        
        private void ClearMapUI()
        {
            // UI 노드 제거
            foreach (var nodeUI in nodeUIElements.Values)
            {
                if (nodeUI != null)
                {
                    Destroy(nodeUI.gameObject);
                }
            }
            nodeUIElements.Clear();
            
            // 포탈 인스턴스 제거
            foreach (var portal in portalInstances.Values)
            {
                if (portal != null)
                {
                    Destroy(portal);
                }
            }
            portalInstances.Clear();
        }
        
        private void CreateNodeUI(MapNode node)
        {
            if (nodeUIPrefab == null || mapContainer == null) return;
            
            // Calculate position based on node layer and depth
            Vector2 position = CalculateNodePosition(node);
            
            // Create node UI element
            GameObject nodeObj = Instantiate(nodeUIPrefab, mapContainer);
            RectTransform nodeRect = nodeObj.GetComponent<RectTransform>();
            nodeRect.anchoredPosition = position;
            
            // Initialize node UI
            MapNodeUI nodeUI = nodeObj.GetComponent<MapNodeUI>();
            if (nodeUI != null)
            {
                // Initialize with node data
                nodeUI.Initialize(node, false); // Initially not accessible
                
                // Store reference
                nodeUIElements[node.id] = nodeUI;
            }
        }
        
        private Vector2 CalculateNodePosition(MapNode node)
        {
            // Calculate position based on node's layer and depth
            float x = node.position.x * nodeSpacingX;
            float y = (-node.depth * nodeSpacingY - node.layer * layerSpacingY);
        
            return new Vector2(x, y);
        }
        
        private void CreateNodeConnections(List<MapNode> allNodes)
        {
            foreach (var node in allNodes)
            {
                if (nodeUIElements.TryGetValue(node.id, out MapNodeUI sourceNodeUI))
                {
                    // Clear existing connections
                    sourceNodeUI.ClearConnections();
                    
                    // Create connections to all connected nodes
                    foreach (int connectedId in node.childNodeIds)
                    {
                        if (nodeUIElements.TryGetValue(connectedId, out MapNodeUI targetNodeUI))
                        {
                            sourceNodeUI.CreateConnectionTo(targetNodeUI);
                        }
                    }
                }
            }
        }
        
        public void UpdateNodeAccessibility()
        {
            // Get all nodes from controller
            List<MapNode> allNodes = mapController.GetAllNodes();
            
            foreach (var node in allNodes)
            {
                if (nodeUIElements.TryGetValue(node.id, out MapNodeUI nodeUI))
                {
                    // Update node UI to reflect accessibility
                    nodeUI.UpdateAccessibility(node.isAccessible);
                    
                    // If this is the current node, update its state
                    if (node.isCurrent)
                    {
                        nodeUI.UpdateAccessibility(true);
                    }
                    
                    // 포탈 인스턴스의 활성화 상태 업데이트
                    UpdatePortalAccessibility(node.id, node.isAccessible);
                }
            }
        }
        
        public void ShowNodeTooltip(MapNode node, Vector3 position)
        {
            if (tooltipPanel == null || tooltipTitle == null || tooltipDescription == null) return;
            
            // Set tooltip content
            tooltipTitle.text = GetNodeTypeName(node.nodeType);
            tooltipDescription.text = GetNodeDescription(node);
            
            // Position tooltip near the node
            RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            Vector3 screenPos = Camera.main.WorldToScreenPoint(position);
            tooltipRect.position = screenPos + new Vector3(100f, 50f, 0f); // Offset to not cover the node
            
            // Show tooltip
            tooltipPanel.SetActive(true);
        }
        
        public void HideNodeTooltip()
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }
        
        private string GetNodeTypeName(NodeType type)
        {
            switch (type)
            {
                case NodeType.Battle: return "전투";
                case NodeType.Shop: return "상점";
                case NodeType.Camp: return "캠프";
                case NodeType.Elite: return "엘리트 전투";
                case NodeType.Boss: return "보스 전투";
                case NodeType.Start: return "시작";
                default: return "";
            }
        }
        
        private string GetNodeDescription(MapNode node)
        {
            string description = "";
            
            switch (node.nodeType)
            {
                case NodeType.Battle:
                    description = "일반 적과의 전투입니다.\n";
                    description += $"난이도: {node.difficultyMultiplier:F1}x\n";
                    description += $"보상: {node.rewardMultiplier:F1}x";
                    break;
                case NodeType.Shop:
                    description = "상점에서 아이템을 구매할 수 있습니다.\n";
                    description += "골드를 사용하여 무기, 방어구, 포션 등을 구매하세요.";
                    break;
                case NodeType.Camp:
                    description = "휴식을 취하고 체력을 회복할 수 있는 캠프입니다.\n";
                    description += "HP를 회복하고 버프를 받을 수 있습니다.";
                    break;
                case NodeType.Elite:
                    description = "강력한 엘리트 적과의 전투입니다.\n";
                    description += $"난이도: {node.difficultyMultiplier:F1}x\n";
                    description += $"보상: {node.rewardMultiplier:F1}x";
                    break;
                case NodeType.Boss:
                    description = "최종 보스와의 전투입니다.\n";
                    description += $"난이도: {node.difficultyMultiplier:F1}x\n";
                    description += "승리하면 다음 층으로 진행할 수 있습니다.";
                    break;
                case NodeType.Start:
                    description = "여정의 시작점입니다.";
                    break;
                default:
                    description = "";
                    break;
            }
            
            return description;
        }
        
        // Call this when the map needs to be refreshed (e.g., after traveling to a new node)
        public void RefreshMap()
        {
            UpdateNodeAccessibility();
        }

        // 맵 노드 데이터를 기반으로 실제 게임 월드에 포탈 인스턴스 생성
        private void CreatePortalInstance(MapNode node)
        {
            if (portalPrefab == null || portalContainer == null) return;
            
            // 노드 타입에 따라 포탈 위치 결정 (실제 게임에 맞게 조정 필요)
            Vector3 portalPosition = portalContainer.transform.position;
            
            // 포탈 인스턴스 생성
            GameObject portalInstance = Instantiate(portalPrefab, portalPosition, Quaternion.identity, portalContainer);
            
            // 포탈 이름 설정
            portalInstance.name = $"Portal_{GetNodeTypeName(node.nodeType)}_{node.id}";
            
            // 포탈에 노드 데이터 연결 (필요시 컴포넌트 추가)
            PortalBehavior portalBehavior = portalInstance.AddComponent<PortalBehavior>();
            portalBehavior.Initialize(node, this);
            
            // 포탈 인스턴스 딕셔너리에 저장
            portalInstances[node.id] = portalInstance;
            
            // 초기 접근성 설정
            UpdatePortalAccessibility(node.id, node.isAccessible);
        }
        
        // 노드 데이터를 기반으로 포탈의 월드 위치 계산
        private Vector3 CalculatePortalWorldPosition(MapNode node)
        {
            // 기본 위치 설정 (실제 게임에 맞게 조정 필요)
            float xPos = node.position.x * 2.0f; // UI 위치에서 월드 위치로 변환 (비율 조정 필요)
            float yPos = 0.5f; // 바닥으로부터 높이
            float zPos = node.position.y * -2.0f; // UI의 y값을 월드의 z값으로 변환
            
            // 노드 타입에 따라 위치 조정 (선택사항)
            switch (node.nodeType)
            {
                case NodeType.Boss:
                    zPos -= 5.0f; // 보스는 더 멀리 배치
                    break;
                case NodeType.Shop:
                    xPos += 1.0f; // 상점은 약간 오른쪽에 배치
                    break;
                case NodeType.Camp:
                    xPos -= 1.0f; // 캠프는 약간 왼쪽에 배치
                    break;
            }
            
            return new Vector3(xPos, yPos, zPos);
        }
        
        // 포탈 인스턴스의 접근성 업데이트
        private void UpdatePortalAccessibility(int nodeId, bool isAccessible)
        {
            if (portalInstances.TryGetValue(nodeId, out GameObject portalInstance) && portalInstance != null)
            {
                // 포탈 활성화/비활성화
                portalInstance.SetActive(isAccessible);
                
                // 포탈 컴포넌트 업데이트
                PortalBehavior portalBehavior = portalInstance.GetComponent<PortalBehavior>();
                if (portalBehavior != null)
                {
                    portalBehavior.UpdateAccessibility(isAccessible);
                }
            }
        }

        public void ToggleMapUI()
        {
            if(Input.GetKeyDown(KeyCode.M))
            {
                Debug.Log("맵 열기");
                mapContainer.gameObject.SetActive(!mapContainer.gameObject.activeSelf);
            }
            else if(Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log("맵 닫기");
                mapContainer.gameObject.SetActive(false);
            }
        }
    }
}
