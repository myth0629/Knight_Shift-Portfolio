using TMPro;
using Unity.Cinemachine;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject uiPanel;  // 켜고 끌 UI 패널
    public CinemachineCamera vcam;
    
    private bool isPanelOpen = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            ToggleUIPanel();
        }
    }

    public void ToggleUIPanel()
    {
        isPanelOpen = !isPanelOpen;
        uiPanel.SetActive(isPanelOpen);

        if (isPanelOpen)
        {
            // 마우스 커서 보이게 & 잠금 해제
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 게임 일시정지 (필요 시)
            Time.timeScale = 0f;
            
            // 카메라 시점 이동 금지
            vcam.gameObject.SetActive(false);
        }
        else
        {
            // 마우스 커서 숨기고 잠금
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // 게임 재시작
            Time.timeScale = 1f;
            
            // 카메라 시점 이동 허용
            vcam.gameObject.SetActive(true);
        }
    }
}