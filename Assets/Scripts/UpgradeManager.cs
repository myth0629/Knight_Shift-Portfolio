using TMPro;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public WeaponData[] allWeapons;
    public WeaponDisplay[] displaySlots;

    void Start()
    {
        ShowRandomUpgrades();
    }

    public void ShowRandomUpgrades()
    {
        var selected = new WeaponData[displaySlots.Length];
        var usedIndices = new System.Collections.Generic.List<int>();

        for (int i = 0; i < displaySlots.Length; i++)
        {
            int randIndex;
            do {
                randIndex = Random.Range(0, allWeapons.Length);
            } while (usedIndices.Contains(randIndex));

            usedIndices.Add(randIndex);
            selected[i] = allWeapons[randIndex];

            displaySlots[i].SetWeapon(selected[i].weaponModelPrefab);
        }
    }
}