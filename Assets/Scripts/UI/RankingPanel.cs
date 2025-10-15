using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 보스 처치 후 클리어 시간을 표시하는 랭킹 패널
/// </summary>
public class RankingPanel : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI clearTimeText;
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button quitButton;

    [Header("설정")]
    [SerializeField] private float displayDelay = 2f; // 보스 처치 후 패널 표시까지의 지연 시간

    private void Awake()
    {
        // 패널 초기 비활성화
        if (panel != null)
        {
            panel.SetActive(false);
        }

        // 버튼 이벤트 등록
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnClickContinue);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnClickQuit);
        }
    }

    /// <summary>
    /// 랭킹 패널 표시 (보스 처치 시 호출)
    /// </summary>
    public void ShowRanking(float delay = 0f)
    {
        if (delay > 0f)
        {
            Invoke(nameof(DisplayPanel), delay);
        }
        else
        {
            DisplayPanel();
        }
    }

    private void DisplayPanel()
    {
        if (panel == null)
        {
            Debug.LogError("[RankingPanel] Panel이 설정되지 않았습니다!");
            return;
        }

        // 게임 타이머에서 클리어 시간 가져오기
        if (GameTimer.Instance != null)
        {
            string clearTime = GameTimer.Instance.GetFormattedTime();
            
            if (clearTimeText != null)
            {
                clearTimeText.text = $"클리어 타임: {clearTime}";
            }

            Debug.Log($"[RankingPanel] 클리어 타임: {clearTime}");
        }
        else
        {
            Debug.LogWarning("[RankingPanel] GameTimer를 찾을 수 없습니다!");
            if (clearTimeText != null)
            {
                clearTimeText.text = "클리어 타임: --분 --초";
            }
        }

        // 현재 스테이지 표시
        if (stageText != null && StageManager.Instance != null)
        {
            stageText.text = $"Stage {StageManager.Instance.CurrentStage} 클리어!";
        }

        // 패널 활성화
        panel.SetActive(true);

        // 커서 표시
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("[RankingPanel] 랭킹 패널 표시됨");
    }

    /// <summary>
    /// 계속하기 버튼 클릭 (다음 스테이지로)
    /// </summary>
    private void OnClickContinue()
    {
        Debug.Log("[RankingPanel] 계속하기 버튼 클릭");
        
        // 패널 비활성화
        if (panel != null)
        {
            panel.SetActive(false);
        }

        // 커서 숨김
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // StageManager의 기존 씬 전환 로직 실행
        // (BossStageManager에서 이미 처리되므로 여기서는 패널만 닫음)
    }

    /// <summary>
    /// 종료 버튼 클릭 (타이틀 화면으로)
    /// </summary>
    private void OnClickQuit()
    {
        Debug.Log("[RankingPanel] 종료 버튼 클릭");
        
        // 게임 타이머 리셋
        if (GameTimer.Instance != null)
        {
            GameTimer.Instance.ResetTimer();
        }

        // 스테이지 리셋
        if (StageManager.Instance != null)
        {
            // PlayerPrefs를 통해 스테이지 리셋
            PlayerPrefs.SetInt("StageLevel", 1);
            PlayerPrefs.Save();
        }

        // 타이틀 씬으로 이동 (예: "Title" 또는 "Login")
        UnityEngine.SceneManagement.SceneManager.LoadScene("Login");
    }

    private void OnDestroy()
    {
        // 버튼 이벤트 해제
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnClickContinue);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(OnClickQuit);
        }
    }
}
