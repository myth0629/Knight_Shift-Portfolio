using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeConfirmPanel : MonoBehaviour, IUIPanel
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

    private void Start()
    {
        // UIPanelManager에 등록
        if (UIPanelManager.Instance != null)
        {
            UIPanelManager.Instance.RegisterPanel(this);
        }
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(OnConfirmClicked);

        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(OnCancelClicked);

        // UIPanelManager에서 등록 해제
        if (UIPanelManager.Instance != null)
        {
            UIPanelManager.Instance.UnregisterPanel(this);
        }
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

        // UIPanelManager에 패널이 열렸음을 알림
        if (UIPanelManager.Instance != null)
        {
            UIPanelManager.Instance.OnPanelOpened(this);
        }
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

    // IUIPanel 인터페이스 구현
    public bool IsOpen()
    {
        if (panelObject != null)
            return panelObject.activeSelf;
        else
            return gameObject.activeSelf;
    }

    public void Close()
    {
        Hide();
    }
}
