using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

/// <summary>
/// UIManager가 관리하는 패널을 IUIPanel로 래핑
/// SlotPanel(무기 업그레이드)와 ShopPanel을 ESC로 닫을 수 있게 함
/// </summary>
public class UIManagerPanelWrapper : MonoBehaviour, IUIPanel
{
    [SerializeField] private GameObject targetPanel;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private bool isSlotPanel = true; // true: SlotPanel, false: ShopPanel
    
    private CinemachineCamera vcam;
    private PlayerInput input;

    private void Start()
    {
        vcam = FindFirstObjectByType<CinemachineCamera>();
        input = FindFirstObjectByType<PlayerInput>();
        
        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<UIManager>();
        }
        
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

    public void OnPanelOpened()
    {
        // UIPanelManager에 패널이 열렸음을 알림
        if (UIPanelManager.Instance != null)
        {
            UIPanelManager.Instance.OnPanelOpened(this);
        }
    }

    // IUIPanel 인터페이스 구현
    public bool IsOpen()
    {
        return targetPanel != null && targetPanel.activeSelf;
    }

    public void Close()
    {
        if (targetPanel == null || !targetPanel.activeSelf) return;
        
        targetPanel.SetActive(false);
        
        // 커서 숨기고 잠금
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // 게임 재개
        if (isSlotPanel)
        {
            Time.timeScale = 1f;
        }
        
        // 카메라 활성화
        if (vcam != null)
        {
            vcam.gameObject.SetActive(true);
        }
        
        // 입력 활성화 (ShopPanel의 경우)
        if (!isSlotPanel && input != null)
        {
            input.enabled = true;
        }
    }
}
