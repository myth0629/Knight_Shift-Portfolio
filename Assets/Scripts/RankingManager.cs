using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;

/// <summary>
/// Firebase를 사용한 랭킹 관리 시스템
/// </summary>
public class RankingManager : MonoBehaviour
{
    public static RankingManager Instance { get; private set; }
    
    private DatabaseReference databaseReference;
    private const string RANKING_PATH = "rankings";
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[RankingManager] 인스턴스 생성 완료");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Firebase 초기화를 기다림
        InitializeFirebase();
    }
    
    private async void InitializeFirebase()
    {
        Debug.Log("[RankingManager] Firebase 초기화 대기 중...");
        
        // FirebaseInit이 준비될 때까지 대기
        int maxWaitTime = 10; // 최대 10초 대기
        int waitedTime = 0;
        
        while (!FirebaseInit.IsReady && waitedTime < maxWaitTime)
        {
            await Task.Delay(100); // 0.1초 대기
            waitedTime++;
        }
        
        if (!FirebaseInit.IsReady)
        {
            Debug.LogError("[RankingManager] Firebase 초기화 시간 초과!");
            return;
        }
        
        try
        {
            // FirebaseInit의 DB를 사용
            databaseReference = FirebaseInit.DB;
            
            if (databaseReference != null)
            {
                Debug.Log("[RankingManager] ✅ Firebase Database 연결 완료!");
            }
            else
            {
                Debug.LogError("[RankingManager] ❌ Firebase DB reference가 null입니다!");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[RankingManager] Firebase 초기화 실패: {e.Message}\n{e.StackTrace}");
        }
    }
    
    /// <summary>
    /// 클리어 기록을 Firebase에 저장
    /// </summary>
    public async Task<bool> UploadClearRecord(string email, float clearTimeSeconds, string formattedTime, int stage)
    {
        if (databaseReference == null)
        {
            Debug.LogError("[RankingManager] Database reference is null!");
            return false;
        }
        
        try
        {
            // 이메일을 키로 사용 가능한 형태로 변환 (. 제거)
            string sanitizedEmail = SanitizeEmail(email);
            
            RankingData rankingData = new RankingData(email, clearTimeSeconds, formattedTime, stage);
            
            // JSON으로 변환
            string json = JsonUtility.ToJson(rankingData);
            
            // Firebase에 저장
            await databaseReference
                .Child(RANKING_PATH)
                .Child(sanitizedEmail)
                .SetRawJsonValueAsync(json);
            
            Debug.Log($"[RankingManager] 클리어 기록 업로드 성공: {email}, {formattedTime}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[RankingManager] 업로드 실패: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 전체 랭킹 가져오기 (정렬 포함)
    /// </summary>
    public async Task<List<RankingData>> GetAllRankings(bool ascending = true)
    {
        if (databaseReference == null)
        {
            Debug.LogError("[RankingManager] Database reference is null!");
            return new List<RankingData>();
        }
        
        List<RankingData> rankings = new List<RankingData>();
        
        try
        {
            // 모든 랭킹 데이터 가져오기
            var snapshot = await databaseReference
                .Child(RANKING_PATH)
                .GetValueAsync();
            
            if (snapshot.Exists)
            {
                foreach (var childSnapshot in snapshot.Children)
                {
                    string json = childSnapshot.GetRawJsonValue();
                    RankingData data = JsonUtility.FromJson<RankingData>(json);
                    rankings.Add(data);
                }
                
                // 클리어 시간 기준으로 정렬 (오름차순 - 빠른 시간이 1등)
                rankings.Sort((a, b) => a.clearTime.CompareTo(b.clearTime));
                if (!ascending)
                {
                    rankings.Reverse();
                }
                
                Debug.Log($"[RankingManager] 전체 랭킹 {rankings.Count}개 로드 완료");
            }
            else
            {
                Debug.LogWarning("[RankingManager] 랭킹 데이터가 없습니다.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[RankingManager] 랭킹 로드 실패: {e.Message}");
        }
        
        return rankings;
    }
    
    /// <summary>
    /// 상위 N명의 랭킹 가져오기
    /// </summary>
    public async Task<List<RankingData>> GetTopRankings(int count = 5)
    {
        // 전체 랭킹을 로드한 뒤 상위 N개만 반환
        var all = await GetAllRankings(true);
        if (all == null) return new List<RankingData>();
        if (all.Count <= count) return all;
        return all.GetRange(0, count);
    }
    
    /// <summary>
    /// 특정 사용자의 기록 가져오기
    /// </summary>
    public async Task<RankingData> GetUserRecord(string email)
    {
        if (databaseReference == null)
        {
            Debug.LogError("[RankingManager] Database reference is null!");
            return null;
        }
        
        try
        {
            string sanitizedEmail = SanitizeEmail(email);
            
            var snapshot = await databaseReference
                .Child(RANKING_PATH)
                .Child(sanitizedEmail)
                .GetValueAsync();
            
            if (snapshot.Exists)
            {
                string json = snapshot.GetRawJsonValue();
                RankingData data = JsonUtility.FromJson<RankingData>(json);
                Debug.Log($"[RankingManager] 사용자 기록 로드: {email}, {data.formattedTime}");
                return data;
            }
            else
            {
                Debug.LogWarning($"[RankingManager] {email}의 기록을 찾을 수 없습니다.");
                return null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[RankingManager] 사용자 기록 로드 실패: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// 이메일을 Firebase 키로 사용 가능하게 변환
    /// Firebase 키에는 . $ # [ ] / 문자 사용 불가
    /// </summary>
    private string SanitizeEmail(string email)
    {
        return email.Replace(".", "_")
                    .Replace("$", "_")
                    .Replace("#", "_")
                    .Replace("[", "_")
                    .Replace("]", "_")
                    .Replace("/", "_");
    }
}
