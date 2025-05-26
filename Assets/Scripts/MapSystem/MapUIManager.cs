using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        
        [Header("Map Appearance")]
        [SerializeField] private float nodeSpacingX = 150f;
        [SerializeField] private float nodeSpacingY = 100f;
        [SerializeField] private float layerSpacingY = 300f;
        
        private Dictionary<int, MapNodeUI> nodeUIElements = new Dictionary<int, MapNodeUI>();
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
            }
            
            // Create connections between nodes
            CreateNodeConnections(allNodes);
            
            // Update node accessibility
            UpdateNodeAccessibility();

            Debug.Log("맵 UI 초기화 완료");
        }
        
        private void ClearMapUI()
        {
            foreach (var nodeUI in nodeUIElements.Values)
            {
                if (nodeUI != null)
                {
                    Destroy(nodeUI.gameObject);
                }
            }
            
            nodeUIElements.Clear();
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
            
            // Update all nodes
            foreach (var nodeUI in nodeUIElements.Values)
            {
                MapNode nodeData = nodeUI.GetNodeData();
                
                // Find the corresponding node in the controller's data
                MapNode controllerNode = allNodes.Find(n => n.id == nodeData.id);
                
                if (controllerNode != null)
                {
                    // Update node UI based on the node's accessibility in the controller
                    nodeUI.UpdateAccessibility(controllerNode.isAccessible || controllerNode.isCurrent);
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

        public void ToggleMapUI()
        {
            if(Input.GetKeyDown(KeyCode.M))
            {
                mapContainer.gameObject.SetActive(!mapContainer.gameObject.activeSelf);
            }
            else if(Input.GetKeyDown(KeyCode.Escape))
            {
                mapContainer.gameObject.SetActive(false);
            }
        }
    }
}
