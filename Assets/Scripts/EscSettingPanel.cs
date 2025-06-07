using UnityEngine;

public class EscSettingPanel : MonoBehaviour
{
    public GameObject settingPanel; // 설정창 연결

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool isActive = settingPanel.activeSelf;
            settingPanel.SetActive(!isActive); // ESC로 열고 닫기
        }
    }
}
