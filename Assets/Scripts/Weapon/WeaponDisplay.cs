using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WeaponDisplay : MonoBehaviour
{
    public Transform weaponParent;
    public TextMeshProUGUI weaponNameText;
    public Button selectButton;

    private GameObject spawnedWeapon;
    private GameObject assignedWeaponPrefab;
    private int upgradeLevel = 0;

    public WeaponManager weaponManager;
    UIManager uiManager;
    
    void OnEnable()
    {
        if (weaponManager == null)
        {
            weaponManager = WeaponManager.Instance;
        }

        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<UIManager>();
        }
    }

    public void DisplaySetWeapon(WeaponData weaponData)
    {
        InternalDisplaySetWeapon(weaponData, false);
    }

    // 선택창에서 현재 장착중인 무기에 대해 다음 강화 예상(+1) 표시용
    public void DisplaySetWeapon(WeaponData weaponData, bool previewNextUpgrade)
    {
        InternalDisplaySetWeapon(weaponData, previewNextUpgrade);
    }

    private void InternalDisplaySetWeapon(WeaponData weaponData, bool previewNext)
    {
        if (weaponManager == null)
        {
            weaponManager = WeaponManager.Instance;
        }
        
        assignedWeaponPrefab = weaponData.weaponModelPrefab;

        // 기존 무기 제거
        if (spawnedWeapon != null)
        {
            Destroy(spawnedWeapon);
        }

        // 무기 생성 및 부모 설정
        spawnedWeapon = Instantiate(assignedWeaponPrefab, weaponParent);
        spawnedWeapon.transform.localPosition = Vector3.zero;
        spawnedWeapon.transform.localRotation = Quaternion.identity;

        // 무기 이름 표시
        if (weaponData != null)
        {
            upgradeLevel = WeaponUpgradeSystem.Instance != null ? WeaponUpgradeSystem.Instance.GetLevel(weaponData) : 0;
            int displayLevel = upgradeLevel;
            if (previewNext && WeaponUpgradeSystem.Instance != null)
            {
                int max = WeaponUpgradeSystem.Instance.maxLevel;
                int nextLevel = upgradeLevel + 1;
                if (max > 0) nextLevel = Mathf.Min(nextLevel, max);
                displayLevel = nextLevel;
            }
            weaponNameText.text = FormatWeaponName(weaponData.weaponName, displayLevel);
        }
        else
        {
            weaponNameText.text = "Unknown Weapon";
        }
        
        
        
        if (weaponData != null && weaponManager != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() =>
            {
                // 현재 장착 무기와 같은 무기를 선택하면 강화
                var currentWeaponDataField = weaponManager.GetType().GetField("currentWeaponData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                WeaponData equipped = null;
                if (currentWeaponDataField != null)
                {
                    equipped = currentWeaponDataField.GetValue(weaponManager) as WeaponData;
                }

                if (equipped == weaponData)
                {
                    if (WeaponUpgradeSystem.Instance != null)
                    {
                        WeaponUpgradeSystem.Instance.Upgrade(weaponData, 1);
                        // 이름 재표시
                        weaponNameText.text = FormatWeaponName(weaponData.weaponName, WeaponUpgradeSystem.Instance.GetLevel(weaponData));
                    }
                }
                else
                {
                    weaponManager.EquipWeapon(weaponData);
                }

                uiManager.ToggleUpgradeUIPanel();
            });
        }
        else
        {
            Debug.LogWarning("무기 정보 또는 weaponManager가 null입니다.");
        }
    }

    private string FormatWeaponName(string baseName, int level)
    {
        if (level <= 0) return baseName;
        return $"{baseName} +{level}";
    }

    public void RefreshUpgradeLevel()
    {
        if (assignedWeaponPrefab == null || weaponNameText == null) return;
        // We don't store WeaponData directly here; caller should re-call DisplaySetWeapon if data changed.
        // This method kept for potential extension.
    }
}