using System;
using System.Collections;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("스테이지 설정")]
    [Tooltip("처음 게임 시작 시 기본 스테이지 레벨 (1부터)")]
    public int defaultStageLevel = 1;

    [Tooltip("(선택) 스테이지별 보스 프리팹. 현재 플로우에선 포탈을 통해 보스 씬을 로드하므로 필수는 아님")]
    public GameObject[] bossPrefabs;

    [Header("스폰 위치")]
    public Transform bossSpawnPoint;

    [Header("현재 상태 (읽기전용)")]
    [SerializeField] private int currentStage; // 영구 스테이지 레벨
    [SerializeField] private GameObject currentBoss;

    public int CurrentStage => currentStage;

    [Header("씬 설정")]
    [SerializeField] private string startSceneName = "Start";

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
    }

    void Start()
    {
        // 자동 보스 스폰은 하지 않는다. (포탈을 통해 보스 씬으로 이동)
        OnStageStarted?.Invoke(currentStage);
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

        // 영구 스테이지 레벨 +1 저장
        currentStage += 1;
        PlayerPrefs.SetInt(StageLevelKey, currentStage);
        PlayerPrefs.Save();

        // Start 씬으로 이동하여 루틴을 처음부터 반복
        if (!string.IsNullOrEmpty(startSceneName))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(startSceneName);
        }
        else
        {
            Debug.LogWarning("[StageManager] startSceneName 미설정. 씬 전환이 수행되지 않습니다.");
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