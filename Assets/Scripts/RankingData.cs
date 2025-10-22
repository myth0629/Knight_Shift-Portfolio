using System;

/// <summary>
/// 랭킹 데이터 모델
/// </summary>
[Serializable]
public class RankingData
{
    public string email;
    public float clearTime; // 초 단위
    public string formattedTime; // "00분 00초" 형식
    public long timestamp; // Unix timestamp
    public int stage; // 클리어한 스테이지
    
    public RankingData()
    {
    }
    
    public RankingData(string email, float clearTime, string formattedTime, int stage)
    {
        this.email = email;
        this.clearTime = clearTime;
        this.formattedTime = formattedTime;
        this.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        this.stage = stage;
    }
}
