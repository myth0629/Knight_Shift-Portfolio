using UnityEngine;

/// <summary>
/// 계정별 데이터 저장을 위한 헬퍼 클래스
/// Firebase 사용자 UID를 기반으로 PlayerPrefs 키를 생성합니다.
/// </summary>
public static class AccountDataManager
{
    private const string DEFAULT_USER_ID = "Guest";

    /// <summary>
    /// 현재 로그인한 사용자의 고유 ID를 반환합니다.
    /// </summary>
    public static string GetCurrentUserId()
    {
        if (FirebaseInit.User != null && !string.IsNullOrEmpty(FirebaseInit.User.UserId))
        {
            return FirebaseInit.User.UserId;
        }
        return DEFAULT_USER_ID;
    }

    /// <summary>
    /// 계정별 키를 생성합니다.
    /// </summary>
    public static string BuildAccountKey(string baseKey)
    {
        string userId = GetCurrentUserId();
        return $"{userId}_{baseKey}";
    }

    /// <summary>
    /// 계정별 정수 값을 저장합니다.
    /// </summary>
    public static void SetInt(string key, int value)
    {
        string accountKey = BuildAccountKey(key);
        PlayerPrefs.SetInt(accountKey, value);
    }

    /// <summary>
    /// 계정별 정수 값을 가져옵니다.
    /// </summary>
    public static int GetInt(string key, int defaultValue = 0)
    {
        string accountKey = BuildAccountKey(key);
        return PlayerPrefs.GetInt(accountKey, defaultValue);
    }

    /// <summary>
    /// 계정별 문자열 값을 저장합니다.
    /// </summary>
    public static void SetString(string key, string value)
    {
        string accountKey = BuildAccountKey(key);
        PlayerPrefs.SetString(accountKey, value);
    }

    /// <summary>
    /// 계정별 문자열 값을 가져옵니다.
    /// </summary>
    public static string GetString(string key, string defaultValue = "")
    {
        string accountKey = BuildAccountKey(key);
        return PlayerPrefs.GetString(accountKey, defaultValue);
    }

    /// <summary>
    /// 계정별 실수 값을 저장합니다.
    /// </summary>
    public static void SetFloat(string key, float value)
    {
        string accountKey = BuildAccountKey(key);
        PlayerPrefs.SetFloat(accountKey, value);
    }

    /// <summary>
    /// 계정별 실수 값을 가져옵니다.
    /// </summary>
    public static float GetFloat(string key, float defaultValue = 0f)
    {
        string accountKey = BuildAccountKey(key);
        return PlayerPrefs.GetFloat(accountKey, defaultValue);
    }

    /// <summary>
    /// 계정별 키가 존재하는지 확인합니다.
    /// </summary>
    public static bool HasKey(string key)
    {
        string accountKey = BuildAccountKey(key);
        return PlayerPrefs.HasKey(accountKey);
    }

    /// <summary>
    /// 계정별 키를 삭제합니다.
    /// </summary>
    public static void DeleteKey(string key)
    {
        string accountKey = BuildAccountKey(key);
        PlayerPrefs.DeleteKey(accountKey);
    }

    /// <summary>
    /// 변경사항을 저장합니다.
    /// </summary>
    public static void Save()
    {
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 현재 사용자의 모든 데이터를 삭제합니다.
    /// </summary>
    public static void ClearCurrentUserData()
    {
        // 주의: 이 메서드는 현재 사용자의 모든 키를 삭제하지는 않습니다.
        // 필요한 경우 특정 키들을 수동으로 삭제해야 합니다.
        Debug.LogWarning("ClearCurrentUserData: 개별 키를 지정하여 삭제해야 합니다.");
    }
}
