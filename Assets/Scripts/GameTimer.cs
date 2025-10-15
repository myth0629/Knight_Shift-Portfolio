using UnityEngine;

/// <summary>
/// 게임 시작부터 보스 처치까지의 시간을 측정하는 싱글톤 매니저
/// </summary>
public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance { get; private set; }
    
    private float startTime;
    private float endTime;
    private bool isRunning = false;
    private bool hasFinished = false;
    
    public float ElapsedTime => isRunning ? (Time.time - startTime) : (endTime - startTime);
    public bool IsRunning => isRunning;
    public bool HasFinished => hasFinished;

    private void Awake()
    {
        // 싱글톤 패턴 (씬 전환 시 유지)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[GameTimer] 초기화 완료");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 타이머 시작 (게임 시작 시 호출)
    /// </summary>
    public void StartTimer()
    {
        startTime = Time.time;
        isRunning = true;
        hasFinished = false;
        Debug.Log($"[GameTimer] 타이머 시작: {startTime}");
    }

    /// <summary>
    /// 타이머 정지 (보스 처치 시 호출)
    /// </summary>
    public void StopTimer()
    {
        if (isRunning)
        {
            endTime = Time.time;
            isRunning = false;
            hasFinished = true;
            Debug.Log($"[GameTimer] 타이머 정지: {GetFormattedTime()}");
        }
    }

    /// <summary>
    /// 타이머 리셋 (게임 재시작 시)
    /// </summary>
    public void ResetTimer()
    {
        startTime = 0f;
        endTime = 0f;
        isRunning = false;
        hasFinished = false;
        Debug.Log("[GameTimer] 타이머 리셋");
    }

    /// <summary>
    /// 현재 경과 시간을 "00분 00초" 형식으로 반환
    /// </summary>
    public string GetFormattedTime()
    {
        float elapsed = ElapsedTime;
        int minutes = Mathf.FloorToInt(elapsed / 60f);
        int seconds = Mathf.FloorToInt(elapsed % 60f);
        return $"{minutes:D2}분 {seconds:D2}초";
    }

    /// <summary>
    /// 현재 경과 시간을 초 단위로 반환
    /// </summary>
    public float GetElapsedSeconds()
    {
        return ElapsedTime;
    }
}
