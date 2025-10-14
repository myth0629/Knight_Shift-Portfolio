using UnityEngine;
using TMPro; // TextMeshPro 네임스페이스 사용

/// <summary>
/// 씬에 있는 살아있는 몬스터의 수를 세어 UI 텍스트에 표시하는 클래스입니다.
/// </summary>
public class EnemyCount : MonoBehaviour
{
    [Header("UI 설정")]
    [Tooltip("현재 몬스터 마릿수를 표시할 TextMeshProUGUI 컴포넌트를 여기에 연결하세요.")]
    public TextMeshProUGUI monsterCountText;

    [Header("몬스터 설정")]
    [Tooltip("몬스터로 인식할 게임 오브젝트의 태그입니다.")]
    public string monsterTag = "Enemy"; // 대부분의 프로젝트에서 'Enemy' 태그를 사용합니다.

    void Update()
    {
        // monsterTag로 설정된 태그를 가진 모든 게임 오브젝트를 찾습니다.
        GameObject[] monsters = GameObject.FindGameObjectsWithTag(monsterTag);
        
        int aliveCount = 0;
        // 찾은 모든 몬스터를 순회합니다.
        foreach (GameObject monster in monsters)
        {
            // 각 몬스터의 EnemyController 스크립트를 가져옵니다.
            EnemyController controller = monster.GetComponent<EnemyController>();

            // EnemyController 스크립트가 있고, isDead 상태가 아니라면 (살아있다면)
            if (controller != null && !controller.isDead)
            {
                aliveCount++; // 살아있는 몬스터 수를 1 증가시킵니다.
            }
        }

        // UI 텍스트가 할당되었다면, 살아있는 몬스터 수로 텍스트 내용을 업데이트합니다.
        if (monsterCountText != null)
        {
            monsterCountText.text = "남은 몬스터: " + aliveCount;
        }
    }
}