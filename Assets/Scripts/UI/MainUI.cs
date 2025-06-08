using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class MainUI : MonoBehaviour
{
    [SerializeField] GameObject loginPanel;
    [SerializeField] GameObject registerPanel;
    
    public void ToggleLoginPanel()
    {
        if (loginPanel != null)
        {
            loginPanel.SetActive(!loginPanel.activeSelf);
        }
    }
    
    // 공통 패널 닫기 함수
    public void ClosePanel(GameObject panel)
    {
        panel.SetActive(false);
    }

    public void OpenPanel(GameObject panel)
    {
        if (loginPanel != null && loginPanel.activeSelf)
        {
            loginPanel.SetActive(false); // 현재 UI 끄기
        }
        if (registerPanel != null && registerPanel.activeSelf)
        {
            registerPanel.SetActive(false); // 현재 UI 끄기
        }

        panel.SetActive(true); // 새 UI 켜기
    }
}
