using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace MapSystem
{
    public enum NodeType
    {
        Start,
        Battle,
        Shop,
        Camp,
        Boss
    }
    
    [System.Serializable]
    public class MapNode
    {
        [Tooltip("노드 고유 ID")]
        public int id;
        
        [Tooltip("노드 타입")]
        public NodeType nodeType;
        
        [Tooltip("노드가 위치한 층")]
        public int layer;
        
        [Tooltip("층 내에서의 위치 인덱스")]
        public int depth;
        
        [Tooltip("UI 상의 위치")]
        public Vector2 position;
        
        [Tooltip("이 노드 선택 시 로드할 씬 이름")]
        public string sceneName;
        
        [Tooltip("난이도 배율")]
        public float difficultyMultiplier = 1f;
        
        [Tooltip("보상 배율")]
        public float rewardMultiplier = 1f;
        
        // 노드 연결 정보
        [Tooltip("이 노드에서 갈 수 있는 다음 노드들의 ID")]
        public List<int> childNodeIds = new List<int>();
        
        [Tooltip("이 노드로 올 수 있는 이전 노드들의 ID")]
        public List<int> parentNodeIds = new List<int>();
        
        // 노드 상태 추적
        [Tooltip("현재 접근 가능한 노드인지")]
        public bool isAccessible = false;
        
        [Tooltip("이미 완료한 노드인지")]
        public bool isCompleted = false;
        
        [Tooltip("현재 선택된 노드인지")]
        public bool isCurrent = false;
    }

    public class MapController : MonoBehaviour
    {
        // 싱글톤 인스턴스
        public static MapController Instance { get; private set; }

        [Header("Map Generation Settings")]
        [Tooltip("시작부터 보스까지의 총 층 수")]
        [SerializeField] private int totalLayers = 6; // 시작~보스 총 6층
        
        [Tooltip("각 층당 최소 노드 수")]
        [SerializeField] private int minNodesPerLayer = 2; // 층당 최소 2개 노드
        
        [Tooltip("각 층당 최대 노드 수")]
        [SerializeField] private int maxNodesPerLayer = 4; // 층당 최대 4개 노드
        
        [Header("Node Connection Settings")]
        [Tooltip("각 노드가 다음 층에 연결할 최소 노드 수")]
        [SerializeField] private int minBranchesPerNode = 1; // 다음 층의 최소 1개와 연결
        
        [Tooltip("각 노드가 다음 층에 연결할 최대 노드 수")]
        [SerializeField] private int maxBranchesPerNode = 2; // 다음 층의 최대 2개와 연결
        
        [Header("Special Placement Rules")]
        [Tooltip("보스 전층에 캠프나 상점 포함 여부")]
        [SerializeField] private bool placeShopOrCampBeforeBoss = true; // 보스 전층은 캠프/상점 포함
        
        [Tooltip("전투 노드 앞에 캠프 우선 배치 여부")]
        [SerializeField] private bool placeCampBeforeBattle = true; // 전투 앞에 캠프 우선 배치
        
        [Tooltip("상점이 배치되는 초반 층 수 (1부터 시작)")]
        [SerializeField] private int shopEarlyLayerLimit = 3; // 상점은 초반 2~3층에 배치
        
        // 노드 타입 결정 (비율에 따라)
        [Header("맵 타입 별 생성 비율")]
        [SerializeField] private float battleProbability = 0.6f;
        [SerializeField] private float campProbability = 0.2f;
        [SerializeField] private float shopProbability = 0.1f;

        [Header("Portal Generation")]
        [SerializeField] private PortalGenerator portalGenerator;
        
        // 생성된 맵 데이터
        private List<MapNode> mapNodes = new List<MapNode>();
        private int nextNodeId = 0;
        private MapNode currentNode;
        
        // 이벤트
        public delegate void MapNodeSelectedHandler(MapNode node);
        public event MapNodeSelectedHandler OnNodeSelected;
        
        // 씬 이름
        private const string BATTLE_SCENE = "Battle";
        private const string SHOP_SCENE = "Shop";
        private const string CAMP_SCENE = "Camp";
        private const string BOSS_SCENE = "Boss";
        private const string START_SCENE = "Battle";
        
        private void Awake()
        {
            // 싱글톤 인스턴스 설정
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            // 맵 생성
            GenerateMap();
            
            // 시작 노드 선택
            MapNode startNode = mapNodes.Find(node => node.nodeType == NodeType.Start);
            if (startNode != null)
            {
                SelectNode(startNode.id);
            }
        }
        
        private void Start()
        {
            DebugLogMapStructure();
            
            // 씬 전환 후 저장된 노드 ID가 있는지 확인
            if (PlayerPrefs.HasKey("SelectedNodeId"))
            {
                int selectedNodeId = PlayerPrefs.GetInt("SelectedNodeId");
                SelectNode(selectedNodeId);
                
                // 사용한 후 키 제거
                PlayerPrefs.DeleteKey("SelectedNodeId");
            }
        }
        
        public void GenerateMap()
        {
            mapNodes.Clear();
            nextNodeId = 0;
            
            // 맵 그리드 생성 (각 층별 노드 리스트)
            List<List<MapNode>> layerGrid = new List<List<MapNode>>();
            for (int i = 0; i < totalLayers; i++)
            {
                layerGrid.Add(new List<MapNode>());
            }
            
            // 1. 맨 아래(시작)에 Start 노드 추가
            MapNode startNode = CreateNode(NodeType.Start, 0,  0);
            startNode.position = new Vector2(0, (totalLayers - 1) * -1.5f); // 중앙 하단에 배치
            layerGrid[0].Add(startNode);
            mapNodes.Add(startNode);
            
            // 2. 맨 위(끝)에 Boss 노드 추가    
            MapNode bossNode = CreateNode(NodeType.Boss, totalLayers - 1, 0);
            bossNode.position = new Vector2(0, 0); // 중앙 상단에 배치
            layerGrid[totalLayers - 1].Add(bossNode);
            mapNodes.Add(bossNode);
            
            // 3. 중간 층에 노드 분포
            for (int layer = 1; layer < totalLayers - 1; layer++)
            {
                // 각 층에 몇 개의 노드를 생성할지 결정
                int nodesInLayer = UnityEngine.Random.Range(minNodesPerLayer, maxNodesPerLayer + 1);
                
                // 노드 간 간격 설정
                float spacing = 3.0f;
                
                for (int i = 0; i < nodesInLayer; i++)
                {
                    // 노드 타입 결정 (비율에 따라)
                    NodeType nodeType = DetermineNodeType(layer);
                    
                    // 특수 규칙 적용
                    // 보스 전층(layer==1)은 캠프나 상점 포함
                    if (layer == 1 && placeShopOrCampBeforeBoss && i == 0)
                    {
                        nodeType = UnityEngine.Random.value < 0.5f ? NodeType.Camp : NodeType.Shop;
                    }
                    
                    // 상점은 초반 2~3층에만 배치
                    if (nodeType == NodeType.Shop && layer > totalLayers - shopEarlyLayerLimit)
                    {
                        nodeType = NodeType.Camp;
                    }
                    
                    MapNode node = CreateNode(nodeType, layer, i);
                    
                    float xOffset = (i - (nodesInLayer - 1) / 2.0f) * spacing;
                    float yOffset = (totalLayers - 1 -layer) * -1.5f; // Y 위치는 층에 따라 조정
                    node.position = new Vector2(xOffset, yOffset);
                    
                    layerGrid[layer].Add(node);
                    mapNodes.Add(node);
                }
            }
            
            // 4. 노드 간 연결 생성
            for (int layer = 0; layer < totalLayers - 1; layer++)
            {
                ConnectNodesAtLayers(layerGrid[layer], layerGrid[layer + 1]);
            }
            
            // 5. 특별 규칙 적용 - 전투 앞에 캠프 우선 배치
            if (placeCampBeforeBattle)
            {
                EnsureCampBeforeBattle(layerGrid);
            }
            
            // 6. 시작 노드를 현재 노드로 설정
            currentNode = startNode;
            startNode.isAccessible = true;
            startNode.isCurrent = true;
        }
        
        private NodeType DetermineNodeType(int layer)
        {
            // 노드 타입 비율: 전투 > 캠프 > 상점
            float rand = UnityEngine.Random.value;

            if (rand < battleProbability) // 전투
                return NodeType.Battle;
            else if (rand < battleProbability + campProbability) // 캠프
                return NodeType.Camp;
            else
                return NodeType.Shop;
        }
        
        // 노드 생성
        private MapNode CreateNode(NodeType type, int layer, int depth)
        {
            switch (type)
            {
                case NodeType.Battle:
                    break;
                case NodeType.Shop:
                    break;
                case NodeType.Camp:
                    break;
                case NodeType.Boss:
                    break;
            }
            
            MapNode node = new MapNode
            {
                id = nextNodeId++,
                nodeType = type,
                layer = layer,
                depth = depth,
                position = new Vector2(0, 0), // 기본 위치 (나중에 조정)
                sceneName = GetSceneNameForNodeType(type),
            };
            
            return node;
        }
        
        // 노드 타입에 따른 씬 이름 반환
        private string GetSceneNameForNodeType(NodeType type)
        {
            switch (type)
            {
                case NodeType.Battle: return BATTLE_SCENE;
                case NodeType.Shop: return SHOP_SCENE;
                case NodeType.Camp: return CAMP_SCENE;
                case NodeType.Boss: return BOSS_SCENE;
                case NodeType.Start: return START_SCENE;
                default: return BATTLE_SCENE;
            }
        }
        
        // 노드 연결 생성
        private void ConnectNodesAtLayers(List<MapNode> upperNodes, List<MapNode> lowerNodes)
        {
            // 각 노드는 다음 층의 1~2개 노드와 연결
            foreach (MapNode upperNode in upperNodes)
            {
                // 이 노드에서 만들 연결 수 결정 (1~2개)
                int connectionsToMake = UnityEngine.Random.Range(minBranchesPerNode, maxBranchesPerNode + 1);
                connectionsToMake = Mathf.Min(connectionsToMake, lowerNodes.Count); // 연결 가능한 노드 수 제한
                
                // 연결할 노드 선택 - 가까운 노드 우선 (X 위치 기준)
                List<MapNode> potentialConnections = new List<MapNode>(lowerNodes);
                potentialConnections.Sort((a, b) => 
                    Mathf.Abs(a.position.x - upperNode.position.x).CompareTo(Mathf.Abs(b.position.x - upperNode.position.x)));
                
                // 선택된 수만큼 연결
                for (int i = 0; i < connectionsToMake && i < potentialConnections.Count; i++)
                {
                    // 연결 추가
                    upperNode.childNodeIds.Add(potentialConnections[i].id);
                    potentialConnections[i].parentNodeIds.Add(upperNode.id);
                }
            }
            
            // 모든 노드가 최소한 하나의 연결을 가지도록 확인
            foreach (MapNode lowerNode in lowerNodes)
            {
                if (lowerNode.parentNodeIds.Count == 0 && upperNodes.Count > 0)
                {
                    // 가장 가까운 상위 노드 찾기
                    upperNodes.Sort((a, b) => 
                        Mathf.Abs(a.position.x - lowerNode.position.x).CompareTo(Mathf.Abs(b.position.x - lowerNode.position.x)));
                    
                    // 연결 추가
                    upperNodes[0].childNodeIds.Add(lowerNode.id);
                    lowerNode.parentNodeIds.Add(upperNodes[0].id);
                }
            }
        }
        
        // 전투 노드 앞에 캠프 우선 배치
        private void EnsureCampBeforeBattle(List<List<MapNode>> layerGrid)
        {
            // 보스 노드 앞에 캠프 우선 배치 규칙 적용
            for (int layer = 0; layer < totalLayers - 1; layer++)
            {
                foreach (MapNode node in layerGrid[layer])
                {
                    // 전투 노드인 경우 확인
                    if (node.nodeType == NodeType.Boss)
                    {
                        bool hasCampConnection = false;
                        
                        // 이 전투 노드로 연결된 모든 노드 확인
                        foreach (int parentId in node.parentNodeIds)
                        {
                            MapNode parentNode = mapNodes.Find(n => n.id == parentId);
                            if (parentNode != null && parentNode.nodeType == NodeType.Camp)
                            {
                                hasCampConnection = true;
                                break;
                            }
                        }
                        
                        // 캠프가 없고 부모 노드가 있으면 하나를 캠프로 변경
                        if (!hasCampConnection && node.parentNodeIds.Count > 0)
                        {
                            // 부모 노드 중 하나를 무작위로 선택하여 캠프로 변경
                            // 단, 이미 특수 노드(상점, 캠프, 보스)가 아닌 경우에만
                            List<MapNode> eligibleParents = new List<MapNode>();
                            
                            foreach (int parentId in node.parentNodeIds)
                            {
                                MapNode parent = mapNodes.Find(n => n.id == parentId);
                                if (parent != null && 
                                    parent.nodeType != NodeType.Shop && 
                                    parent.nodeType != NodeType.Boss &&
                                    parent.nodeType != NodeType.Camp)
                                {
                                    eligibleParents.Add(parent);
                                }
                            }
                            
                            if (eligibleParents.Count > 0)
                            {
                                MapNode nodeToChange = eligibleParents[UnityEngine.Random.Range(0, eligibleParents.Count)];
                                nodeToChange.nodeType = NodeType.Camp;
                                nodeToChange.sceneName = CAMP_SCENE;
                            }
                        }
                    }
                }
            }
        }
        
        // 디버그용 맵 구조 로그 출력
        private void DebugLogMapStructure()
        {
            Debug.Log($"Generated map with {mapNodes.Count} nodes across {totalLayers} layers");
            
            foreach (var node in mapNodes)
            {
                string connections = string.Join(", ", node.childNodeIds);
                Debug.Log($"Node {node.id} (Type: {node.nodeType}, Layer: {node.layer}, Depth: {node.depth}) " +
                          $"connects to: [{connections}]");
            }
        }
        
        public void SelectNode(int nodeId)
        {
            MapNode selectedNode = mapNodes.Find(node => node.id == nodeId);
            if (selectedNode != null && selectedNode.isAccessible)
            {
                // Update current node
                if (currentNode != null)
                {
                    currentNode.isCurrent = false;
                    currentNode.isCompleted = true;
                }
                
                currentNode = selectedNode;
                currentNode.isCurrent = true;
                
                // Update accessible nodes
                UpdateAccessibleNodes();
                
                // Trigger event
                OnNodeSelected?.Invoke(currentNode);
            }
        }
        
        private void UpdateAccessibleNodes()
        {
            // Reset all nodes to inaccessible
            foreach (var node in mapNodes)
            {
                node.isAccessible = false;
            }
            
            // Mark nodes connected to current node as accessible
            foreach (int nodeId in currentNode.childNodeIds)
            {
                MapNode node = mapNodes.Find(n => n.id == nodeId);
                if (node != null)
                {
                    node.isAccessible = true;
                }
            }
        }
        
        public List<MapNode> GetAllNodes()
        {
            return new List<MapNode>(mapNodes);
        }
    }
}
