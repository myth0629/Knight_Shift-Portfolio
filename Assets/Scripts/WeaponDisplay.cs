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

    public WeaponManager weaponManager;
    public UIManager uiManager;

    public void DisplaySetWeapon(WeaponData weaponData)
    {
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
            weaponNameText.text = weaponData.weaponName;
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
                weaponManager.EquipWeapon(weaponData);
                uiManager.ToggleUIPanel();
            });
        }
        else
        {
            Debug.LogWarning("무기 정보 또는 weaponManager가 null입니다.");
        }
    }
}