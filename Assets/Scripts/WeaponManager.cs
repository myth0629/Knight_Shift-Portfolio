using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponManager : MonoBehaviour
{
    public Transform weaponHolder;
    public Sword currentWeapon;
    [SerializeField] WeaponData currentWeaponData;
    [SerializeField] WeaponData StartWeaponData;
    [SerializeField] WeaponData testWeaponData;

    void Start()
    {
        currentWeaponData = StartWeaponData;
        EquipWeapon(currentWeaponData);
    }

    void Update()
    {
        // 테스트용 (추후 수정)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            EquipWeapon(testWeaponData);
        }
    }

    public void EquipWeapon(WeaponData newWeaponData)
    {
        // 이전 무기 제거
        if (currentWeapon != null)
        {
            Destroy(currentWeapon.gameObject);
        }

        // 새 무기 프리팹 생성 및 장착
        GameObject newWeaponObj = Instantiate(newWeaponData.weaponModelPrefab, weaponHolder);
        newWeaponObj.transform.localPosition = Vector3.zero;
        newWeaponObj.transform.localRotation = Quaternion.identity;

        // Sword 스크립트 가져오기
        currentWeapon = newWeaponObj.GetComponent<Sword>();

        // 무기 데이터 설정
        if (currentWeapon != null)
        {
            currentWeapon.weaponData = newWeaponData;
            currentWeaponData = newWeaponData; // 현재 무기 데이터도 업데이트
            Debug.Log("무기 장착: " + newWeaponData.weaponName);
        }
        else
        {
            Debug.LogWarning("무기 프리팹에 Sword 스크립트가 없습니다!");
        }
    }
}