using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class StatUpgradeSlot : MonoBehaviour
{
    [Header("식별자")]
    [SerializeField] private string id = "MaxHp";
    [SerializeField] private string displayName = "Max HP";

    [Header("참조")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button upgradeButton;

    [Header("업그레이드 설정")]
    [SerializeField] private PlayerStatType statType = PlayerStatType.MaxHp;
    [SerializeField] private int statIncrease = 10;
    [SerializeField] private int baseCost = 100;
    [SerializeField, Tooltip("업그레이드마다 곱해지는 비용 배수 (1.0 = 고정 비용)")] private float costMultiplier = 1.5f;
    [SerializeField, Tooltip("0이면 무제한")]
    private int maxLevel = 0;

    [Header("저장된 값(디버그)")]
    [SerializeField] private int currentLevel;
    [SerializeField] private int accumulatedBonus;

    private StatUpgradeShop shop;

    private void Reset()
    {
        titleText = GetComponentInChildren<TMP_Text>();
        upgradeButton = GetComponentInChildren<Button>();
        if (string.IsNullOrEmpty(id))
        {
            id = statType.ToString();
        }
    }

    private void Awake()
    {
        if (upgradeButton == null)
            upgradeButton = GetComponentInChildren<Button>();

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(OnUpgradeButtonClicked);
            upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
        }

        if (string.IsNullOrEmpty(id))
        {
            id = statType.ToString();
        }
    }

    private void OnDestroy()
    {
        if (upgradeButton != null)
            upgradeButton.onClick.RemoveListener(OnUpgradeButtonClicked);
    }

    internal void Initialize(StatUpgradeShop owner, int savedLevel, int savedBonus)
    {
        shop = owner;
        currentLevel = Mathf.Max(0, savedLevel);
        accumulatedBonus = Mathf.Max(0, savedBonus);
        RefreshUI(shop != null ? shop.PlayerGold : 0);
    }

    public void RefreshUI(int playerGold)
    {
        if (titleText != null)
            titleText.text = string.IsNullOrEmpty(displayName) ? statType.ToString() : displayName;

        if (levelText != null)
        {
            if (maxLevel > 0)
                levelText.text = $"Lv. {currentLevel} / {maxLevel}";
            else
                levelText.text = $"Lv. {currentLevel}";
        }

        if (descriptionText != null)
            descriptionText.text = $"+{statIncrease} {GetStatLabel()}";

        bool canUpgrade = CanUpgrade();
        int cost = GetCurrentCost();

        if (costText != null)
        {
            costText.text = canUpgrade ? $"{cost} G" : "MAX";
        }

        if (upgradeButton != null)
        {
            upgradeButton.interactable = canUpgrade && playerGold >= cost;
        }
    }

    private void OnUpgradeButtonClicked()
    {
        shop?.TryUpgrade(this);
    }

    public bool CanUpgrade()
    {
        return maxLevel <= 0 || currentLevel < maxLevel;
    }

    public int GetCurrentCost()
    {
        if (!CanUpgrade()) return 0;
        float factor = Mathf.Pow(Mathf.Max(1f, costMultiplier), currentLevel);
        int cost = Mathf.RoundToInt(baseCost * factor);
        return Mathf.Max(1, cost);
    }

    public void ApplyUpgradeProgress()
    {
        currentLevel++;
        accumulatedBonus += statIncrease;
    }

    public string Id => id;
    public string DisplayName => string.IsNullOrEmpty(displayName) ? statType.ToString() : displayName;
    public PlayerStatType StatType => statType;
    public int StatIncrease => statIncrease;
    public int CurrentLevel => currentLevel;
    public int AccumulatedBonus => accumulatedBonus;

    public static string BuildKey(string prefix, string id, string suffix)
    {
        return $"{prefix}{id}_{suffix}";
    }

    private string GetStatLabel()
    {
        return statType switch
        {
            PlayerStatType.MaxHp => "HP",
            PlayerStatType.MaxSp => "SP",
            _ => "Stat"
        };
    }
}
