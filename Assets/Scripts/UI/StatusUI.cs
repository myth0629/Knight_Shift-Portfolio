using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class StatusUI : MonoBehaviour
{
    [Header("스테이터스 UI 요소들")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI spText;
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI weaponAttackText;
    
    [Header("패널")]
    public GameObject statusPanel;
    
    private PlayerStatus playerStatus;
    private WeaponManager weaponManager;
    private WeaponUpgradeSystem upgradeSystem;
    
    // 임시 레벨 시스템 (현재 레벨 시스템이 없으므로)
    [Header("임시 레벨 설정")]
    public int playerLevel = 1;
    
    void Start()
    {
        // 컴포넌트 참조 가져오기
        playerStatus = FindFirstObjectByType<PlayerStatus>();
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
    }
    
    public void ToggleStatusPanel()
    {
        if (statusPanel != null)
        {
            bool isActive = !statusPanel.activeSelf;
            statusPanel.SetActive(isActive);
            
            if (isActive)
            {
                UpdateStatusDisplay();
                
                // 마우스 커서 보이게 & 잠금 해제
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                // 마우스 커서 숨기고 잠금
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
    
    public void UpdateStatusDisplay()
    {
        // 레벨 표시
        if (levelText != null)
        {
            levelText.text = $"Lv.{playerLevel}";
        }
        
        // HP 표시
        if (hpText != null && playerStatus != null)
        {
            hpText.text = $"HP : {Mathf.RoundToInt(playerStatus.currentHp)}/{playerStatus.maxHp}";
        }
        
        // SP 표시
        if (spText != null && playerStatus != null)
        {
            spText.text = $"SP : {Mathf.RoundToInt(playerStatus.currentSp)}/{playerStatus.maxSp}";
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
                    weaponNameText.text = $"{weaponDisplayName} + {upgradeLevel}";
                }
                
                // 무기 공격력 계산 및 표시
                if (weaponAttackText != null)
                {
                    float baseAttack = weaponData.damage;
                    // 강화에 따른 공격력 증가 (예: 강화 1당 10% 증가)
                    float enhancedAttack = baseAttack * (1 + (upgradeLevel * 0.1f));
                    
                    weaponAttackText.text = $"ATK : {Mathf.RoundToInt(enhancedAttack)}";
                }
                
                Debug.Log($"무기 정보 업데이트: {weaponData.weaponName} +{upgradeLevel}, ATK: {weaponData.damage * (1 + (upgradeLevel * 0.1f))}");
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
    
    // 레벨업 함수 (추후 레벨 시스템 구현 시 사용)
    public void LevelUp()
    {
        playerLevel++;
        if (statusPanel.activeSelf)
        {
            UpdateStatusDisplay();
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