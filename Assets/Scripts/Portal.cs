using MapSystem;
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
                #region 씬 이동 및 상태 저장
                // 다음 씬에서 현재 노드 정보 이어가기 위해 저장
                PlayerPrefs.SetInt("SelectedNodeId", nodeId);
                PlayerPrefs.Save();

                // 씬 이름 또는 번호로 이동
                if (!string.IsNullOrEmpty(sceneName))
                {
                    SceneManager.LoadSceneAsync(sceneName);
                }
                else if (sceneNumber >= 0)
                {
                    SceneManager.LoadSceneAsync(sceneNumber);
                }
                #endregion
            }
        }
    }
}
