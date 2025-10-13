using MapSystem;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    [SerializeField] private string defaultSceneName; // Inspector에서 설정하는 기본 씬 이름 (백업용)
    [SerializeField] private int sceneNumber;
    
    // 노드 ID 저장용 변수
    private int nodeId = -1;
    
    // 동적으로 설정된 씬 이름 (런타임에 SetSceneName으로 설정)
    private string runtimeSceneName = null;
    
    // 포털 클릭 이벤트를 위한 델리게이트
    public delegate void PortalClickedHandler(int nodeId);
    public static event PortalClickedHandler OnPortalClicked;

    // 씬 이름 설정 메서드 (런타임에 동적으로 설정)
    public void SetSceneName(string name)
    {
        runtimeSceneName = name;
        Debug.Log($"Portal scene name set to: {name}");
    }
    
    // 실제 사용할 씬 이름 가져오기 (런타임 설정 우선, 없으면 기본값)
    private string GetSceneName()
    {
        return !string.IsNullOrEmpty(runtimeSceneName) ? runtimeSceneName : defaultSceneName;
    }
    
    // 노드 ID 설정 메서드
    public void SetNodeId(int id)
    {
        nodeId = id;
    }

    void OnTriggerEnter(Collider other)
    {
        // 플레이어가 포탈 트리거에 진입했을 때
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered the portal");

            // 노드 ID가 유효할 때만 실행
            if (nodeId >= 0)
            {
                // 씬 전환 코루틴 시작
                StartCoroutine(TransitionToScene());
            }
        }
    }
    
    // 씬 전환을 위한 코루틴
    private IEnumerator TransitionToScene()
    {
        string targetSceneName = GetSceneName();
        
        // 배틀 씬인 경우 실시간으로 다음 배틀 씬 결정
        if (!string.IsNullOrEmpty(targetSceneName) && IsBattleScene(targetSceneName))
        {
            // 현재 저장된 마지막 배틀 씬 기준으로 다음 씬 결정
            targetSceneName = MapSystem.MapController.GetNextBattleSceneAndUpdate();
            Debug.Log($"Battle scene determined at portal entry: {targetSceneName}");
        }
        
        // 다음 씬에서 현재 노드 정보 이어가기 위해 저장
        PlayerPrefs.SetInt("SelectedNodeId", nodeId);
        PlayerPrefs.Save();
        
        // PersistentObjectManager가 없으면 생성
        if (PersistentObjectManager.Instance == null)
        {
            GameObject persistentManager = new GameObject("PersistentObjectManager");
            persistentManager.AddComponent<PersistentObjectManager>();
            Debug.Log("PersistentObjectManager를 생성했습니다.");
        }
        else
        {
            // 이미 존재하는 경우 유지할 오브젝트 설정 갱신
            PersistentObjectManager.Instance.SetPersistentObjects();
        }
        
        // 페이드 아웃 효과 시작
        if (SceneFadeManager.Instance != null)
        {
            yield return StartCoroutine(SceneFadeManager.Instance.FadeOut());
        }
        
        // 씬 로드 시작
        AsyncOperation asyncLoad = null;
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
            asyncLoad.allowSceneActivation = false; // 로딩 완료 후 수동으로 활성화
            Debug.Log($"씬 '{targetSceneName}'을(를) 로드합니다.");
        }
        else if (sceneNumber >= 0)
        {
            asyncLoad = SceneManager.LoadSceneAsync(sceneNumber);
            asyncLoad.allowSceneActivation = false;
            Debug.Log($"씬 번호 {sceneNumber}을(를) 로드합니다.");
        }
        
        // 씬 로드가 90% 완료될 때까지 대기
        if (asyncLoad != null)
        {
            while (asyncLoad.progress < 0.9f)
            {
                yield return null;
            }
            
            // 페이드 아웃이 완전히 끝난 후 씬 활성화
            yield return new WaitForSeconds(0.1f);
            
            asyncLoad.allowSceneActivation = true;
            
            // 씬 전환 완료 대기
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }
        
        Debug.Log("씬 전환이 완료되었습니다.");
        
        // 새로운 씬에서 페이드 인 효과는 SceneFadeManager의 Start()에서 자동 실행
    }
    
    // 배틀 씬인지 확인하는 헬퍼 메서드
    private bool IsBattleScene(string scene)
    {
        // 배틀 씬 이름들과 비교 (Battle, Battle-Cave, Battle-Dungeon 등)
        return scene.StartsWith("Battle");
    }
}
