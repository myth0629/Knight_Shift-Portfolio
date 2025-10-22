using System;
using System.Collections;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("스테이지 설정")]
    [Tooltip("처음 게임 시작 시 기본 스테이지 레벨 (1부터)")]
    public int defaultStageLevel = 1;
    
    [Tooltip("최대 스테이지 수 (스테이지 2까지)")]
    public int maxStageLevel = 2;

    [Tooltip("(선택) 스테이지별 보스 프리팹. 현재 플로우에선 포탈을 통해 보스 씬을 로드하므로 필수는 아님")]
    public GameObject[] bossPrefabs;

    [Header("스폰 위치")]
    public Transform bossSpawnPoint;

    [Header("일반 몬스터 설정")]
    [Tooltip("생성할 일반 몬스터 프리팹 목록 (현재 2종)")]
    public GameObject[] normalMonsterPrefabs;

    [Tooltip("스테이지 1에서 생성할 기본 몬스터 수")]
    public int baseMonsterCount = 3;

    [Tooltip("스테이지 레벨당 증가할 몬스터 수")]
    public int monsterCountIncreasePerLevel = 1;

    [Header("현재 상태 (읽기전용)")]
    [SerializeField] private int currentStage; // 영구 스테이지 레벨
    [SerializeField] private GameObject currentBoss;
    
    public int BossKillsThisRun { get; private set; } = 0;
    
    public int CurrentStage => currentStage;
    public int MaxStage => maxStageLevel;
    public bool IsLastStage => currentStage >= maxStageLevel;
    [Header("씬 설정")]
    [SerializeField] private string startSceneName = "Start";

    [Header("클리어 후 이동 지연")]
    [Tooltip("보스 처치 후 Start 씬으로 이동하기 전에 대기할 시간 (초)")]
    [SerializeField] private float delayAfterBossDefeat = 5f;

    private const string StageLevelKey = "StageLevel";

    public event Action<int> OnStageStarted;    // 스테이지 시작시 (스테이지 번호)
    public event Action<int> OnStageCleared;    // 스테이지 클리어시 (스테이지 번호)

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 영구 스테이지 레벨 로드
        currentStage = PlayerPrefs.GetInt(StageLevelKey, defaultStageLevel);
        Debug.Log($"[StageManager] Awake - 현재 스테이지: {currentStage} (PlayerPrefs에서 로드)");
    }   
    void Start()
    {
        // 자동 보스 스폰은 하지 않는다. (포탈을 통해 보스 씬으로 이동)
        Debug.Log($"[StageManager] Start - 현재 스테이지: {currentStage}");
        OnStageStarted?.Invoke(currentStage);
    }
    
    /// <summary>
    /// 스테이지를 1로 리셋 (게임 재시작용)
    /// </summary>
    public void ResetToStage1()
    {
        currentStage = 1;
        PlayerPrefs.SetInt(StageLevelKey, currentStage);
        PlayerPrefs.Save();
        BossKillsThisRun = 0;
        Debug.Log("[StageManager] 스테이지를 1로 리셋 완료");
    }

    IEnumerator CoStartStage(int stage)
    {
        currentStage = stage;
        // 이전 보스 제거
        if (currentBoss != null)
        {
            Destroy(currentBoss);
            currentBoss = null;
        }

        // 보스 UI/BGM 리셋
        var healthBar = FindFirstObjectByType<SimpleBossHealthBar>();
        healthBar?.HideBossHealthBar();
        var bgmMgr = FindFirstObjectByType<BossBGMManager>();
        if (bgmMgr != null)
        {
            // 다음 보스 등장 시 BossBGMManager가 자동으로 처리하므로 여기서는 정지해둠
            // 별도 공개 API가 없다면 아무 것도 하지 않음
        }

        // 스폰 포인트 미할당 시 자신의 위치 사용
        Vector3 spawnPos = bossSpawnPoint != null ? bossSpawnPoint.position : transform.position;
        Quaternion spawnRot = bossSpawnPoint != null ? bossSpawnPoint.rotation : Quaternion.identity;

        // 프리팹 로드
        var bossPrefab = GetBossPrefabForStage(stage);
        if (bossPrefab == null)
        {
            Debug.LogWarning($"[StageManager] 스테이지 {stage} 의 보스 프리팹이 없습니다. 진행을 종료합니다.");
            yield break;
        }

        currentBoss = Instantiate(bossPrefab, spawnPos, spawnRot);

        // 보스에게 StageHook 부착 (없다면 추가) — 사망 이벤트 전달용
        var hook = currentBoss.GetComponent<StageBossHook>();
        if (hook == null) hook = currentBoss.AddComponent<StageBossHook>();
        hook.stageManager = this;

        OnStageStarted?.Invoke(currentStage);
        Debug.Log($"[StageManager] Stage {currentStage} 시작. 보스: {currentBoss.name}");
    }

    GameObject GetBossPrefabForStage(int stage)
    {
        int index = Mathf.Clamp(stage - 1, 0, bossPrefabs != null ? bossPrefabs.Length - 1 : 0);
        if (bossPrefabs == null || bossPrefabs.Length == 0) return null;
        if (index < 0 || index >= bossPrefabs.Length) return null;
        return bossPrefabs[index];
    }

    public void NotifyBossDefeated()
    {
        // 현재 스테이지 클리어 처리
        OnStageCleared?.Invoke(currentStage);
        Debug.Log($"[StageManager] Stage {currentStage} 보스 클리어!");
        
        // 타이머는 계속 진행 (랭킹 씬에서 정지)
        if (GameTimer.Instance != null)
        {
            Debug.Log($"[StageManager] 현재 플레이 시간: {GameTimer.Instance.GetFormattedTime()}");
        }
        
        // 보스 처치 카운트 증가
        BossKillsThisRun++;

        // 스테이지 2 완료 확인
        if (currentStage >= maxStageLevel)
        {
            Debug.Log($"[StageManager] 최종 스테이지({maxStageLevel}) 클리어! 게임 종료");
            // 엔딩 씬으로 이동하거나 게임 종료
            StartCoroutine(LoadEndingScene());
        }
        else
        {
            Debug.Log($"[StageManager] 스테이지 {currentStage} 클리어! 다음 스테이지로 진행 준비");

            // 지연 후 Start 씬으로 이동하여 루틴을 처음부터 반복
            StartCoroutine(LoadStartSceneAfterDelay());
        }
    }

    private IEnumerator LoadStartSceneAfterDelay()
    {
        float wait = Mathf.Max(0f, delayAfterBossDefeat);
        if (wait > 0f)
        {
            yield return new WaitForSeconds(wait);
        }
        
        // 스테이지 증가 (씬 로드 전에 먼저 증가)
        currentStage += 1;
        PlayerPrefs.SetInt(StageLevelKey, currentStage);
        PlayerPrefs.Save();
        
        Debug.Log($"[StageManager] 스테이지 증가: Stage {currentStage}");
        
        // 맵 초기화 (새로운 스테이지를 위해)
        if (MapSystem.MapController.Instance != null)
        {
            Debug.Log("[StageManager] 맵 초기화 중...");
            MapSystem.MapController.Instance.ResetAndRegenerateMap();
        }

        if (!string.IsNullOrEmpty(startSceneName))
        {
            // SceneFadeManager가 있으면 페이드 효과와 함께 전환
            if (SceneFadeManager.Instance != null)
            {
                yield return StartCoroutine(SceneFadeManager.Instance.FadeOut());
            }
            
            // 비동기 씬 로딩으로 변경 (로딩 중 멈춤 방지)
            Debug.Log($"[StageManager] {startSceneName} 씬 로딩 시작... (현재 스테이지: {currentStage})");
            AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(startSceneName);
            
            // 씬 로딩이 완료될 때까지 대기
            while (!asyncLoad.isDone)
            {
                Debug.Log($"[StageManager] 로딩 진행률: {asyncLoad.progress * 100}%");
                yield return null;
            }
            
            Debug.Log($"[StageManager] {startSceneName} 씬 로딩 완료! 현재 스테이지: {currentStage}");
        }
        else
        {
            Debug.LogWarning("[StageManager] startSceneName 미설정. 씬 전환이 수행되지 않습니다.");
        }
    }
    
    private IEnumerator LoadEndingScene()
    {
        float wait = Mathf.Max(0f, delayAfterBossDefeat);
        if (wait > 0f)
        {
            yield return new WaitForSeconds(wait);
        }
        
        // 랭킹 씬 이름 (스테이지 2 클리어 후 랭킹 화면)
        string rankingSceneName = "Ranking";
        
        Debug.Log($"[StageManager] 최종 스테이지 클리어! {rankingSceneName} 씬으로 이동");
        
        // SceneFadeManager가 있으면 페이드 효과와 함께 전환
        if (SceneFadeManager.Instance != null)
        {
            yield return StartCoroutine(SceneFadeManager.Instance.FadeOut());
        }
        
        // 랭킹 씬 로드 시도
        if (Application.CanStreamedLevelBeLoaded(rankingSceneName))
        {
            Debug.Log($"[StageManager] {rankingSceneName} 씬 로딩 시작...");
            AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(rankingSceneName);
            
            // 씬 로딩이 완료될 때까지 대기
            while (!asyncLoad.isDone)
            {
                Debug.Log($"[StageManager] 로딩 진행률: {asyncLoad.progress * 100}%");
                yield return null;
            }
            
            Debug.Log($"[StageManager] {rankingSceneName} 씬 로딩 완료!");
        }
        else
        {
            // 랭킹 씬이 없으면 Start 씬으로 복귀
            Debug.LogWarning($"[StageManager] 랭킹 씬 '{rankingSceneName}'을 찾을 수 없습니다. Start 씬으로 복귀합니다.");
            
            // 스테이지 리셋 (게임 재시작)
            currentStage = defaultStageLevel;
            PlayerPrefs.SetInt(StageLevelKey, currentStage);
            PlayerPrefs.Save();
            
            Debug.Log($"[StageManager] {startSceneName} 씬 로딩 시작...");
            AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(startSceneName);
            
            // 씬 로딩이 완료될 때까지 대기
            while (!asyncLoad.isDone)
            {
                Debug.Log($"[StageManager] 로딩 진행률: {asyncLoad.progress * 100}%");
                yield return null;
            }
            
            Debug.Log($"[StageManager] {startSceneName} 씬 로딩 완료!");
        }
    }
}

// 보스 사망을 StageManager에 알리기 위해 보스 오브젝트에 부착되는 훅
public class StageBossHook : MonoBehaviour
{
    [HideInInspector] public StageManager stageManager;
    bool notified = false;

    // 공용 API: 외부(보스)가 호출
    public void OnBossDied()
    {
        if (notified) return;
        notified = true;
        stageManager?.NotifyBossDefeated();
    }
}