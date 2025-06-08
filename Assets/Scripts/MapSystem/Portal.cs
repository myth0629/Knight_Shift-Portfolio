using MapSystem;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    [SerializeField] private string sceneName; // 전환할 씬 이름
    [SerializeField] private int sceneNumber;
    
    // 노드 ID 저장용 변수
    private int nodeId = -1;
    
    // 포털 클릭 이벤트를 위한 델리게이트
    public delegate void PortalClickedHandler(int nodeId);
    public static event PortalClickedHandler OnPortalClicked;

    // 씬 이름 설정 메서드
    public void SetSceneName(string name)
    {
        sceneName = name;
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
        
        // 화면 페이드 아웃 등의 전환 효과를 여기에 추가할 수 있음
        
        // 씬 로드 시작
        AsyncOperation asyncLoad = null;
        if (!string.IsNullOrEmpty(sceneName))
        {
            asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            Debug.Log($"씬 '{sceneName}'을(를) 로드합니다.");
        }
        else if (sceneNumber >= 0)
        {
            asyncLoad = SceneManager.LoadSceneAsync(sceneNumber);
            Debug.Log($"씬 번호 {sceneNumber}을(를) 로드합니다.");
        }
        
        // 씬 로드가 완료될 때까지 대기
        if (asyncLoad != null)
        {
            asyncLoad.allowSceneActivation = true;
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }
        
        Debug.Log("씬 전환이 완료되었습니다.");
    }
}
