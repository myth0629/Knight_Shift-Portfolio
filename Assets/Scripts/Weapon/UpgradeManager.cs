using TMPro;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public WeaponData[] allWeapons;
    public WeaponDisplay[] displaySlots;

    void Start()
    {
        // 비활성화 상태인 오브젝트도 찾음
        displaySlots = FindObjectsByType<WeaponDisplay>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (displaySlots.Length == 0)
        {
            Debug.LogWarning("WeaponDisplay slots not found in scene.");
            return;
        }

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

            displaySlots[i].DisplaySetWeapon(selected[i]);
        }
    }
}