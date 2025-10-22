using UnityEngine;
using TMPro;

/// <summary>
/// 랭킹 항목 UI 컴포넌트
/// </summary>
public class RankingEntry : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private TextMeshProUGUI rankText;      // 순위
    [SerializeField] private TextMeshProUGUI emailText;     // 이메일
    [SerializeField] private TextMeshProUGUI timeText;      // 클리어 시간
    
    /// <summary>
    /// 랭킹 항목 데이터 설정
    /// </summary>
    public void SetData(int rank, string email, string formattedTime)
    {
        if (rankText != null)
        {
            rankText.text = rank.ToString();
            
            // 1~3위 색상 설정
            switch (rank)
            {
                case 1:
                    rankText.color = new Color(1f, 0.84f, 0f); // 금색
                    break;
                case 2:
                    rankText.color = new Color(0.75f, 0.75f, 0.75f); // 은색
                    break;
                case 3:
                    rankText.color = new Color(0.8f, 0.5f, 0.2f); // 동색
                    break;
                default:
                    rankText.color = Color.white;
                    break;
            }
        }
        
        if (emailText != null)
        {
            emailText.text = email;
        }
        
        if (timeText != null)
        {
            timeText.text = formattedTime;
        }
    }
}
