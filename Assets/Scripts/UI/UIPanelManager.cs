using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

/// <summary>
/// UI 패널의 우선순위를 관리하는 매니저
/// ESC 키를 누르면 열려있는 패널을 우선적으로 닫고, 모든 패널이 닫혀있을 때만 옵션 패널을 엽니다.
/// </summary>
public class UIPanelManager : MonoBehaviour
{
    public static UIPanelManager Instance { get; private set; }

    [Header("옵션 패널")]
    [SerializeField] private GameObject optionPanel;

    [Header("기타 설정")]
    [SerializeField] private CinemachineCamera vcam;
    public AudioSource uiSound;

    PlayerInput input;

    // 등록된 UI 패널들 (옵션 패널 제외)
    private List<IUIPanel> registeredPanels = new List<IUIPanel>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (vcam == null)
        {
            vcam = FindFirstObjectByType<CinemachineCamera>();
        }
        input = FindFirstObjectByType<PlayerInput>();
        uiSound = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscapeKey();
        }
    }

    /// <summary>
    /// ESC 키 처리: 열려있는 패널을 우선적으로 닫고, 모두 닫혀있으면 옵션 패널 토글
    /// </summary>
    private void HandleEscapeKey()
    {
        // 1. 등록된 패널 중 열려있는 패널이 있는지 확인
        IUIPanel openPanel = GetTopMostOpenPanel();

        if (openPanel != null)
        {
            // 열려있는 패널이 있으면 닫기
            openPanel.Close();
        }
        else
        {
            // 모든 패널이 닫혀있으면 옵션 패널 토글
            ToggleOptionPanel();
        }
    }

    /// <summary>
    /// 가장 위에 있는 (마지막으로 등록된) 열린 패널 반환
    /// </summary>
    private IUIPanel GetTopMostOpenPanel()
    {
        // 역순으로 검색하여 가장 최근에 열린 패널을 찾음
        for (int i = registeredPanels.Count - 1; i >= 0; i--)
        {
            if (registeredPanels[i] != null && registeredPanels[i].IsOpen())
            {
                return registeredPanels[i];
            }
        }
        return null;
    }

    /// <summary>
    /// 옵션 패널 토글
    /// </summary>
    private void ToggleOptionPanel()
    {
        if (optionPanel == null) return;

        bool isOpening = !optionPanel.activeSelf;
        optionPanel.SetActive(isOpening);

        if (isOpening)
        {
            // 옵션 패널 열기
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            vcam.gameObject.SetActive(false);
            input.enabled = false;
            uiSound.PlayOneShot(uiSound.clip);
        }
        else
        {
            // 옵션 패널 닫기
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            vcam.gameObject.SetActive(true);
            input.enabled = true;
        }
    }

    /// <summary>
    /// UI 패널 등록 (열린 순서대로 관리)
    /// </summary>
    public void RegisterPanel(IUIPanel panel)
    {
        if (!registeredPanels.Contains(panel))
        {
            registeredPanels.Add(panel);
        }
    }

    /// <summary>
    /// UI 패널 등록 해제
    /// </summary>
    public void UnregisterPanel(IUIPanel panel)
    {
        registeredPanels.Remove(panel);
    }

    /// <summary>
    /// 패널이 열릴 때 호출 (우선순위 갱신)
    /// </summary>
    public void OnPanelOpened(IUIPanel panel)
    {
        // 패널을 리스트 맨 뒤로 이동 (최근 열린 패널)
        if (registeredPanels.Contains(panel))
        {
            registeredPanels.Remove(panel);
            registeredPanels.Add(panel);
        }
    }

    /// <summary>
    /// 옵션 패널을 직접 닫기 (다른 스크립트에서 호출용)
    /// </summary>
    public void CloseOptionPanel()
    {
        if (optionPanel != null && optionPanel.activeSelf)
        {
            optionPanel.SetActive(false);
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (vcam != null) vcam.gameObject.SetActive(true);
        }
    }
}
