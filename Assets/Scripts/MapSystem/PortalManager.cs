using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MapSystem
{
    // 포탈 생성 및 관리를 담당하는 매니저
    public class PortalManager : MonoBehaviour
    {
        [Header("Portal Settings")]
        [SerializeField] private GameObject portalPrefab;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        
        // 싱글톤 인스턴스
        public static PortalManager Instance { get; private set; }
        
        // 참조
        private MapController mapController;
        private MapUIManager mapUIManager;
        
        // 현재 씬에 생성된 포탈 목록
        private List<GameObject> activePortals = new List<GameObject>();
        
        private void Awake()
        {
            // 싱글톤 패턴
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 씬 로드 이벤트 등록
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        private void Start()
        {
            // 맵 컨트롤러 및 UI 매니저 참조 가져오기
            mapController = MapController.Instance;
            mapUIManager = MapUIManager.Instance;
            
            if (mapController == null)
            {
                LogDebug("MapController를 찾을 수 없습니다!");
            }
            
            if (mapUIManager == null)
            {
                LogDebug("MapUIManager를 찾을 수 없습니다!");
            }
        }
        
        // 씬 로드 이벤트 핸들러
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            LogDebug($"씬 로드됨: {scene.name}");
            
            // 이전 포탈 정리
            ClearAllPortals();
            
            // 약간의 지연 후 포탈 생성 (씬 로드 완료 후)
            StartCoroutine(SpawnPortalsAfterDelay(0.5f));
        }
        
        // 지연 후 포탈 생성
        private IEnumerator SpawnPortalsAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            // 현재 씬에서 접근 가능한 노드에 대한 포탈 생성
            SpawnPortalsForAccessibleNodes();
        }
        
        // 접근 가능한 노드에 대한 포탈 생성
        private void SpawnPortalsForAccessibleNodes()
        {
            if (mapController == null) return;
            
            // 모든 노드 가져오기
            List<MapNode> allNodes = mapController.GetAllNodes();
            if (allNodes == null || allNodes.Count == 0)
            {
                LogDebug("생성된 노드가 없습니다!");
                return;
            }
            
            // 현재 씬의 모든 포탈 스폰 포인트 찾기
            PortalSpawnPoint[] spawnPoints = FindObjectsOfType<PortalSpawnPoint>();
            if (spawnPoints.Length == 0)
            {
                LogDebug("현재 씬에 포탈 스폰 포인트가 없습니다!");
                return;
            }
            
            LogDebug($"스폰 포인트 {spawnPoints.Length}개 발견, 노드 {allNodes.Count}개 중 접근 가능한 노드에 대한 포탈 생성");
            
            // 접근 가능한 노드에 대해 포탈 생성
            int portalCount = 0;
            foreach (MapNode node in allNodes)
            {
                if (node.isAccessible)
                {
                    // 적합한 스폰 포인트 찾기
                    PortalSpawnPoint spawnPoint = FindSuitableSpawnPoint(spawnPoints, node);
                    
                    if (spawnPoint != null)
                    {
                        // 포탈 생성
                        GameObject portal = spawnPoint.SpawnPortal(portalPrefab, node, mapUIManager);
                        
                        if (portal != null)
                        {
                            activePortals.Add(portal);
                            portalCount++;
                            LogDebug($"포탈 생성됨: {node.nodeType} (ID: {node.id})");
                        }
                    }
                    else
                    {
                        LogDebug($"노드 {node.id} ({node.nodeType})에 대한 적합한 스폰 포인트를 찾을 수 없습니다!");
                    }
                }
            }
            
            LogDebug($"총 {portalCount}개의 포탈이 생성되었습니다.");
        }
        
        // 노드에 적합한 스폰 포인트 찾기
        private PortalSpawnPoint FindSuitableSpawnPoint(PortalSpawnPoint[] spawnPoints, MapNode node)
        {
            // 1. 해당 노드 타입을 허용하고 비어있는 스폰 포인트 찾기
            foreach (PortalSpawnPoint point in spawnPoints)
            {
                if (!point.IsOccupied() && point.CanSpawnNodeType(node.nodeType))
                {
                    return point;
                }
            }
            
            // 2. 타입 제한 없이 비어있는 스폰 포인트 찾기
            foreach (PortalSpawnPoint point in spawnPoints)
            {
                if (!point.IsOccupied())
                {
                    return point;
                }
            }
            
            // 적합한 스폰 포인트 없음
            return null;
        }
        
        // 모든 활성 포탈 제거
        private void ClearAllPortals()
        {
            foreach (GameObject portal in activePortals)
            {
                if (portal != null)
                {
                    Destroy(portal);
                }
            }
            
            activePortals.Clear();
            
            // 씬에 남아있는 스폰 포인트 초기화
            PortalSpawnPoint[] spawnPoints = FindObjectsOfType<PortalSpawnPoint>();
            foreach (PortalSpawnPoint point in spawnPoints)
            {
                point.ClearPortal();
            }
        }
        
        // 디버그 로그
        private void LogDebug(string message)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[PortalManager] {message}");
            }
        }
        
        private void OnDestroy()
        {
            // 이벤트 해제
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}
