using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 캠프 씬 강화 NPC용 스크립트
// - 플레이어가 일정 거리 내에서 E 키를 누르면 강화 패널 노출
// - 현재 무기 이름과 강화 레벨 표시
// - 업그레이드 버튼: 현재 무기 강화 레벨 +1
// - 취소 버튼: 패널 닫기
// - 패널 열림/닫힘 시 커서/카메라/입력 상태 전환
public class UpgradeNpc : MonoBehaviour, IUIPanel
{
    [Header("상호작용 설정")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("UI")]
    [Tooltip("강화 패널 (Canvas 하위 GameObject)")]
    public GameObject upgradePanel;
    [Tooltip("현재 무기와 강화 레벨을 표시할 텍스트")]
    public TextMeshProUGUI currentWeaponText;
    [Tooltip("업그레이드 후 무기와 강화 레벨을 표시할 텍스트")]
    public TextMeshProUGUI nextWeaponText;
    [Tooltip("현재 무기 공격력 텍스트")]
    public TextMeshProUGUI currentAttackText;
    [Tooltip("업그레이드 후 무기 공격력 텍스트")]
    public TextMeshProUGUI nextAttackText;
    [Tooltip("업그레이드 비용 표시 텍스트")]
    public TextMeshProUGUI costText;
    [Tooltip("안내/오류 메시지 텍스트")]
    public TextMeshProUGUI messageText;
    [Tooltip("업그레이드 실행 버튼")]
    public Button upgradeButton;

    [Header("강화 설정")]
    [SerializeField] private int maxLevel = 5; // 최대 강화 레벨
    [SerializeField] private int[] levelCosts = new int[] { 100, 200, 300, 400, 500 }; // 레벨별 비용 (0->1, 1->2 ...)
    [SerializeField, Tooltip("레벨당 공격력 증가율 (예: 0.2 = 20%)")] private float upgradeRate = 0.2f;

    private Transform playerTransform;
    private PlayerInput input;
    private CinemachineCamera vcam;
    private WeaponManager weaponManager;
    private WeaponUpgradeSystem upgradeSystem;
    private StatusUI statusUI;
    private PlayerDataManager playerData;
    private PlayerUI playerUI;

    private bool isPanelOpen = false;

    private void Start()
    {
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
        {
            playerTransform = playerGo.transform;
        }

        input = FindFirstObjectByType<PlayerInput>();
        vcam = FindFirstObjectByType<CinemachineCamera>();
    statusUI = FindFirstObjectByType<StatusUI>();
    playerData = FindFirstObjectByType<PlayerDataManager>();
    playerUI = FindFirstObjectByType<PlayerUI>();

        weaponManager = WeaponManager.Instance != null ? WeaponManager.Instance : FindFirstObjectByType<WeaponManager>();
        upgradeSystem = WeaponUpgradeSystem.Instance != null ? WeaponUpgradeSystem.Instance : FindFirstObjectByType<WeaponUpgradeSystem>();

        if (upgradePanel != null) upgradePanel.SetActive(false);
        
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

    private void Update()
    {
        if (playerTransform == null) return;

        if (!isPanelOpen && Vector3.Distance(transform.position, playerTransform.position) <= interactionDistance)
        {
            if (Input.GetKeyDown(interactKey))
            {
                OpenPanel();
            }
        }
    }

    private void OpenPanel()
    {
        if (upgradePanel == null) return;
        isPanelOpen = true;
        upgradePanel.SetActive(true);

        // 커서 표시
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 카메라/입력 비활성
        if (vcam != null) vcam.gameObject.SetActive(false);
        if (input != null) input.enabled = false;

        // 일시정지
        Time.timeScale = 0f;

        RefreshPanelUI();
        
        // UIPanelManager에 패널이 열렸음을 알림
        if (UIPanelManager.Instance != null)
        {
            UIPanelManager.Instance.OnPanelOpened(this);
        }
    }

    public void ClosePanel()
    {
        if (upgradePanel == null) return;
        isPanelOpen = false;
        upgradePanel.SetActive(false);

        // 커서 숨김
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 카메라/입력 복구
        if (vcam != null) vcam.gameObject.SetActive(true);
        if (input != null) input.enabled = true;

        // 재개
        Time.timeScale = 1f;
    }

    // IUIPanel 인터페이스 구현
    public bool IsOpen()
    {
        return isPanelOpen && upgradePanel != null && upgradePanel.activeSelf;
    }

    public void Close()
    {
        ClosePanel();
    }

    private void RefreshPanelUI()
    {
        // 무기/레벨 텍스트 및 공격력 표시
        var weapon = weaponManager != null ? weaponManager.currentWeapon : null;
        var data = weapon != null ? weapon.weaponData : null;

        if (data == null)
        {
            if (currentWeaponText != null) currentWeaponText.text = "무기 없음";
            if (nextWeaponText != null) nextWeaponText.text = "-";
            if (costText != null) costText.text = "-";
            if (upgradeButton != null) upgradeButton.interactable = false;
            if (currentAttackText != null) currentAttackText.text = "-";
            if (nextAttackText != null) nextAttackText.text = "-";
            return;
        }

        int level = upgradeSystem != null ? upgradeSystem.GetLevel(data) : 0;
        if (currentWeaponText != null) currentWeaponText.text = $"{data.weaponName} (+{level})";

        // 다음 레벨 무기 표기
        if (nextWeaponText != null)
        {
            if (level >= maxLevel)
                nextWeaponText.text = "최대 레벨";
            else
                nextWeaponText.text = $"{data.weaponName} (+{level + 1})";
        }

        // 현재/다음 공격력 계산 (레벨당 20%)
        float baseAttack = data.damage;
        float currentAttack = baseAttack * (1f + level * upgradeRate);
        int nextLevel = Mathf.Min(level + 1, maxLevel);
        float nextAttack = baseAttack * (1f + nextLevel * upgradeRate);

        if (currentAttackText != null)
            currentAttackText.text = $"ATK: {Mathf.RoundToInt(currentAttack)}";
        if (nextAttackText != null)
        {
            if (level >= maxLevel)
                nextAttackText.text = "업그레이드 불가(최대)";
            else
                nextAttackText.text = $"ATK: {Mathf.RoundToInt(nextAttack)}";
        }

        // 비용/버튼 상태
        int cost = GetCostForNextLevel(level);
        if (costText != null)
        {
            if (level >= maxLevel)
                costText.text = "최대 레벨";
            else
                costText.text = $"비용: {cost} G";
        }

        if (upgradeButton != null)
        {
            bool canUpgrade = level < maxLevel && playerData != null && playerData.Gold >= cost;
            upgradeButton.interactable = canUpgrade;
        }

        // 메시지는 필요 시 유지 (자동 초기화하지 않음)
    }

    // 업그레이드 버튼 콜백
    public void OnUpgrade()
    {
        var weapon = weaponManager != null ? weaponManager.currentWeapon : null;
        var data = weapon != null ? weapon.weaponData : null;
        if (data == null || upgradeSystem == null) return;

        int currentLevel = upgradeSystem.GetLevel(data);
        if (currentLevel >= maxLevel)
        {
            SetMessage("최대 레벨에 도달했습니다.");
            RefreshPanelUI();
            return;
        }

        int cost = GetCostForNextLevel(currentLevel);
        if (playerData == null)
        {
            SetMessage("플레이어 데이터가 없습니다.");
            return;
        }
        if (!playerData.SpendGold(cost))
        {
            SetMessage("골드가 부족합니다.");
            RefreshPanelUI();
            return;
        }

        int newLevel = upgradeSystem.Upgrade(data, 1);
        SetMessage($"강화 성공! +{currentLevel} -> +{newLevel}");

        // 골드 UI 갱신
        playerUI?.UpdateGold();

        // 스테이터스 UI 갱신 (공격력/표시 등)
        statusUI?.ForceUpdateWeaponInfo();

        // 패널 내 표시도 최신화
        RefreshPanelUI();
    }

    // 취소 버튼 콜백
    public void OnCancel()
    {
        ClosePanel();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }

    private int GetCostForNextLevel(int currentLevel)
    {
        if (currentLevel >= maxLevel) return 0;
        // Array 인덱스: 0->1 비용은 levelCosts[0]
        if (levelCosts != null && currentLevel >= 0 && currentLevel < levelCosts.Length)
        {
            return levelCosts[currentLevel];
        }
        // 배열이 짧으면 기본값 패턴 적용: 100 * (다음 레벨)
        return 100 * (currentLevel + 1);
    }

    private void SetMessage(string text)
    {
        if (messageText != null)
        {
            messageText.text = text;
        }
    }
}
