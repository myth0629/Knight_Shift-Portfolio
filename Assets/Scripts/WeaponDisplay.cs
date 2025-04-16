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

    private WeaponManager weaponManager;

    public void Init(WeaponManager manager)
    {
        weaponManager = manager;
    }

    public void SetWeapon(GameObject weaponPrefab)
    {
        assignedWeaponPrefab = weaponPrefab;

        // 기존 무기 제거
        if (spawnedWeapon != null)
        {
            Destroy(spawnedWeapon);
        }

        // 무기 생성 및 부모 설정
        spawnedWeapon = Instantiate(weaponPrefab, weaponParent);
        spawnedWeapon.transform.localPosition = Vector3.zero;
        spawnedWeapon.transform.localRotation = Quaternion.identity;

        // 무기 이름 표시
        Weapon weapon = spawnedWeapon.GetComponent<Weapon>();
        if (weapon != null)
        {
            weaponNameText.text = weapon.weaponData.weaponName;
        }
        else
        {
            weaponNameText.text = "Unknown Weapon";
        }

        // selectButton.onClick.RemoveAllListeners();
        // selectButton.onClick.AddListener(() => upgradeManager.SelectWeapon(assignedWeaponPrefab));
    }
}