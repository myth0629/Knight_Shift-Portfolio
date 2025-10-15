using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class StatusUI : MonoBehaviour, IUIPanel
{
    [Header("스테이터스 UI 요소들")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI spText;
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI weaponAttackText;
    public TextMeshProUGUI weaponTierText; // 무기 티어 텍스트 추가
    public TextMeshProUGUI stageText; // 스테이지 표시 텍스트 추가
    
    [Header("패널")]
    public GameObject statusPanel;
    
    private PlayerStatus playerStatus;
    private WeaponManager weaponManager;
    private WeaponUpgradeSystem upgradeSystem;

    // 임시 레벨 시스템 (현재 레벨 시스템이 없으므로)
    [Header("임시 레벨 설정")]
    public int playerLevel = 1;
    private AudioSource uiSound;
    
    void Start()
    {
        // 컴포넌트 참조 가져오기
        playerStatus = FindFirstObjectByType<PlayerStatus>();

        // UIPanelManager에 등록
        if (UIPanelManager.Instance != null)
        {
            UIPanelManager.Instance.RegisterPanel(this);
        }
        uiSound = FindFirstObjectByType<WeaponManager>().GetComponent<AudioSource>();
    }

    private void OnDestroy()
    {
        // UIPanelManager에서 등록 해제
        if (UIPanelManager.Instance != null)
        {
            UIPanelManager.Instance.UnregisterPanel(this);
        }
    }
    
    private void InitializeWeaponReferences()
    {
        if (weaponManager == null)
        {
            weaponManager = WeaponManager.Instance;
        }
        if (upgradeSystem == null)
        {
            upgradeSystem = WeaponUpgradeSystem.Instance;
        }
        
        // 디버그 로그로 초기화 상태 확인
        Debug.Log($"WeaponManager: {(weaponManager != null ? "OK" : "NULL")}");
        Debug.Log($"UpgradeSystem: {(upgradeSystem != null ? "OK" : "NULL")}");
        if (weaponManager != null && weaponManager.currentWeapon != null)
        {
            Debug.Log($"Current Weapon: {weaponManager.currentWeapon.weaponData?.weaponName}");
        }
    }
    
    void Update()
    {
        // 무기 매니저가 아직 초기화되지 않은 경우 계속 시도
        if (weaponManager == null && WeaponManager.Instance != null)
        {
            weaponManager = WeaponManager.Instance;
            Debug.Log("WeaponManager 늦은 초기화 완료");
        }
        
        if (upgradeSystem == null && WeaponUpgradeSystem.Instance != null)
        {
            upgradeSystem = WeaponUpgradeSystem.Instance;
            Debug.Log("WeaponUpgradeSystem 늦은 초기화 완료");
        }
        
        // 스테이지 표시 업데이트
        UpdateStageDisplay();
    }
    
    /// <summary>
    /// 스테이지 표시 업데이트
    /// </summary>
    private void UpdateStageDisplay()
    {
        if (stageText != null && StageManager.Instance != null)
        {
            stageText.text = $"Stage {StageManager.Instance.CurrentStage}";
        }
    }
    
    public void ToggleStatusPanel()
    {
        if (statusPanel != null)
        {
            bool isActive = !statusPanel.activeSelf;
            statusPanel.SetActive(isActive);
            
            if (isActive)
            {
                // 패널을 열 때마다 PlayerStatus 참조 확인
                if (playerStatus == null)
                {
                    playerStatus = FindFirstObjectByType<PlayerStatus>();
                    Debug.Log("패널 열기: PlayerStatus 참조 갱신");
                }
                
                UpdateStatusDisplay();
            
                uiSound.PlayOneShot(uiSound.clip);
                
                // UIPanelManager에 패널이 열렸음을 알림
                if (UIPanelManager.Instance != null)
                {
                    UIPanelManager.Instance.OnPanelOpened(this);
                }
            }
        }
    }

    // IUIPanel 인터페이스 구현
    public bool IsOpen()
    {
        return statusPanel != null && statusPanel.activeSelf;
    }

    public void Close()
    {
        if (statusPanel != null && statusPanel.activeSelf)
        {
            statusPanel.SetActive(false);
            
            // 마우스 커서 숨기고 잠금
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    
    public void UpdateStatusDisplay()
    {
        // PlayerStatus가 없으면 다시 찾기 시도
        if (playerStatus == null)
        {
            playerStatus = FindFirstObjectByType<PlayerStatus>();
            Debug.Log("PlayerStatus 늦은 초기화 시도");
        }
        
        // 레벨 표시
        if (levelText != null)
        {
            levelText.text = $"Lv.{playerLevel}";
        }
        
        // HP 표시
        if (hpText != null && playerStatus != null)
        {
            hpText.text = $"HP : {Mathf.RoundToInt(playerStatus.currentHp)}/{playerStatus.maxHp}";
            Debug.Log($"HP 업데이트: {playerStatus.currentHp}/{playerStatus.maxHp}");
        }
        else if (hpText != null && playerStatus == null)
        {
            Debug.LogWarning("PlayerStatus를 찾을 수 없습니다!");
            hpText.text = "HP : 0/0";
        }
        
        // SP 표시
        if (spText != null && playerStatus != null)
        {
            spText.text = $"SP : {Mathf.RoundToInt(playerStatus.currentSp)}/{playerStatus.maxSp}";
            Debug.Log($"SP 업데이트: {playerStatus.currentSp}/{playerStatus.maxSp}");
        }
        else if (spText != null && playerStatus == null)
        {
            Debug.LogWarning("PlayerStatus를 찾을 수 없습니다!");
            spText.text = "SP : 0/0";
        }
        
        // 무기 정보 표시
        UpdateWeaponDisplay();
    }
    
    private void UpdateWeaponDisplay()
    {
        // 무기 매니저 참조가 없으면 다시 찾기 시도
        if (weaponManager == null)
        {
            weaponManager = WeaponManager.Instance;
        }
        if (upgradeSystem == null)
        {
            upgradeSystem = WeaponUpgradeSystem.Instance;
        }
        
        if (weaponManager != null && weaponManager.currentWeapon != null)
        {
            var weaponData = weaponManager.currentWeapon.weaponData;
            
            if (weaponData != null)
            {
                // 무기 강화 레벨 가져오기
                int upgradeLevel = 0;
                if (upgradeSystem != null)
                {
                    upgradeLevel = upgradeSystem.GetLevel(weaponData);
                }
                
                // 무기 이름 + 강화도 표시
                if (weaponNameText != null)
                {
                    string weaponDisplayName = weaponData.weaponName;
                    if (upgradeLevel > 0)
                    {
                        weaponDisplayName += $" +{upgradeLevel}";
                    }
                    weaponNameText.text = weaponDisplayName;
                }
                
                // 무기 티어 표시
                if (weaponTierText != null)
                {
                    weaponTierText.text = $"Tier {weaponData.tier}";
                }
                
                // 무기 공격력 계산 및 표시 (실제 데미지와 동일하게 계산)
                if (weaponAttackText != null)
                {
                    // Weapon.GetActualDamage()와 동일한 계산 방식 사용
                    float baseAttack = weaponData.damage;
                    // 강화에 따른 공격력 증가 (강화 1당 20% 증가)
                    float enhancedAttack = baseAttack * (1f + (upgradeLevel * 0.2f));
                    
                    weaponAttackText.text = $"ATK : {Mathf.RoundToInt(enhancedAttack)}";
                }
                
                Debug.Log($"무기 정보 업데이트: {weaponData.weaponName} +{upgradeLevel}, Tier {weaponData.tier}, ATK: {weaponData.damage * (1f + (upgradeLevel * 0.2f))}");
            }
            else
            {
                Debug.LogWarning("WeaponData가 null입니다!");
                SetDefaultWeaponDisplay();
            }
        }
        else
        {
            Debug.LogWarning($"WeaponManager: {(weaponManager != null ? "OK" : "NULL")}, CurrentWeapon: {(weaponManager?.currentWeapon != null ? "OK" : "NULL")}");
            SetDefaultWeaponDisplay();
        }
    }
    
    private void SetDefaultWeaponDisplay()
    {
        // 무기가 없는 경우 기본값 표시
        if (weaponNameText != null)
        {
            weaponNameText.text = "Weapon + 0";
        }
        if (weaponTierText != null)
        {
            weaponTierText.text = "Tier 0";
        }
        if (weaponAttackText != null)
        {
            weaponAttackText.text = "ATK : 0";
        }
    }
    
    public void CloseStatusPanel()
    {
        if (statusPanel != null)
        {
            statusPanel.SetActive(false);
            
            // 마우스 커서 숨기고 잠금
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            // 게임 재시작
            Time.timeScale = 1f;
        }
    }
    
    // 외부에서 무기 정보 강제 업데이트용
    public void ForceUpdateWeaponInfo()
    {
        // 무기 매니저 재초기화 시도
        if (weaponManager == null)
        {
            weaponManager = WeaponManager.Instance;
        }
        if (upgradeSystem == null)
        {
            upgradeSystem = WeaponUpgradeSystem.Instance;
        }
        
        // 무기 정보 업데이트
        UpdateWeaponDisplay();
        
        Debug.Log("무기 정보 강제 업데이트 완료");
    }
}