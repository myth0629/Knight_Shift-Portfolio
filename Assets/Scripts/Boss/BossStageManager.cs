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
            Debug.Log("보스 사망! 타이머 정지 및 랭킹 패널 표시...");
            
            // 게임 타이머 정지
            if (GameTimer.Instance != null)
            {
                GameTimer.Instance.StopTimer();
                Debug.Log($"[BossStageManager] 클리어 타임: {GameTimer.Instance.GetFormattedTime()}");
            }
            
            // 랭킹 패널 표시
            RankingPanel rankingPanel = FindFirstObjectByType<RankingPanel>();
            if (rankingPanel != null)
            {
                rankingPanel.ShowRanking(1f); // 1초 후 랭킹 패널 표시
            }
            else
            {
                Debug.LogWarning("[BossStageManager] RankingPanel을 찾을 수 없습니다!");
            }
            
            StartCoroutine(TransitionToNextScene());
        }
    }

    private IEnumerator TransitionToNextScene()
    {
        isTransitioning = true;
        
        // 대기 시간 (승리 연출, 골드 획득 등)
        Debug.Log($"{delayBeforeTransition}초 대기 중...");
        yield return new WaitForSeconds(delayBeforeTransition);
        
        // StageManager가 있으면 그쪽에서 처리 (스테이지 로직 통합)
        if (StageManager.Instance != null)
        {
            Debug.Log("[BossStageManager] StageManager에게 보스 처치 알림 전달");
            StageManager.Instance.NotifyBossDefeated();
        }
        else
        {
            // StageManager가 없는 경우 기본 동작 (맵 초기화 후 Start 씬으로)
            Debug.LogWarning("[BossStageManager] StageManager를 찾을 수 없습니다. 기본 동작으로 진행합니다.");
            
            // 맵 초기화
            if (MapSystem.MapController.Instance != null)
            {
                Debug.Log("[BossStageManager] 맵 초기화 중...");
                MapSystem.MapController.Instance.ResetAndRegenerateMap();
            }
            
            // SceneFadeManager가 있으면 페이드 효과와 함께 전환
            if (SceneFadeManager.Instance != null)
            {
                yield return StartCoroutine(SceneFadeManager.Instance.FadeOut());
            }
            
            // 비동기 씬 로딩으로 변경 (로딩 중 멈춤 방지)
            Debug.Log($"[BossStageManager] {nextSceneName} 씬 로딩 시작...");
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName);
            
            // 씬 로딩이 완료될 때까지 대기
            while (!asyncLoad.isDone)
            {
                Debug.Log($"[BossStageManager] 로딩 진행률: {asyncLoad.progress * 100}%");
                yield return null;
            }
            
            Debug.Log($"[BossStageManager] {nextSceneName} 씬 로딩 완료!");
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
