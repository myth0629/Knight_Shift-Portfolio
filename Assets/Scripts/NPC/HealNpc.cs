using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

// 캠프 씬 회복 NPC용 스크립트
// - 플레이어가 일정 거리 내에서 E 키를 누르면 회복 확인 패널 노출
// - 확인 버튼: HP/SP를 전부 회복하고 패널 닫기
// - 취소 버튼: 패널만 닫기
// - 패널 열림/닫힘 시 커서/카메라/입력 상태 전환
public class HealNpc : MonoBehaviour
{
    [Header("상호작용 설정")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("UI")]
    [Tooltip("회복 여부를 묻는 패널 (Canvas 하위 GameObject)")]
    public GameObject healPanel;

    private Transform playerTransform;
    private PlayerStatus playerStatus;
    private PlayerUI playerUI;
    private CinemachineCamera vcam;
    private PlayerInput input;

    private bool isPanelOpen = false;
    private bool hasHealed = false; // 이 씬에서 한 번 회복했는지 여부

    private void Start()
    {
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
        {
            playerTransform = playerGo.transform;
            playerStatus = playerGo.GetComponent<PlayerStatus>();
        }

        playerUI = FindFirstObjectByType<PlayerUI>();
        vcam = FindFirstObjectByType<CinemachineCamera>();
        input = FindFirstObjectByType<PlayerInput>();

        if (healPanel != null) healPanel.SetActive(false);
    }

    private void Update()
    {
        if (playerTransform == null) return;

        if (!isPanelOpen && !hasHealed && Vector3.Distance(transform.position, playerTransform.position) <= interactionDistance)
        {
            if (Input.GetKeyDown(interactKey))
            {
                OpenPanel();
            }
        }
    }

    public void OpenPanel()
    {
        if (healPanel == null) return;
        if (hasHealed) return; // 이미 회복했다면 다시 열리지 않음
        isPanelOpen = true;
        healPanel.SetActive(true);

        // 마우스 커서 표시
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 카메라 시점 이동 금지
        if (vcam != null) vcam.gameObject.SetActive(false);
        // 플레이어 입력 차단
        if (input != null) input.enabled = false;

        // 게임 일시정지 (선택사항)
        Time.timeScale = 0f;
    }

    public void ClosePanel()
    {
        if (healPanel == null) return;
        isPanelOpen = false;
        healPanel.SetActive(false);

        // 마우스 커서 숨김
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 카메라 시점 이동 허용
        if (vcam != null) vcam.gameObject.SetActive(true);
        // 플레이어 입력 허용
        if (input != null) input.enabled = true;

        // 게임 재개
        Time.timeScale = 1f;
    }

    // 확인 버튼용 - HP/SP를 최대치로 회복
    public void OnConfirmHeal()
    {
        if (playerStatus != null)
        {
            playerStatus.currentHp = playerStatus.maxHp;
            playerStatus.currentSp = playerStatus.maxSp;
        }
        if (playerUI != null)
        {
            playerUI.UpdateUI();
        }
        hasHealed = true; // 일회성 회복 완료
        ClosePanel();
    }

    // 취소 버튼용 - 패널만 닫기
    public void OnCancel()
    {
        ClosePanel();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
