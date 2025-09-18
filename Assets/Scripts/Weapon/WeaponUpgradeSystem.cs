using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class WeaponUpgradeSystem : MonoBehaviour
{
    public static WeaponUpgradeSystem Instance { get; private set; }

    private Dictionary<WeaponData, int> _levels = new Dictionary<WeaponData, int>();

    [Tooltip("기본 시작 강화 레벨")] public int defaultStartLevel = 0;
    [Tooltip("최대 강화 레벨 (0 = 제한 없음)")] public int maxLevel = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public int GetLevel(WeaponData data)
    {
        if (data == null) return 0;
        if (_levels.TryGetValue(data, out int lv)) return lv;
        _levels[data] = defaultStartLevel;
        return defaultStartLevel;
    }

    public int Upgrade(WeaponData data, int amount = 1)
    {
        if (data == null) return 0;
        int current = GetLevel(data);
        int next = current + amount;
        if (maxLevel > 0) next = Mathf.Min(next, maxLevel);
        _levels[data] = next;
        return next;
    }
}
