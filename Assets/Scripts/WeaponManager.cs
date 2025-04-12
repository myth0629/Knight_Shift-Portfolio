using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponManager : MonoBehaviour
{
    public Transform weaponHolder;
    private Weapon currentWeapon;
    [SerializeField] GameObject weaponPrefab;

    void Update()
    {
        // 테스트용 (추후 수정)
        if(Input.GetKeyDown(KeyCode.Q))
        {
            EquipWeapon(weaponPrefab);
            Debug.Log("무기 변경");
        }
    } 

    public void EquipWeapon(GameObject weaponPrefab)
    {
        foreach(Transform child in weaponHolder)
        {
            Destroy(child.gameObject);
        }

        GameObject newWeapon = Instantiate(weaponPrefab, weaponHolder);
        newWeapon.transform.localPosition = Vector3.zero;
        newWeapon.transform.localRotation = Quaternion.identity;

        currentWeapon = newWeapon.GetComponent<Weapon>();
        if(currentWeapon == null)
        {
            Debug.LogWarning("무기 프리팹에 Weapon 스크립트 없음");
        }
    }
}
