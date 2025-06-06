using Unity.Cinemachine;
using UnityEngine;

/// UI 전체를 관리하는 매니저 클래스.
/// ESC로 옵션 UI 토글 + 추후 UI 확장 고려.

public class ButtonUIManager : MonoBehaviour
{

    [Header("UI References")]
    public GameObject optionUI;
    public GameObject soundbrightsettingUI;
    public GameObject graphicUI; 
    public GameObject displayUI; 

    public CinemachineCamera cinemachineCamera;

    private bool isOptionOpen = false;

    void Update()
    {
        HandleEscapeKey();
    }

    /// ESC 키 입력 처리
    private void HandleEscapeKey()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleOptionUI();
        }
    }

    /// 옵션 UI 표시/비표시 전환
    private void ToggleOptionUI()
    {

  // 👉 소리,밝기가 켜져있으면 닫고 종료
    if (soundbrightsettingUI.activeSelf)
    {
        soundbrightsettingUI.SetActive(false);
        SetCursorState(false);
        SetGamePauseState(false);
        return;
    }

     // 👉 GraphicUI가 켜져있으면 닫고 종료
    if (graphicUI.activeSelf)
    {
        graphicUI.SetActive(false);
        SetCursorState(false);
        SetGamePauseState(false);
        return;
    }

    // 👉 DisplayUI가 켜져있으면 닫고 종료
    if (displayUI.activeSelf)
    {
        displayUI.SetActive(false);
        SetCursorState(false);
        SetGamePauseState(false);
        return;
    }

        isOptionOpen = !isOptionOpen;
        optionUI.SetActive(isOptionOpen);

        SetCursorState(isOptionOpen);
        SetGamePauseState(isOptionOpen);
    }



    /// 커서 상태 설정
    private void SetCursorState(bool isUIOpen)
    {
        Cursor.lockState = isUIOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isUIOpen;
    }

    /// 게임 정지/재개
    
    public void SetGamePauseState(bool isPaused)
    {
        Debug.Log("일시정지");
        Time.timeScale = isPaused ? 0f : 1f;
        cinemachineCamera.enabled = !isPaused;

    }

    

    /// ▶ 소리, 밝기 설정 UI로 전환 (버튼에서 호출)
  public void OpenSoundBrightSettings()
    {
        optionUI.SetActive(false);
        soundbrightsettingUI.SetActive(true);

        // 상태 반영: 옵션창은 닫힌 상태로 간주
        isOptionOpen = false;

        // 커서, 게임 상태는 계속 UI 모드로 유지
        SetCursorState(true);
        SetGamePauseState(true);
    }
  
    /// ▶ Graphic 설정 UI로 전환 (버튼에서 호출)
    public void OpenGraphicSettings()
    {
        optionUI.SetActive(false);
        graphicUI.SetActive(true);

        // 상태 반영: 옵션창은 닫힌 상태로 간주
        isOptionOpen = false;

        // 커서, 게임 상태는 계속 UI 모드로 유지
        SetCursorState(true);
        SetGamePauseState(true);
    }


    /// ▶ Display 설정 UI로 전환 (버튼에서 호출)
    public void OpenDisplaySettings()
    {
        optionUI.SetActive(false);
        displayUI.SetActive(true);

        // 상태 반영: 옵션창은 닫힌 상태로 간주
        isOptionOpen = false;

        // 커서, 게임 상태는 계속 UI 모드로 유지
        SetCursorState(true);
        SetGamePauseState(true);
    }
}