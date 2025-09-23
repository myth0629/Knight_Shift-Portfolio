using TMPro;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public WeaponData[] allWeapons;
    public WeaponDisplay[] displaySlots;
    [Header("Current Weapon Reference")]
    public WeaponData currentWeapon; // 현재 장착 무기 (외부에서 세팅 필요)

    [Tooltip("업그레이드 후보가 부족할 때 슬롯 숨김 대신 동일 티어 반복 허용 여부")] public bool allowDuplicatesIfInsufficient = true;

    void Start()
    {
        // 비활성화 상태인 오브젝트도 찾음
        displaySlots = FindObjectsByType<WeaponDisplay>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (displaySlots.Length == 0)
        {
            Debug.LogWarning("WeaponDisplay slots not found in scene.");
            return;
        }

        // 현재 장착 무기 자동 동기화 (외부에서 미지정 대비)
        ResolveCurrentWeaponData();

        ShowRandomUpgrades();
    }

    public void ShowRandomUpgrades()
    {
        if (displaySlots == null || displaySlots.Length == 0) return;
        if (allWeapons == null || allWeapons.Length == 0)
        {
            Debug.LogWarning("[UpgradeManager] allWeapons 비어있음");
            return;
        }

        // 장착 무기 최신화 시도
        ResolveCurrentWeaponData();

        int currentTier = currentWeapon != null ? currentWeapon.tier : 1; // 기본 1티어로 가정

        // 1) 후보 필터링 (현재 티어 또는 +1 티어)
        var candidates = new System.Collections.Generic.List<WeaponData>();
        foreach (var w in allWeapons)
        {
            if (w == null) continue;
            if (w.tier == currentTier || w.tier == currentTier + 1)
            {
                // 자신과 동일 무기라도 허용할지? 동일 프리팹 제외하려면 아래 조건 추가 가능
                candidates.Add(w);
            }
        }

        // Fallback 제거: 요구사항상 현재 티어 또는 +1만 허용. 조건에 맞는 무기가 없다면 슬롯을 비활성화 상태로 둡니다.
        if (candidates.Count == 0)
        {
            Debug.LogWarning($"[UpgradeManager] 조건(tier {currentTier} 또는 {currentTier+1})에 맞는 무기가 allWeapons에 없습니다. 슬롯을 비활성화합니다.");
        }

        // 2) 후보 수가 슬롯 수보다 적으면 중복 허용 여부 고려
        var finalList = new System.Collections.Generic.List<WeaponData>();
        var used = new System.Collections.Generic.HashSet<WeaponData>();

        System.Random rng = new System.Random();

        if (candidates.Count >= displaySlots.Length)
        {
            // 충분하면 중복 없이 랜덤 선택
            var temp = new System.Collections.Generic.List<WeaponData>(candidates);
            // Fisher–Yates shuffle 일부
            int n = temp.Count;
            while (n > 1)
            {
                int k = rng.Next(n--);
                (temp[n], temp[k]) = (temp[k], temp[n]);
            }
            for (int i = 0; i < displaySlots.Length; i++)
            {
                finalList.Add(temp[i]);
            }
        }
        else
        {
            if (!allowDuplicatesIfInsufficient)
            {
                Debug.LogWarning($"[UpgradeManager] 후보({candidates.Count}) < 슬롯({displaySlots.Length}) 이지만 중복 허용 비활성. 남은 슬롯 비움.");
            }
            for (int i = 0; i < displaySlots.Length; i++)
            {
                WeaponData pick;
                if (candidates.Count == 0)
                {
                    pick = null;
                }
                else if (i < candidates.Count)
                {
                    pick = candidates[i];
                }
                else
                {
                    // 후보를 순환하거나 중복 허용
                    pick = allowDuplicatesIfInsufficient ? candidates[UnityEngine.Random.Range(0, candidates.Count)] : null;
                }
                finalList.Add(pick);
            }
        }

        // 3) UI 반영 (자동 강화 제거: 선택 시 강화하도록 변경)
        for (int slotIndex = 0; slotIndex < displaySlots.Length; slotIndex++)
        {
            var weapon = finalList[slotIndex];
            if (weapon != null)
            {
                displaySlots[slotIndex].gameObject.SetActive(true);
                bool isCurrent = (currentWeapon != null && weapon == currentWeapon);
                // 현재 무기라면 다음 강화( +1 ) 미리보기 표기
                displaySlots[slotIndex].DisplaySetWeapon(weapon, isCurrent);
            }
            else
            {
                displaySlots[slotIndex].gameObject.SetActive(false);
            }
        }
    }

    // 현재 플레이어가 사용중인 무기의 WeaponData를 가져와 currentWeapon에 반영
    private void ResolveCurrentWeaponData()
    {
        if (currentWeapon != null) return;

        var wm = WeaponManager.Instance;
        if (wm != null && wm.currentWeapon != null && wm.currentWeapon.weaponData != null)
        {
            currentWeapon = wm.currentWeapon.weaponData;
            Debug.Log($"[UpgradeManager] 현재 장착 무기 자동 인식: {currentWeapon.weaponName} (tier {currentWeapon.tier})");
        }
    }
}