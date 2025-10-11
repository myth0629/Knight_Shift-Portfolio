using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// 간단한 UI 패널을 IUIPanel로 래핑하는 클래스
/// GameObject를 직접 관리하는 패널에 사용
/// </summary>
public class SimpleUIPanel : MonoBehaviour, IUIPanel
{
    [SerializeField] private GameObject panelObject;
    [SerializeField] private bool manageCursor = true;
    [SerializeField] private bool pauseGame = false;
    
    private CinemachineCamera vcam;

    private void Start()
    {
        vcam = FindFirstObjectByType<CinemachineCamera>();
        
        // UIPanelManager에 등록
        if (UIPanelManager.Instance != null)
        {
            UIPanelManager.Instance.RegisterPanel(this);
        }
    }

    private void OnDestroy()
    {
        // UIPanelManager에서 등록 해제
        if (UIPanelManager.Instance != null)
        {
            UIPanelManager.Instance.UnregisterPanel(this);
        }
    }

    /// <summary>
    /// 패널 열기
    /// </summary>
    public void Open()
    {
        if (panelObject == null) return;

        panelObject.SetActive(true);

        if (manageCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (pauseGame)
        {
            Time.timeScale = 0f;
        }

        if (vcam != null)
        {
            vcam.gameObject.SetActive(false);
        }

        // UIPanelManager에 패널이 열렸음을 알림
        if (UIPanelManager.Instance != null)
        {
            UIPanelManager.Instance.OnPanelOpened(this);
        }
    }

    /// <summary>
    /// 패널 닫기
    /// </summary>
    public void Close()
    {
        if (panelObject == null) return;

        panelObject.SetActive(false);

        if (manageCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (pauseGame)
        {
            Time.timeScale = 1f;
        }

        if (vcam != null)
        {
            vcam.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 패널 토글
    /// </summary>
    public void Toggle()
    {
        if (IsOpen())
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    // IUIPanel 인터페이스 구현
    public bool IsOpen()
    {
        return panelObject != null && panelObject.activeSelf;
    }

    /// <summary>
    /// 패널 오브젝트를 코드에서 설정
    /// </summary>
    public void SetPanelObject(GameObject panel)
    {
        panelObject = panel;
    }
}
