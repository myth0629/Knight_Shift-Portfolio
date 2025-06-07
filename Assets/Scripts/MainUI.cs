using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class MainUI : MonoBehaviour
{
    [SerializeField] GameObject loginPanel;
    
    public void ToggleLoginPanel()
    {
        if (loginPanel != null)
        {
            loginPanel.SetActive(!loginPanel.activeSelf);
        }
        else if (loginPanel.activeSelf)
        {
            // 로그인 패널이 활성화되어 있을 때
            loginPanel.SetActive(false);
        }
        else if (!loginPanel.activeSelf)
        {
            // 로그인 패널이 비활성화되어 있을 때
            loginPanel.SetActive(true);
        }
    }
}
