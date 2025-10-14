using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 보스 스테이지를 관리하고 보스가 죽으면 다음 씬으로 전환
/// </summary>
public class BossStageManager : MonoBehaviour
{
    public static BossStageManager Instance { get; private set; }
    
    [Header("씬 전환 설정")]
    [SerializeField] private string nextSceneName = "Start";
    [SerializeField] private float delayBeforeTransition = 3f; // 보스 사망 후 대기 시간
    
    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 보스가 죽었을 때 호출되는 메서드
    /// </summary>
    public void OnBossDeath()
    {
        if (!isTransitioning)
        {
            Debug.Log("보스 사망! 씬 전환 시작...");
            StartCoroutine(TransitionToNextScene());
        }
    }

    private IEnumerator TransitionToNextScene()
    {
        isTransitioning = true;
        
        // 대기 시간 (승리 연출, 골드 획득 등)
        Debug.Log($"{delayBeforeTransition}초 대기 중...");
        yield return new WaitForSeconds(delayBeforeTransition);
        
        // 맵 초기화 (새로운 스테이지 시작을 위해)
        if (MapSystem.MapController.Instance != null)
        {
            Debug.Log("[BossStageManager] 맵 초기화 중...");
            MapSystem.MapController.Instance.ResetAndRegenerateMap();
        }
        else
        {
            Debug.LogWarning("[BossStageManager] MapController를 찾을 수 없습니다.");
        }
        
        // SceneFadeManager가 있으면 페이드 효과와 함께 전환
        if (SceneFadeManager.Instance != null)
        {
            Debug.Log($"페이드 효과와 함께 {nextSceneName} 씬으로 전환");
            
            // 페이드 아웃
            yield return StartCoroutine(SceneFadeManager.Instance.FadeOut());
            
            // 씬 로드
            SceneManager.LoadScene(nextSceneName);
            
            // 페이드 인은 SceneFadeManager의 OnSceneLoaded에서 자동 처리
        }
        else
        {
            // SceneFadeManager가 없으면 바로 전환
            Debug.LogWarning("SceneFadeManager를 찾을 수 없습니다. 즉시 씬 전환합니다.");
            SceneManager.LoadScene(nextSceneName);
        }
    }

    /// <summary>
    /// 외부에서 수동으로 씬 전환 트리거 (테스트용)
    /// </summary>
    public void TriggerSceneTransition()
    {
        if (!isTransitioning)
        {
            Debug.Log("수동 씬 전환 트리거!");
            StartCoroutine(TransitionToNextScene());
        }
    }

    /// <summary>
    /// 씬 전환 대기 시간 설정
    /// </summary>
    public void SetTransitionDelay(float delay)
    {
        delayBeforeTransition = delay;
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}
