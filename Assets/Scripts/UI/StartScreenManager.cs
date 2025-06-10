using UnityEngine;
using UnityEngine.SceneManagement;

public class StartScreenUIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject optionUI;
    public GameObject soundUI;
    public GameObject graphicUI;
    public GameObject displayUI;

    private bool isOptionOpen = false;

    // ▶ 환경설정 버튼 클릭 시 호출
    public void ToggleOptionUI()
    {
        // 설정 창 켜기/끄기
        if (IsAnySubSettingOpen())
        {
            CloseAllSettings();
            return;
        }

        isOptionOpen = !isOptionOpen;
        optionUI.SetActive(isOptionOpen);
    }

    // ▶ 소리/밝기 버튼 클릭 시
    public void OpenSoundBrightSettings()
    {
        ShowOnlySetting(soundUI);
    }

    // ▶ 그래픽 버튼 클릭 시
    public void OpenGraphicSettings()
    {
        ShowOnlySetting(graphicUI);
    }

    // ▶ 디스플레이 버튼 클릭 시
    public void OpenDisplaySettings()
    {
        ShowOnlySetting(displayUI);
    }

    // ▶ 내부에서 하나만 켜기
    private void ShowOnlySetting(GameObject targetUI)
    {
        optionUI.SetActive(false);
        soundUI.SetActive(false);
        graphicUI.SetActive(false);
        displayUI.SetActive(false);

        targetUI.SetActive(true);
        isOptionOpen = false; // optionUI는 닫힌 것으로 간주
    }

    // ▶ 소리/그래픽/디스플레이 중 하나라도 켜져있나?
    private bool IsAnySubSettingOpen()
    {
        return soundUI.activeSelf || graphicUI.activeSelf || displayUI.activeSelf;
    }

    // ▶ 서브 설정 창 모두 닫기
    private void CloseAllSettings()
    {
        soundUI.SetActive(false);
        graphicUI.SetActive(false);
        displayUI.SetActive(false);
    }

    // ▶ 게임 시작 버튼 (예정)
    public void StartGame()
    {
        // 추후 SceneManager.LoadScene("GameScene") 등으로 추가
    }

    // ▶ 게임 종료 버튼 (예정)
    public void QuitGame()
    {
        Application.Quit();
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene("Main");
        gameObject.SetActive(false);
    }
    
    public void ClosePanel(GameObject panel)
    {
        panel.SetActive(false);
    }
    
    public void StartSceneLoad()
    {
        SceneManager.LoadScene("Start");
    }
}
