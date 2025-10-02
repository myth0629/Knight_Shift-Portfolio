using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeConfirmPanel : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private GameObject panelObject;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private StatUpgradeSlot currentSlot;
    private StatUpgradeShop shop;

    private void Awake()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);

        Hide();
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(OnConfirmClicked);

        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(OnCancelClicked);
    }

    public void Show(StatUpgradeShop upgradeShop, StatUpgradeSlot slot)
    {
        if (slot == null || upgradeShop == null) return;

        shop = upgradeShop;
        currentSlot = slot;

        if (titleText != null)
            titleText.text = $"{slot.DisplayName} 업그레이드";

        if (descriptionText != null)
        {
            string statLabel = slot.StatType switch
            {
                PlayerStatType.MaxHp => "HP",
                PlayerStatType.MaxSp => "SP",
                _ => "스탯"
            };
            descriptionText.text = $"\n{statLabel}를 {slot.StatIncrease}만큼 증가시킵니다.\n정말 업그레이드 하시겠습니까?";
        }

        if (costText != null)
        {
            int cost = slot.GetCurrentCost();
            costText.text = $"비용: {cost} G";
        }

        if (panelObject != null)
            panelObject.SetActive(true);
        else
            gameObject.SetActive(true);
    }

    public void Hide()
    {
        currentSlot = null;
        shop = null;

        if (panelObject != null)
            panelObject.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void OnConfirmClicked()
    {
        if (shop != null && currentSlot != null)
        {
            shop.ConfirmUpgrade(currentSlot);
        }
        Hide();
    }

    private void OnCancelClicked()
    {
        Hide();
    }
}
