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
        Event,
        Shop,
        Camp,
        Elite,
        Boss
    }
    
    [System.Serializable]
    public class NodeTypeDistribution
    {
        [Tooltip("노드 타입")]
        public NodeType nodeType;
        
        [Tooltip("생성 확률 (%)")]
        [Range(0, 100)]
        public float percentage;
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
        
        [Header("Node Type Distribution")]
        [Tooltip("노드 타입별 생성 확률 (사용하지 않음 - 코드에서 직접 설정)")]
        [SerializeField] private List<NodeTypeDistribution> nodeTypeDistributions = new List<NodeTypeDistribution>();
        
        [Header("Special Placement Rules")]
        [Tooltip("보스 전층에 캠프나 상점 포함 여부")]
        [SerializeField] private bool placeShopOrCampBeforeBoss = true; // 보스 전층은 캠프/상점 포함
        
        [Tooltip("전투 노드 앞에 캠프 우선 배치 여부")]
        [SerializeField] private bool placeCampBeforeBattle = true; // 전투 앞에 캠프 우선 배치
        
        [Tooltip("상점이 배치되는 초반 층 수 (1부터 시작)")]
        [SerializeField] private int shopEarlyLayerLimit = 3; // 상점은 초반 2~3층에 배치
        
        // 생성된 맵 데이터
        private List<MapNode> mapNodes = new List<MapNode>();
        private int nextNodeId = 0;
        private MapNode currentNode;
        
        // 이벤트
        public delegate void MapNodeSelectedHandler(MapNode node);
        public event MapNodeSelectedHandler OnNodeSelected;
        
        // 씬 이름
        private const string BATTLE_SCENE = "Battle";
        private const string EVENT_SCENE = "Event";
        private const string SHOP_SCENE = "Shop";
        private const string CAMP_SCENE = "Camp";
        private const string ELITE_SCENE = "Battle"; // 엘리트는 일반 전투 씬 사용
        private const string BOSS_SCENE = "Boss";
        private const string START_SCENE = "Battle"; // 시작은 일반 전투 씬 사용
        public static MapController Instance { get; private set; }
        
        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        private void Start()
        {
            Invoke("DelayedStart", 0.1f);
        }

        private void DelayedStart()
        {
            GenerateMap();
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
            MapNode startNode = CreateNode(NodeType.Start, totalLayers - 1, 0);
            startNode.position = new Vector2(0, (totalLayers - 1) * -1.5f); // 중앙 하단에 배치
            layerGrid[totalLayers - 1].Add(startNode);
            mapNodes.Add(startNode);
            
            // 2. 맨 위(끝)에 Boss 노드 추가
            MapNode bossNode = CreateNode(NodeType.Boss, 0, 0);
            bossNode.position = new Vector2(0, 0); // 중앙 상단에 배치
            layerGrid[0].Add(bossNode);
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
                        nodeType = NodeType.Event; // 상점 대신 이벤트로 변경
                    }
                    
                    MapNode node = CreateNode(nodeType, layer, i);
                    
                    // X 위치 분포 설정
                    float xOffset = (i - (nodesInLayer - 1) / 2.0f) * spacing;
                    node.position = new Vector2(xOffset, layer * -1.5f);
                    
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
            
            // 디버그 로그
            DebugLogMapStructure();
        }
        
        // 노드 타입 결정 (비율에 따라)
        private NodeType DetermineNodeType(int layer)
        {
            // 노드 타입 비율: 전투 > 이벤트 > 캠프 > 상점 > 엘리트
            float rand = UnityEngine.Random.value;
            
            if (rand < 0.45f) // 45% - 전투
                return NodeType.Battle;
            else if (rand < 0.70f) // 25% - 이벤트
                return NodeType.Event;
            else if (rand < 0.85f) // 15% - 캠프
                return NodeType.Camp;
            else if (rand < 0.95f) // 10% - 상점
                return NodeType.Shop;
            else // 5% - 엘리트
                return NodeType.Elite;
        }
        
        // 노드 생성
        private MapNode CreateNode(NodeType type, int layer, int depth)
        {
            // 난이도와 보상 조정 - 경로에 따른 의미 부여
            float diffMult = 1f + (depth * 0.1f); // 기본 난이도
            float rewardMult = 1f + (depth * 0.1f); // 기본 보상
            
            // 노드 타입에 따른 난이도/보상 조정
            switch (type)
            {
                case NodeType.Battle:
                    // 기본 전투 - 표준 난이도와 보상
                    break;
                case NodeType.Event:
                    // 이벤트 - 변동성 있음 (위험할 수도, 이득일 수도)
                    diffMult *= UnityEngine.Random.Range(0.8f, 1.2f);
                    rewardMult *= UnityEngine.Random.Range(0.8f, 1.5f);
                    break;
                case NodeType.Shop:
                    // 상점 - 낮은 난이도, 자원 소모
                    diffMult *= 0.5f;
                    rewardMult *= 0.5f; // 상점은 보상이 아닌 자원 소모
                    break;
                case NodeType.Camp:
                    // 캠프 - 매우 낮은 난이도, 회복 효과
                    diffMult *= 0.2f;
                    rewardMult *= 0.3f;
                    break;
                case NodeType.Elite:
                    // 엘리트 - 높은 난이도, 높은 보상
                    diffMult *= 1.5f;
                    rewardMult *= 1.8f;
                    break;
                case NodeType.Boss:
                    // 보스 - 매우 높은 난이도, 매우 높은 보상
                    diffMult *= 2.0f;
                    rewardMult *= 2.5f;
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
                difficultyMultiplier = diffMult,
                rewardMultiplier = rewardMult
            };
            
            return node;
        }
        
        // 노드 타입에 따른 씬 이름 반환
        private string GetSceneNameForNodeType(NodeType type)
        {
            switch (type)
            {
                case NodeType.Battle: return BATTLE_SCENE;
                case NodeType.Event: return EVENT_SCENE;
                case NodeType.Shop: return SHOP_SCENE;
                case NodeType.Camp: return CAMP_SCENE;
                case NodeType.Elite: return ELITE_SCENE;
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
            // 전투 노드 앞에 캠프 우선 배치 규칙 적용
            for (int layer = 0; layer < totalLayers - 1; layer++)
            {
                foreach (MapNode node in layerGrid[layer])
                {
                    // 전투 노드인 경우 확인
                    if (node.nodeType == NodeType.Battle)
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
                            // 단, 이미 특수 노드(상점, 엘리트, 보스)가 아닌 경우에만
                            List<MapNode> eligibleParents = new List<MapNode>();
                            
                            foreach (int parentId in node.parentNodeIds)
                            {
                                MapNode parent = mapNodes.Find(n => n.id == parentId);
                                if (parent != null && 
                                    parent.nodeType != NodeType.Shop && 
                                    parent.nodeType != NodeType.Elite && 
                                    parent.nodeType != NodeType.Boss)
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
