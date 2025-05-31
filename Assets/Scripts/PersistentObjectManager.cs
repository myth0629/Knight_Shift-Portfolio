using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 씬 전환 시 유지되어야 하는 오브젝트들을 관리하는 매니저 클래스
public class PersistentObjectManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static PersistentObjectManager Instance { get; private set; }
    
    // 씬 전환 시 유지할 태그 목록
    [SerializeField] private string[] persistentTags = { "Player", "MainCamera", "UI", "GameManager" };
    
    // 유지되는 오브젝트들의 원래 위치 저장
    private Dictionary<string, Vector3> originalPositions = new Dictionary<string, Vector3>();
    
    // 씬 로드 완료 이벤트를 위한 델리게이트
    public delegate void SceneLoadedHandler(string sceneName);
    public static event SceneLoadedHandler OnSceneLoaded;
    
    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 씬 로드 이벤트 구독
            SceneManager.sceneLoaded += OnSceneLoadedEvent;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // 시작 시 유지할 오브젝트들 설정
        SetPersistentObjects();
    }
    
    // 유지할 오브젝트들을 찾아 DontDestroyOnLoad 설정
    public void SetPersistentObjects()
    {
        foreach (string tag in persistentTags)
        {
            GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in objects)
            {
                // 원래 위치 저장
                originalPositions[obj.name] = obj.transform.position;
                
                // DontDestroyOnLoad 설정
                DontDestroyOnLoad(obj);
                Debug.Log($"오브젝트 '{obj.name}'를 DontDestroyOnLoad로 설정했습니다.");
            }
        }
    }
    
    // 씬 로드 완료 시 호출되는 이벤트 핸들러
    private void OnSceneLoadedEvent(Scene scene, LoadSceneMode mode)
    {
        // 플레이어 위치 조정 (각 씬의 시작 위치로)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // 새 씬의 PlayerStart 오브젝트 찾기
            GameObject playerStart = GameObject.FindGameObjectWithTag("PlayerStart");
            Vector3 targetPosition = playerStart != null ? playerStart.transform.position : new Vector3(0, 1, 0);

            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
                player.transform.position = targetPosition;
                controller.enabled = true;
                Debug.Log($"플레이어(Rigidbody) 위치를 {targetPosition}로 조정했습니다.");
            }
            else
            {
                player.transform.position = targetPosition;
                Debug.Log($"플레이어 위치를 {targetPosition}로 조정했습니다.");
            }
        }
        
        // 카메라 위치 조정 (플레이어 기준)
        GameObject mainCamera = GameObject.FindGameObjectWithTag("Camera");
        if (mainCamera != null && player != null)
        {
            // 카메라와 플레이어 간의 상대 위치 유지
            Vector3 cameraOffset = new Vector3(0, 5, -10); // 적절한 오프셋으로 조정
            mainCamera.transform.position = player.transform.position + cameraOffset;
            Debug.Log("카메라 위치를 플레이어 기준으로 조정했습니다.");
        }
        
        // 이벤트 발생
        OnSceneLoaded?.Invoke(scene.name);
        Debug.Log($"씬 '{scene.name}'이(가) 로드되었습니다.");
    }
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        SceneManager.sceneLoaded -= OnSceneLoadedEvent;
    }
}
