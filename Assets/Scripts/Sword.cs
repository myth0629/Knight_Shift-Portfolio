using UnityEngine;

public class Sword : Weapon
{
    public WeaponData weaponData;

    void Start()
    {
        if(weaponData != null)
        {
            weaponName = weaponData.weaponName;
            damage = weaponData.damage;
        }
    }

    public override void Attack()
    {
        Debug.Log("Sword attack with damage: " + weaponData.damage);
    }
}
