using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

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
        private const string BATTLE_SCENE_1 = "Battle-Cave";
        private const string BATTLE_SCENE_2 = "Battle-Dungeon"; // 새로운 배틀 씬
        private const string SHOP_SCENE = "Shop";
        private const string CAMP_SCENE = "Camp";
        private const string BOSS_SCENE = "Boss";
        private const string START_SCENE = "Battle";
        
        // AccountDataManager 키
        private const string LAST_BATTLE_SCENE_KEY = "LastBattleScene";
        
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
            Debug.Log("MapController initialized");

            // 씬 로드 이벤트를 통해 노드 갱신 시도
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            // 이벤트 해제
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (PlayerPrefs.HasKey("SelectedNodeId"))
            {
                int selectedNodeId = PlayerPrefs.GetInt("SelectedNodeId");
                SelectNode(selectedNodeId);
                Debug.Log($"{selectedNodeId} 노드로 이동되었습니다.");
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
            
            // 3. 중간 층에 노드 분포 (레이어 내 노드 타입 유일성 보장 + Camp/Shop 연속 금지)
            for (int layer = 1; layer < totalLayers - 1; layer++)
            {
                // 각 층에 몇 개의 노드를 생성할지 결정
                int requestedNodesInLayer = UnityEngine.Random.Range(minNodesPerLayer, maxNodesPerLayer + 1);

                // 이 레이어에서 사용할 수 있는 타입 목록 구성 (Start/Boss 제외)
                List<NodeType> availableTypes = new List<NodeType> { NodeType.Battle, NodeType.Camp, NodeType.Shop };

                // 상점은 초반 레이어 우선: 너무 늦은 레이어라면 후보에서 제거 (유일성 보장을 위해)
                if (layer > totalLayers - shopEarlyLayerLimit)
                {
                    availableTypes.Remove(NodeType.Shop);
                }

                // 바로 이전 레이어에 등장한 Camp/Shop은 이번 레이어에 반복 금지
                if (layer - 1 >= 0)
                {
                    var prevLayerTypes = new HashSet<NodeType>(layerGrid[layer - 1].Select(n => n.nodeType));
                    if (prevLayerTypes.Contains(NodeType.Camp))
                    {
                        availableTypes.Remove(NodeType.Camp);
                    }
                    if (prevLayerTypes.Contains(NodeType.Shop))
                    {
                        availableTypes.Remove(NodeType.Shop);
                    }
                }

                // 유일성 보장을 위해 생성 가능한 최대 노드 수는 사용 가능한 타입 수로 제한
                int maxUniqueForLayer = Mathf.Max(0, availableTypes.Count);
                int nodesInLayer = Mathf.Min(requestedNodesInLayer, maxUniqueForLayer);

                if (nodesInLayer < requestedNodesInLayer)
                {
                    Debug.Log($"[MapGen] Layer {layer}: 요청된 노드 수 {requestedNodesInLayer} 중 {nodesInLayer}개만 생성 (레이어 내 타입 유일성 제한)");
                }

                // 보스 전층에는 캠프/상점 포함 규칙 적용: 해당 타입이 후보에 없다면 캠프는 강제로 포함
                bool isPreBossLayer = (layer == totalLayers - 2);
                List<NodeType> selectedTypes = new List<NodeType>();

                if (nodesInLayer > 0)
                {
                    // 먼저 무작위로 타입 셔플
                    availableTypes = availableTypes.OrderBy(_ => UnityEngine.Random.value).ToList();

                    if (isPreBossLayer && placeShopOrCampBeforeBoss)
                    {
                        // 가능한 경우 상점/캠프 중 하나를 우선 선택
                        List<NodeType> preferred = new List<NodeType>();
                        if (availableTypes.Contains(NodeType.Shop)) preferred.Add(NodeType.Shop);
                        if (availableTypes.Contains(NodeType.Camp)) preferred.Add(NodeType.Camp);

                        if (preferred.Count > 0)
                        {
                            NodeType forced = preferred[UnityEngine.Random.Range(0, preferred.Count)];
                            selectedTypes.Add(forced);
                            availableTypes.Remove(forced);
                        }
                        else
                        {
                            // 이전 레이어에 Camp/Shop이 모두 등장해 필터링된 경우, 강제 포함을 생략하고 로그로 안내
                            Debug.Log($"[MapGen] Pre-Boss 레이어 {layer}: 이전 레이어와의 연속 금지 규칙으로 인해 Camp/Shop 강제 포함을 생략합니다.");
                        }
                    }

                    // 남은 타입에서 부족분 채우기
                    int remaining = Mathf.Max(0, nodesInLayer - selectedTypes.Count);
                    for (int i = 0; i < remaining && i < availableTypes.Count; i++)
                    {
                        selectedTypes.Add(availableTypes[i]);
                    }
                }

                // 노드 간 간격 설정
                float spacing = 3.0f;

                // 선택된 타입들로 노드 생성 (depth는 0부터 순서대로)
                for (int i = 0; i < selectedTypes.Count; i++)
                {
                    NodeType nodeType = selectedTypes[i];

                    MapNode node = CreateNode(nodeType, layer, i);

                    float xOffset = (i - (selectedTypes.Count - 1) / 2.0f) * spacing;
                    float yOffset = (totalLayers - 1 - layer) * -1.5f; // Y 위치는 층에 따라 조정
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

            // 7. 레이어별 타입 유일성 검증 (디버그)
            ValidatePerLayerUniqueness();

            // 8. 인접 레이어 간 Camp/Shop 연속 금지 검증 (디버그)
            ValidateNoConsecutiveCampShop();
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
                case NodeType.Battle: return GetNextBattleSceneName();
                case NodeType.Shop: return SHOP_SCENE;
                case NodeType.Camp: return CAMP_SCENE;
                case NodeType.Boss: return BOSS_SCENE;
                case NodeType.Start: return GetInitialBattleSceneName();
                default: return GetNextBattleSceneName();
            }
        }
        
        // 처음 시작할 때 랜덤으로 배틀 씬 선택 (카운터 업데이트 없이 이름만 반환)
        private string GetInitialBattleSceneName()
        {
            // AccountDataManager에서 마지막 배틀 씬 확인
            string lastScene = AccountDataManager.GetString(LAST_BATTLE_SCENE_KEY, "");
            
            // 처음이거나 값이 없으면 랜덤 선택
            if (string.IsNullOrEmpty(lastScene))
            {
                bool pickFirst = Random.value < 0.5f;
                string selectedScene = pickFirst ? BATTLE_SCENE_1 : BATTLE_SCENE_2;
                Debug.Log($"Initial battle scene selected randomly: {selectedScene}");
                return selectedScene;
            }
            
            // 이미 기록이 있으면 번갈아가는 씬 이름만 반환 (업데이트는 하지 않음)
            return GetNextBattleSceneName();
        }
        
        // 다음 배틀 씬 이름만 반환 (카운터 업데이트는 Portal에서 수행)
        private string GetNextBattleSceneName()
        {
            // 마지막으로 플레이한 배틀 씬 가져오기
            string lastScene = AccountDataManager.GetString(LAST_BATTLE_SCENE_KEY, "");
            
            // 처음이거나 값이 없으면 랜덤 선택
            if (string.IsNullOrEmpty(lastScene))
            {
                bool pickFirst = Random.value < 0.5f;
                string selectedScene = pickFirst ? BATTLE_SCENE_1 : BATTLE_SCENE_2;
                Debug.Log($"First battle scene name: {selectedScene} (will be saved when portal entered)");
                return selectedScene;
            }
            
            // 이전과 다른 씬 이름 반환 (업데이트는 Portal에서)
            string nextScene = (lastScene == BATTLE_SCENE_1) ? BATTLE_SCENE_2 : BATTLE_SCENE_1;
            Debug.Log($"Next battle scene name: {nextScene} (previous was {lastScene}, will be saved when portal entered)");
            return nextScene;
        }
        
        // 배틀 씬 카운터 업데이트 (Portal에서 호출)
        public static void UpdateBattleSceneCounter(string sceneName)
        {
            AccountDataManager.SetString(LAST_BATTLE_SCENE_KEY, sceneName);
            Debug.Log($"Battle scene counter updated to: {sceneName}");
        }
        
        // Portal 진입 시 실시간으로 다음 배틀 씬 결정 및 카운터 업데이트
        public static string GetNextBattleSceneAndUpdate()
        {
            // 마지막으로 플레이한 배틀 씬 가져오기
            string lastScene = AccountDataManager.GetString(LAST_BATTLE_SCENE_KEY, "");
            
            string nextScene;
            
            // 처음이거나 값이 없으면 랜덤 선택
            if (string.IsNullOrEmpty(lastScene))
            {
                bool pickFirst = Random.value < 0.5f;
                nextScene = pickFirst ? BATTLE_SCENE_1 : BATTLE_SCENE_2;
                Debug.Log($"First battle scene (random): {nextScene}");
            }
            else
            {
                // 이전과 다른 씬 선택 (번갈아가며)
                nextScene = (lastScene == BATTLE_SCENE_1) ? BATTLE_SCENE_2 : BATTLE_SCENE_1;
                Debug.Log($"Next battle scene (alternating): {nextScene} (previous was {lastScene})");
            }
            
            // 카운터 업데이트
            AccountDataManager.SetString(LAST_BATTLE_SCENE_KEY, nextScene);
            
            return nextScene;
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

        // 레이어별로 노드 타입이 유일한지 확인 (Start/Boss 제외)
        private void ValidatePerLayerUniqueness()
        {
            var grouped = mapNodes
                .Where(n => n.nodeType != NodeType.Start && n.nodeType != NodeType.Boss)
                .GroupBy(n => n.layer);

            foreach (var layerGroup in grouped)
            {
                var types = layerGroup.Select(n => n.nodeType).ToList();
                var distinctTypes = types.Distinct().ToList();
                if (types.Count != distinctTypes.Count)
                {
                    Debug.LogWarning($"[MapGen] 레이어 {layerGroup.Key}에서 노드 타입 중복 감지: {string.Join(", ", types)}");
                }
                else
                {
                    Debug.Log($"[MapGen] 레이어 {layerGroup.Key} 타입 유일성 OK: {string.Join(", ", distinctTypes)}");
                }
            }
        }

        // 인접 레이어 간 Camp/Shop 연속 배치를 방지하는 검증
        private void ValidateNoConsecutiveCampShop()
        {
            // 레이어별 타입 집합 구성
            var byLayer = mapNodes
                .GroupBy(n => n.layer)
                .ToDictionary(g => g.Key, g => new HashSet<NodeType>(g.Select(n => n.nodeType)));

            for (int layer = 1; layer < totalLayers - 1; layer++)
            {
                if (!byLayer.ContainsKey(layer) || !byLayer.ContainsKey(layer - 1)) continue;

                bool prevHadCamp = byLayer[layer - 1].Contains(NodeType.Camp);
                bool prevHadShop = byLayer[layer - 1].Contains(NodeType.Shop);
                bool currHasCamp = byLayer[layer].Contains(NodeType.Camp);
                bool currHasShop = byLayer[layer].Contains(NodeType.Shop);

                if (prevHadCamp && currHasCamp)
                {
                    Debug.LogWarning($"[MapGen] 레이어 {layer-1}와 {layer}에 Camp가 연속 배치되었습니다.");
                }
                if (prevHadShop && currHasShop)
                {
                    Debug.LogWarning($"[MapGen] 레이어 {layer-1}와 {layer}에 Shop이 연속 배치되었습니다.");
                }
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
        
        /// <summary>
        /// 맵을 완전히 초기화하고 새로 생성 (보스 처치 후 새 스테이지 시작 시 사용)
        /// </summary>
        public void ResetAndRegenerateMap()
        {
            Debug.Log("[MapController] 맵 초기화 및 재생성 시작...");
            
            // 기존 맵 데이터 초기화
            mapNodes.Clear();
            nextNodeId = 0;
            currentNode = null;
            
            // 새 맵 생성
            GenerateMap();
            
            // 시작 노드 선택
            MapNode startNode = mapNodes.Find(node => node.nodeType == NodeType.Start);
            if (startNode != null)
            {
                SelectNode(startNode.id);
                Debug.Log("[MapController] 새 맵 생성 완료! 시작 노드로 초기화됨");
            }
            
            // 포탈 재생성 이벤트 발생 (PortalGenerator가 OnNodeSelected 이벤트를 구독 중)
            OnNodeSelected?.Invoke(currentNode);
        }
    }
}
