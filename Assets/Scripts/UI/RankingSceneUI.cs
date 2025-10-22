using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// 랭킹 씬 UI 관리
public class RankingSceneUI : MonoBehaviour
{
    [Header("상위 랭킹 UI (큰 박스)")]
    [SerializeField] private Transform topRankingsContainer;
    [SerializeField] private GameObject rankingEntryPrefab; // 랭킹 항목 Prefab
    
    [Header("내 기록 UI (작은 박스)")]
    [SerializeField] private TextMeshProUGUI myRecordText;
    [SerializeField] private TextMeshProUGUI myEmailText;
    [SerializeField] private TextMeshProUGUI myTimeText;
    
    [Header("버튼")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;
    
    [Header("로딩 UI")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TextMeshProUGUI loadingText;
    
    private string currentUserEmail;

    private void Start()
    {
        // 버튼 이벤트 등록
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }
        
        // 커서 표시
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // 랭킹 데이터 로드 및 업로드
        InitializeRanking();
    }
    
    private async void InitializeRanking()
    {
        ShowLoading(true, "클리어 기록 업로드 중...");
        
        Debug.Log("[RankingSceneUI] ========== 랭킹 초기화 시작 ==========");
        
        // 🎯 랭킹 씬 진입 시 타이머 정지 (최종 클리어 시간 고정)
        if (GameTimer.Instance != null && GameTimer.Instance.IsRunning)
        {
            GameTimer.Instance.StopTimer();
            Debug.Log($"[RankingSceneUI] ⏱️ 타이머 정지! 최종 클리어 시간: {GameTimer.Instance.GetFormattedTime()}");
        }
        
        // 현재 로그인한 사용자 이메일 가져오기
        currentUserEmail = GetCurrentUserEmail();
        
        if (string.IsNullOrEmpty(currentUserEmail))
        {
            Debug.LogWarning("[RankingSceneUI] 로그인 정보를 찾을 수 없습니다!");
            if (myRecordText != null) myRecordText.text = "로그인 정보 없음";
            if (myEmailText != null) myEmailText.text = "로그인 필요";
            if (myTimeText != null) myTimeText.text = "--분 --초";
            ShowLoading(false);
            return;
        }
        
        Debug.Log($"[RankingSceneUI] 로그인 사용자: {currentUserEmail}");
        
        // GameTimer 상태 확인
        if (GameTimer.Instance == null)
        {
            Debug.LogError("[RankingSceneUI] ❌ GameTimer.Instance가 null입니다!");
        }
        else
        {
            Debug.Log($"[RankingSceneUI] GameTimer 상태 - IsRunning: {GameTimer.Instance.IsRunning}, HasFinished: {GameTimer.Instance.HasFinished}");
        }
        
        // 게임 타이머에서 클리어 시간 가져오기
        if (GameTimer.Instance != null && GameTimer.Instance.HasFinished)
        {
            float clearTimeSeconds = GameTimer.Instance.GetElapsedSeconds();
            string formattedTime = GameTimer.Instance.GetFormattedTime();
            int currentStage = StageManager.Instance != null ? StageManager.Instance.CurrentStage : 2;
            
            Debug.Log($"[RankingSceneUI] ✅ 클리어 시간: {formattedTime} ({clearTimeSeconds}초), 스테이지: {currentStage}");
            
            // Firebase에 기록 업로드
            if (RankingManager.Instance != null)
            {
                Debug.Log("[RankingSceneUI] Firebase에 기록 업로드 시도...");
                bool success = await RankingManager.Instance.UploadClearRecord(
                    currentUserEmail,
                    clearTimeSeconds,
                    formattedTime,
                    currentStage
                );
                
                if (success)
                {
                    Debug.Log("[RankingSceneUI] ✅ 클리어 기록 업로드 성공!");
                }
                else
                {
                    Debug.LogError("[RankingSceneUI] ❌ 클리어 기록 업로드 실패!");
                }
            }
            else
            {
                Debug.LogError("[RankingSceneUI] ❌ RankingManager.Instance가 null입니다!");
            }
        }
        else
        {
            Debug.LogWarning("[RankingSceneUI] ⚠️ GameTimer를 찾을 수 없거나 게임이 완료되지 않았습니다!");
            Debug.LogWarning("[RankingSceneUI] 테스트용 더미 데이터 업로드 시도...");
            
            // 테스트용 더미 데이터 업로드
            if (RankingManager.Instance != null && !string.IsNullOrEmpty(currentUserEmail))
            {
                bool success = await RankingManager.Instance.UploadClearRecord(
                    currentUserEmail,
                    300f, // 5분
                    "05분 00초",
                    2
                );
                Debug.Log($"[RankingSceneUI] 테스트 데이터 업로드 결과: {success}");
            }
        }
        
        // 랭킹 로드
        ShowLoading(true, "랭킹 불러오는 중...");
        Debug.Log("[RankingSceneUI] 랭킹 데이터 로드 시작...");
        await LoadRankings();
        
        Debug.Log("[RankingSceneUI] ========== 랭킹 초기화 완료 ==========");
        ShowLoading(false);
    }
    
    private async System.Threading.Tasks.Task LoadRankings()
    {
        if (RankingManager.Instance == null)
        {
            Debug.LogError("[RankingSceneUI] ❌ RankingManager를 찾을 수 없습니다!");
            
            // 빈 상태 UI 표시
            DisplayEmptyState();
            return;
        }
        
        Debug.Log("[RankingSceneUI] 상위 5명 랭킹 로드 중...");
        
        // 상위 5명 랭킹 로드
        List<RankingData> topRankings = await RankingManager.Instance.GetTopRankings(5);
        
        if (topRankings == null)
        {
            Debug.LogError("[RankingSceneUI] ❌ topRankings가 null입니다!");
        }
        else
        {
            Debug.Log($"[RankingSceneUI] ✅ 상위 랭킹 {topRankings.Count}개 로드됨");
        }
        
        // 상위 랭킹 UI 표시
        DisplayTopRankings(topRankings);
        
        Debug.Log($"[RankingSceneUI] 내 기록 로드 중... (이메일: {currentUserEmail})");
        
        // 내 기록 로드 및 표시
        RankingData myRecord = await RankingManager.Instance.GetUserRecord(currentUserEmail);
        
        if (myRecord == null)
        {
            Debug.LogWarning("[RankingSceneUI] ⚠️ 내 기록을 찾을 수 없습니다!");
        }
        else
        {
            Debug.Log($"[RankingSceneUI] ✅ 내 기록 로드됨: {myRecord.formattedTime}");
        }
        
        DisplayMyRecord(myRecord, topRankings);
    }
    
    private void DisplayEmptyState()
    {
        Debug.Log("[RankingSceneUI] 빈 상태 표시");
        
        if (myRecordText != null) myRecordText.text = "기록 없음";
        if (myEmailText != null) myEmailText.text = currentUserEmail ?? "이메일 없음";
        if (myTimeText != null) myTimeText.text = "--분 --초";
        
        // 상위 랭킹도 빈 상태 표시
        if (topRankingsContainer != null)
        {
            foreach (Transform child in topRankingsContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }
    
    private void DisplayTopRankings(List<RankingData> rankings)
    {
        Debug.Log("[RankingSceneUI] DisplayTopRankings 호출됨");
        
        // 기존 항목 제거
        foreach (Transform child in topRankingsContainer)
        {
            Destroy(child.gameObject);
        }
        
        if (rankings == null || rankings.Count == 0)
        {
            Debug.LogWarning("[RankingSceneUI] ⚠️ 표시할 랭킹이 없습니다.");
            
            // 빈 상태 표시
            if (rankingEntryPrefab != null)
            {
                GameObject emptyEntry = Instantiate(rankingEntryPrefab, topRankingsContainer);
                var texts = emptyEntry.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 3)
                {
                    texts[0].text = "-";
                    texts[1].text = "기록 없음";
                    texts[2].text = "--분 --초";
                }
                Debug.Log("[RankingSceneUI] 빈 상태 항목 생성됨");
            }
            else
            {
                Debug.LogError("[RankingSceneUI] ❌ rankingEntryPrefab이 null입니다!");
            }
            return;
        }
        
        Debug.Log($"[RankingSceneUI] {rankings.Count}개의 랭킹 항목 생성 시작");
        
        // 랭킹 항목 생성
        for (int i = 0; i < rankings.Count; i++)
        {
            RankingData data = rankings[i];
            
            GameObject entry = Instantiate(rankingEntryPrefab, topRankingsContainer);
            
            // TextMeshProUGUI 컴포넌트 찾기
            TextMeshProUGUI[] texts = entry.GetComponentsInChildren<TextMeshProUGUI>();
            
            Debug.Log($"[RankingSceneUI] Entry {i}: TextMeshPro 개수 = {texts.Length}");
            
            if (texts.Length >= 3)
            {
                // 0: 순위, 1: 이메일, 2: 시간
                texts[0].text = $"{i + 1}";
                texts[1].text = MaskEmail(data.email);
                texts[2].text = data.formattedTime;
                
                // 1등은 금색, 2등은 은색, 3등은 동색으로 표시 (선택사항)
                if (i == 0)
                {
                    texts[0].color = new Color(1f, 0.84f, 0f); // 금색
                }
                else if (i == 1)
                {
                    texts[0].color = new Color(0.75f, 0.75f, 0.75f); // 은색
                }
                else if (i == 2)
                {
                    texts[0].color = new Color(0.8f, 0.5f, 0.2f); // 동색
                }
            }
            
            Debug.Log($"[RankingSceneUI] ✅ {i + 1}위: {data.email} - {data.formattedTime}");
        }
    }
    
    private void DisplayMyRecord(RankingData myRecord, List<RankingData> topRankings)
    {
        Debug.Log("[RankingSceneUI] DisplayMyRecord 호출됨");
        
        if (myRecord == null)
        {
            Debug.LogWarning("[RankingSceneUI] ⚠️ myRecord가 null - 기본값 표시");
            if (myRecordText != null) myRecordText.text = "내 기록: 기록 없음";
            if (myEmailText != null) myEmailText.text = currentUserEmail ?? "이메일 없음";
            if (myTimeText != null) myTimeText.text = "--분 --초";
            return;
        }
        
        Debug.Log($"[RankingSceneUI] 내 기록 데이터: {myRecord.email}, {myRecord.formattedTime}");
        
        // 내 순위 계산
        int myRank = -1;
        if (topRankings != null)
        {
            myRank = topRankings.FindIndex(r => r.email == myRecord.email) + 1;
            Debug.Log($"[RankingSceneUI] 내 순위: {myRank}");
        }
        
        if (myRank > 0 && myRank <= 5)
        {
            if (myRecordText != null) myRecordText.text = $"#{myRank}";
            Debug.Log($"[RankingSceneUI] ✅ 상위 5위 안에 포함: {myRank}위");
        }
        else
        {
            if (myRecordText != null) myRecordText.text = "내 기록";
            Debug.Log("[RankingSceneUI] 상위 5위 밖 또는 순위 계산 실패");
        }
        
        if (myEmailText != null) myEmailText.text = MaskEmail(myRecord.email);
        if (myTimeText != null) myTimeText.text = myRecord.formattedTime;
        
        Debug.Log($"[RankingSceneUI] ✅ 내 기록 표시 완료: {myRecord.formattedTime}");
    }
    
    /// <summary>
    /// 이메일 마스킹 (개인정보 보호)
    /// 예: test@example.com → te**@ex*****.com
    /// </summary>
    private string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains("@"))
        {
            return email;
        }
        
        string[] parts = email.Split('@');
        string localPart = parts[0];
        string domainPart = parts[1];
        
        // 로컬 파트 마스킹 (앞 2글자만 표시)
        string maskedLocal = localPart.Length > 2 
            ? localPart.Substring(0, 2) + new string('*', localPart.Length - 2)
            : localPart;
        
        // 도메인 파트 마스킹 (앞 2글자와 .com 만 표시)
        int dotIndex = domainPart.LastIndexOf('.');
        if (dotIndex > 0)
        {
            string domainName = domainPart.Substring(0, dotIndex);
            string extension = domainPart.Substring(dotIndex);
            string maskedDomain = domainName.Length > 2
                ? domainName.Substring(0, 2) + new string('*', domainName.Length - 2)
                : domainName;
            domainPart = maskedDomain + extension;
        }
        
        return $"{maskedLocal}@{domainPart}";
    }
    
    private string GetCurrentUserEmail()
    {
        // FirebaseAuthManager에서 현재 로그인한 사용자 이메일 가져오기
        if (FirebaseAuthManager.Instance != null && FirebaseAuthManager.Instance.GetCurrentUser() != null)
        {
            string email = FirebaseAuthManager.Instance.GetCurrentUser().Email;
            Debug.Log($"[RankingSceneUI] 현재 사용자: {email}");
            return email;
        }
        
        Debug.LogWarning("[RankingSceneUI] FirebaseAuthManager에서 사용자 정보를 가져올 수 없습니다!");
        return null;
    }
    
    private void ShowLoading(bool show, string message = "로딩 중...")
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(show);
        }
        
        if (loadingText != null)
        {
            loadingText.text = message;
        }
    }
    
    private void OnRestartClicked()
    {
        Debug.Log("[RankingSceneUI] 다시 시작 버튼 클릭");
        
        // 게임 타이머 리셋
        if (GameTimer.Instance != null)
        {
            GameTimer.Instance.ResetTimer();
            Debug.Log("[RankingSceneUI] GameTimer 리셋 완료");
        }
        
        // 스테이지를 1로 리셋
        if (StageManager.Instance != null)
        {
            StageManager.Instance.ResetToStage1();
            Debug.Log("[RankingSceneUI] StageManager를 통해 스테이지 1로 리셋");
        }
        else
        {
            // StageManager가 없으면 PlayerPrefs에 직접 저장
            PlayerPrefs.SetInt("StageLevel", 1);
            PlayerPrefs.Save();
            Debug.Log("[RankingSceneUI] PlayerPrefs에 스테이지 1 저장");
        }
        
        // Login 씬으로 이동
        Debug.Log("[RankingSceneUI] Login 씬으로 이동");
        SceneManager.LoadScene("Login");
    }
    
    private void OnQuitClicked()
    {
        Debug.Log("[RankingSceneUI] 종료 버튼 클릭");
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    
    private void OnDestroy()
    {
        // 버튼 이벤트 해제
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartClicked);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(OnQuitClicked);
        }
    }
}
