using System.Runtime.CompilerServices;
using UnityEngine;

public class Sword : MonoBehaviour
{
    public WeaponData weaponData;

    void Update()
    {
        Debug.Log(weaponData.damage);
    }

    public void Attack()
    {
        // 공통 로직
    }
}
