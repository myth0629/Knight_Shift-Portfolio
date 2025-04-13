using System.Runtime.CompilerServices;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public WeaponData weaponData;


    public void ApplyWeaponTypeToAnimator(Animator animator)
    {
        animator.SetInteger("WeaponType", (int)weaponData.weaponType);
    }

    public void Attack()
    {
        // 공통 로직
    }
}
