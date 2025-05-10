using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System;

namespace MapSystem
{
    public enum NodeType
    {
        Battle,
        Event,
        Shop,
        Camp,
        Elite,
        Boss,
        Start
    }

    [System.Serializable]
    public class NodeTypeDistribution
    {
        public NodeType nodeType;
        [Range(0, 100)]
        public float percentage;
    }

    [System.Serializable]
    public class MapNode
    {
        public int id;
        public NodeType nodeType;
        public Vector2 position;
        public List<int> connectedNodesIds = new List<int>();
        public int layer;
        public int depth;

        // Scene to load when this node is selected
        public string sceneName;
        
        // Difficulty/reward scaling
        public float difficultyMultiplier = 1f;
        public float rewardMultiplier = 1f;
    }

    public class MapController : MonoBehaviour
    {
        [Header("Map Generation Settings")]
        [SerializeField] private int totalLayers = 3;
        [SerializeField] private int minNodesPerLayer = 15;
        [SerializeField] private int maxNodesPerLayer = 20;
        [SerializeField] private int minDepthPerLayer = 10;
        [SerializeField] private int maxDepthPerLayer = 12;
        
        [Header("Node Connection Settings")]
        [SerializeField] private int minBranchesPerNode = 1;
        [SerializeField] private int maxBranchesPerNode = 3;
        [SerializeField] private float branchingProbability = 0.7f;
        
        [Header("Node Type Distribution")]
        [SerializeField] private List<NodeTypeDistribution> nodeTypeDistributions = new List<NodeTypeDistribution>();
        
        [Header("Special Placement Rules")]
        [SerializeField] private int campFrequency = 3; // Camp appears at least every X nodes
        [SerializeField] private bool placeShopOrCampBeforeBoss = true;
        [SerializeField] private int maxConsecutiveBattles = 3;
        
        [Header("Scene Mapping")]
        [SerializeField] private string battleSceneName = "Battle";
        [SerializeField] private string eventSceneName = "Event";
        [SerializeField] private string shopSceneName = "Shop";
        [SerializeField] private string campSceneName = "Camp";
        // [SerializeField] private string eliteSceneName = "Elite";
        // [SerializeField] private string bossSceneName = "Boss";
        
        // The generated map
        private List<MapNode> mapNodes = new List<MapNode>();
        private int nextNodeId = 0;
        
        // Current player position in the map
        private MapNode currentNode;
        
        // Singleton instance
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
        // 지연된 초기화로 다른 컴포넌트가 준비될 시간 확보
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
            
            // Create layers of the map
            for (int layer = 0; layer < totalLayers; layer++)
            {
                GenerateLayer(layer);
            }
            
            // Ensure there's a path from start to end
            EnsurePathFromStartToEnd();
            
            // Apply special placement rules
            ApplySpecialPlacementRules();
            
            // Set the current node to the start node
            currentNode = mapNodes.Find(node => node.nodeType == NodeType.Start);
            
            // Debug log the map structure
            DebugLogMapStructure();
        }
        
        private void GenerateLayer(int layerIndex)
        {
            int safetyCounter = 0;
            int maxIterations = 1000; // 안전한 최대 반복 횟수

            // Determine number of nodes for this layer
            int nodesInLayer = UnityEngine.Random.Range(minNodesPerLayer, maxNodesPerLayer + 1);
            
            // Determine depth (number of steps) for this layer
            int layerDepth = UnityEngine.Random.Range(minDepthPerLayer, maxDepthPerLayer + 1);
            
            // Create a grid to help with node placement
            List<List<MapNode>> depthGrid = new List<List<MapNode>>();
            for (int i = 0; i < layerDepth; i++)
            {
                depthGrid.Add(new List<MapNode>());
            }
            
            // Special case for first and last layer
            if (layerIndex == 0)
            {
                // Add start node at the first depth
                MapNode startNode = CreateNode(NodeType.Start, layerIndex, 0);
                depthGrid[0].Add(startNode);
                mapNodes.Add(startNode);
            }
            
            if (layerIndex == totalLayers - 1)
            {
                // Add boss node at the last depth
                MapNode bossNode = CreateNode(NodeType.Boss, layerIndex, layerDepth - 1);
                depthGrid[layerDepth - 1].Add(bossNode);
                mapNodes.Add(bossNode);
            }
            
            // Distribute remaining nodes across depths
            int remainingNodes = nodesInLayer - (layerIndex == 0 ? 1 : 0) - (layerIndex == totalLayers - 1 ? 1 : 0);
            
            // Ensure we have at least one node at each depth level
            for (int depth = 0; depth < layerDepth; depth++)
            {
                // Skip if we already added a special node (start/boss)
                if ((layerIndex == 0 && depth == 0) || (layerIndex == totalLayers - 1 && depth == layerDepth - 1))
                {
                    continue;
                }
                
                if (depthGrid[depth].Count == 0 && remainingNodes > 0)
                {
                    NodeType nodeType = GetRandomNodeType(layerIndex, depth, layerDepth);
                    MapNode node = CreateNode(nodeType, layerIndex, depth);
                    depthGrid[depth].Add(node);
                    mapNodes.Add(node);
                    remainingNodes--;
                }
            }
            
            // Distribute remaining nodes randomly but with more weight to middle depths
            while (remainingNodes > 0 && safetyCounter < maxIterations)
            {
                safetyCounter++;

                // Favor middle depths for more nodes
                int depth = GetWeightedRandomDepth(layerDepth);
                
                // Skip if this is a special node position
                if ((layerIndex == 0 && depth == 0) || (layerIndex == totalLayers - 1 && depth == layerDepth - 1))
                {
                    continue;
                }
                
                NodeType nodeType = GetRandomNodeType(layerIndex, depth, layerDepth);
                MapNode node = CreateNode(nodeType, layerIndex, depth);
                depthGrid[depth].Add(node);
                mapNodes.Add(node);
                remainingNodes--;
            }
            
            // Connect nodes between depths
            for (int depth = 0; depth < layerDepth - 1; depth++)
            {
                ConnectNodesAtDepths(depthGrid[depth], depthGrid[depth + 1]);
            }
        }
        
        private MapNode CreateNode(NodeType type, int layer, int depth)
        {
            MapNode node = new MapNode
            {
                id = nextNodeId++,
                nodeType = type,
                layer = layer,
                depth = depth,
                position = new Vector2(UnityEngine.Random.Range(-5f, 5f), depth * -1.5f), // Simple positioning
                sceneName = GetSceneNameForNodeType(type),
                difficultyMultiplier = 1f + (layer * 0.2f) + (depth * 0.05f), // Increase difficulty with depth and layer
                rewardMultiplier = 1f + (layer * 0.2f) + (depth * 0.05f) // Same for rewards
            };
            
            return node;
        }
        
        private string GetSceneNameForNodeType(NodeType type)
        {
            switch (type)
            {
                case NodeType.Battle: return battleSceneName;
                case NodeType.Event: return eventSceneName;
                case NodeType.Shop: return shopSceneName;
                case NodeType.Camp: return campSceneName;
                // case NodeType.Elite: return eliteSceneName;
                // case NodeType.Boss: return bossSceneName;
                case NodeType.Start: return battleSceneName; // Start node typically leads to a battle
                default: return battleSceneName;
            }
        }
        
        private NodeType GetRandomNodeType(int layer, int depth, int maxDepth)
        {
            // Special case for last node in last layer
            if (layer == totalLayers - 1 && depth == maxDepth - 1)
            {
                return NodeType.Boss;
            }
            
            // Special case for first node in first layer
            if (layer == 0 && depth == 0)
            {
                return NodeType.Start;
            }
            
            // Higher chance of elite enemies in later depths
            float eliteChance = Mathf.Lerp(0.05f, 0.3f, (float)depth / maxDepth);
            if (UnityEngine.Random.value < eliteChance)
            {
                return NodeType.Elite;
            }
            
            // Use the configured distribution for other node types
            float rand = UnityEngine.Random.value * 100f;
            float cumulativePercentage = 0f;
            
            foreach (var distribution in nodeTypeDistributions)
            {
                // Skip special node types that are placed by rules
                if (distribution.nodeType == NodeType.Boss || 
                    distribution.nodeType == NodeType.Start || 
                    distribution.nodeType == NodeType.Elite)
                {
                    continue;
                }
                
                cumulativePercentage += distribution.percentage;
                if (rand <= cumulativePercentage)
                {
                    return distribution.nodeType;
                }
            }
            
            // Default to battle if something goes wrong
            return NodeType.Battle;
        }
        
        private int GetWeightedRandomDepth(int maxDepth)
        {
            // Create a bell curve distribution favoring middle depths
            float[] weights = new float[maxDepth];
            float totalWeight = 0f;
            
            for (int i = 0; i < maxDepth; i++)
            {
                // Distance from middle (0 to 1)
                float distFromMiddle = Mathf.Abs(i - (maxDepth - 1) / 2f) / (maxDepth / 2f);
                weights[i] = 1f - (distFromMiddle * 0.8f); // Higher weight for middle depths
                totalWeight += weights[i];
            }
            
            // Choose a depth based on weights
            float random = UnityEngine.Random.value * totalWeight;
            float cumulativeWeight = 0f;
            
            for (int i = 0; i < maxDepth; i++)
            {
                cumulativeWeight += weights[i];
                if (random <= cumulativeWeight)
                {
                    return i;
                }
            }
            
            return maxDepth / 2; // Fallback to middle
        }
        
        private void ConnectNodesAtDepths(List<MapNode> sourceNodes, List<MapNode> targetNodes)
        {
            if (sourceNodes.Count == 0 || targetNodes.Count == 0)
            {
                return;
            }
            
            // Ensure every node has at least one connection
            foreach (var sourceNode in sourceNodes)
            {
                // Determine number of branches for this node
                int branches = UnityEngine.Random.Range(minBranchesPerNode, maxBranchesPerNode + 1);
                branches = Mathf.Min(branches, targetNodes.Count); // Can't have more branches than target nodes
                
                // Create a list of possible target nodes
                List<MapNode> possibleTargets = new List<MapNode>(targetNodes);
                
                // Connect to random targets
                for (int i = 0; i < branches; i++)
                {
                    if (possibleTargets.Count == 0) break;
                    
                    // Select a random target
                    int targetIndex = UnityEngine.Random.Range(0, possibleTargets.Count);
                    MapNode targetNode = possibleTargets[targetIndex];
                    
                    // Connect nodes
                    sourceNode.connectedNodesIds.Add(targetNode.id);
                    
                    // Remove target from possible targets to avoid duplicate connections
                    possibleTargets.RemoveAt(targetIndex);
                }
            }
            
            // Ensure every target node has at least one incoming connection
            foreach (var targetNode in targetNodes)
            {
                bool hasIncomingConnection = sourceNodes.Any(sourceNode => 
                    sourceNode.connectedNodesIds.Contains(targetNode.id));
                
                if (!hasIncomingConnection && sourceNodes.Count > 0)
                {
                    // Connect a random source node to this target
                    int sourceIndex = UnityEngine.Random.Range(0, sourceNodes.Count);
                    sourceNodes[sourceIndex].connectedNodesIds.Add(targetNode.id);
                }
            }
        }
        
        private void EnsurePathFromStartToEnd()
        {
            // Find start and end nodes
            MapNode startNode = mapNodes.Find(node => node.nodeType == NodeType.Start);
            MapNode endNode = mapNodes.Find(node => node.nodeType == NodeType.Boss);
            
            if (startNode == null || endNode == null)
            {
                Debug.LogError("Start or end node not found!");
                return;
            }
            
            // Check if there's a path from start to end
            if (!PathExistsBetweenNodes(startNode.id, endNode.id))
            {
                // Create a path by connecting nodes at each depth
                MapNode currentNode = startNode;
                
                while (currentNode.id != endNode.id)
                {
                    // Find nodes at the next depth
                    List<MapNode> nextDepthNodes = mapNodes.FindAll(node => 
                        node.layer == currentNode.layer && node.depth == currentNode.depth + 1);
                    
                    // If no nodes at next depth in same layer, look for nodes in next layer
                    if (nextDepthNodes.Count == 0 && currentNode.layer < totalLayers - 1)
                    {
                        nextDepthNodes = mapNodes.FindAll(node => 
                            node.layer == currentNode.layer + 1 && node.depth == 0);
                    }
                    
                    if (nextDepthNodes.Count == 0)
                    {
                        Debug.LogError("Cannot create path to end node!");
                        break;
                    }
                    
                    // Find the closest node to the end node
                    MapNode nextNode = nextDepthNodes.OrderBy(node => 
                        Vector2.Distance(node.position, endNode.position)).First();
                    
                    // Connect current node to next node
                    if (!currentNode.connectedNodesIds.Contains(nextNode.id))
                    {
                        currentNode.connectedNodesIds.Add(nextNode.id);
                    }
                    
                    currentNode = nextNode;
                }
            }
        }
        
        private bool PathExistsBetweenNodes(int startId, int endId)
        {
            // Simple BFS to check if a path exists
            HashSet<int> visited = new HashSet<int>();
            Queue<int> queue = new Queue<int>();
            
            queue.Enqueue(startId);
            visited.Add(startId);
            
            while (queue.Count > 0)
            {
                int currentId = queue.Dequeue();
                
                if (currentId == endId)
                {
                    return true;
                }
                
                MapNode currentNode = mapNodes.Find(node => node.id == currentId);
                if (currentNode != null)
                {
                    foreach (int connectedId in currentNode.connectedNodesIds)
                    {
                        if (!visited.Contains(connectedId))
                        {
                            visited.Add(connectedId);
                            queue.Enqueue(connectedId);
                        }
                    }
                }
            }
            
            return false;
        }
        
        private void ApplySpecialPlacementRules()
        {
            // Ensure camp nodes appear regularly
            EnsureRegularCampNodes();
            
            // Ensure shop or camp before boss
            EnsureShopOrCampBeforeBoss();
            
            // Prevent too many consecutive battles
            PreventConsecutiveBattles();
            
            // Ensure proper difficulty progression
            AdjustDifficultyProgression();
        }
        
        private void EnsureRegularCampNodes()
        {
            // Group nodes by layer
            for (int layer = 0; layer < totalLayers; layer++)
            {
                List<MapNode> layerNodes = mapNodes.FindAll(node => node.layer == layer);
                
                // Sort by depth
                layerNodes.Sort((a, b) => a.depth.CompareTo(b.depth));
                
                // Check for camp frequency
                int nodesSinceCamp = 0;
                
                for (int i = 0; i < layerNodes.Count; i++)
                {
                    if (layerNodes[i].nodeType == NodeType.Camp)
                    {
                        nodesSinceCamp = 0;
                    }
                    else
                    {
                        nodesSinceCamp++;
                        
                        // If we've gone too long without a camp and this isn't a special node
                        if (nodesSinceCamp >= campFrequency && 
                            layerNodes[i].nodeType != NodeType.Boss && 
                            layerNodes[i].nodeType != NodeType.Start)
                        {
                            layerNodes[i].nodeType = NodeType.Camp;
                            layerNodes[i].sceneName = campSceneName;
                            nodesSinceCamp = 0;
                        }
                    }
                }
            }
        }
        
        private void EnsureShopOrCampBeforeBoss()
        {
            if (!placeShopOrCampBeforeBoss) return;
            
            MapNode bossNode = mapNodes.Find(node => node.nodeType == NodeType.Boss);
            if (bossNode == null) return;
            
            // Find all nodes that connect directly to the boss
            List<MapNode> nodesThatConnectToBoss = mapNodes.FindAll(node => 
                node.connectedNodesIds.Contains(bossNode.id));
            
            // Ensure at least one is a shop or camp
            bool hasShopOrCamp = nodesThatConnectToBoss.Any(node => 
                node.nodeType == NodeType.Shop || node.nodeType == NodeType.Camp);
            
            if (!hasShopOrCamp && nodesThatConnectToBoss.Count > 0)
            {
                // Convert one random node to shop or camp
                MapNode nodeToConvert = nodesThatConnectToBoss[UnityEngine.Random.Range(0, nodesThatConnectToBoss.Count)];
                nodeToConvert.nodeType = UnityEngine.Random.value < 0.5f ? NodeType.Shop : NodeType.Camp;
                nodeToConvert.sceneName = nodeToConvert.nodeType == NodeType.Shop ? shopSceneName : campSceneName;
            }
        }
        
        private void PreventConsecutiveBattles()
        {
            // Group nodes by layer
            for (int layer = 0; layer < totalLayers; layer++)
            {
                List<MapNode> layerNodes = mapNodes.FindAll(node => node.layer == layer);
                
                // Sort by depth
                layerNodes.Sort((a, b) => a.depth.CompareTo(b.depth));
                
                // Check for consecutive battles
                int consecutiveBattles = 0;
                
                for (int i = 0; i < layerNodes.Count; i++)
                {
                    if (layerNodes[i].nodeType == NodeType.Battle || layerNodes[i].nodeType == NodeType.Elite)
                    {
                        consecutiveBattles++;
                        
                        // If too many consecutive battles
                        if (consecutiveBattles > maxConsecutiveBattles && i < layerNodes.Count - 1)
                        {
                            // Convert next battle to an event or shop
                            for (int j = i + 1; j < layerNodes.Count; j++)
                            {
                                if (layerNodes[j].nodeType == NodeType.Battle && 
                                    layerNodes[j].nodeType != NodeType.Boss && 
                                    layerNodes[j].nodeType != NodeType.Start)
                                {
                                    layerNodes[j].nodeType = UnityEngine.Random.value < 0.7f ? NodeType.Event : NodeType.Shop;
                                    layerNodes[j].sceneName = layerNodes[j].nodeType == NodeType.Event ? eventSceneName : shopSceneName;
                                    consecutiveBattles = 0;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        consecutiveBattles = 0;
                    }
                }
            }
        }
        
        private void AdjustDifficultyProgression()
        {
            // Adjust difficulty based on depth and layer
            foreach (var node in mapNodes)
            {
                // Base difficulty increases with layer and depth
                float baseDifficulty = 1f + (node.layer * 0.3f) + (node.depth * 0.1f);
                
                // Additional difficulty for elite and boss nodes
                if (node.nodeType == NodeType.Elite)
                {
                    baseDifficulty *= 1.5f;
                }
                else if (node.nodeType == NodeType.Boss)
                {
                    baseDifficulty *= 2f;
                }
                
                // Add some randomness
                float randomVariance = UnityEngine.Random.Range(-0.1f, 0.1f);
                
                node.difficultyMultiplier = baseDifficulty + randomVariance;
                
                // Rewards scale with difficulty
                node.rewardMultiplier = node.difficultyMultiplier;
            }
        }
        
        public void TravelToNode(int nodeId)
        {
            MapNode targetNode = mapNodes.Find(node => node.id == nodeId);
            
            if (targetNode == null)
            {
                Debug.LogError("Target node not found!");
                return;
            }
            
            // Check if this node is accessible from current node
            if (currentNode != null && !currentNode.connectedNodesIds.Contains(nodeId))
            {
                Debug.LogError("Cannot travel to this node - not connected!");
                return;
            }
            
            // Set as current node
            currentNode = targetNode;
            
            // Load the appropriate scene
            SceneManager.LoadScene(targetNode.sceneName);
        }
        
        public List<MapNode> GetAccessibleNodes()
        {
            if (currentNode == null) return new List<MapNode>();
            
            List<MapNode> accessibleNodes = new List<MapNode>();
            
            foreach (int nodeId in currentNode.connectedNodesIds)
            {
                MapNode node = mapNodes.Find(n => n.id == nodeId);
                if (node != null)
                {
                    accessibleNodes.Add(node);
                }
            }
            
            return accessibleNodes;
        }
        
        public MapNode GetCurrentNode()
        {
            return currentNode;
        }
        
        public List<MapNode> GetAllNodes()
        {
            return new List<MapNode>(mapNodes);
        }
        
        private void DebugLogMapStructure()
        {
            Debug.Log($"Generated map with {mapNodes.Count} nodes across {totalLayers} layers");
            
            foreach (var node in mapNodes)
            {
                string connections = string.Join(", ", node.connectedNodesIds);
                Debug.Log($"Node {node.id} (Type: {node.nodeType}, Layer: {node.layer}, Depth: {node.depth}) " +
                          $"connects to: [{connections}]");
            }
        }
    }
}
