using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopItemSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI 참조")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button button;

    [Header("데이터")]
    public ShopItemData itemData;

    [Header("설명 패널")]
    [SerializeField] private ShopDescriptionPanel descriptionPanel;

    [Header("구매 확인창")]
    [SerializeField] private ShopConfirmDialog confirmDialog;
    [SerializeField] private bool disableAfterPurchase = true;

    private void Reset()
    {
        // 자동 참조 시도 (에디터에서 컴포넌트 추가할 때 편의)
        nameText = GetComponentInChildren<TMP_Text>();
        button = GetComponent<Button>();
        descriptionPanel = FindFirstObjectByType<ShopDescriptionPanel>(FindObjectsInactive.Include);
        confirmDialog = FindFirstObjectByType<ShopConfirmDialog>(FindObjectsInactive.Include);
    }

    private void Awake()
    {
        ApplyData();
        if (button == null)
            button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(OnButtonClicked);
        if (descriptionPanel == null)
            descriptionPanel = FindFirstObjectByType<ShopDescriptionPanel>(FindObjectsInactive.Include);
        if (confirmDialog == null)
            confirmDialog = FindFirstObjectByType<ShopConfirmDialog>(FindObjectsInactive.Include);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnButtonClicked);
    }

    public void ApplyData()
    {
        if (itemData == null) return;
        if (nameText != null) nameText.text = itemData.itemName;
        if (priceText != null) priceText.text = itemData.price.ToString() + " G";
        if (iconImage != null) iconImage.sprite = itemData.icon;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (itemData == null || descriptionPanel == null) return;
        descriptionPanel.Show(itemData.description);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (descriptionPanel == null) return;
        descriptionPanel.Hide();
    }

    public void OnButtonClicked()
    {
        if (itemData == null)
        {
            Debug.LogWarning($"ShopItemSlot '{name}' clicked but itemData is null.");
            return;
        }

        if (confirmDialog == null)
        {
            confirmDialog = FindFirstObjectByType<ShopConfirmDialog>(FindObjectsInactive.Include);
            if (confirmDialog == null)
            {
                Debug.LogWarning("ShopConfirmDialog not found in scene.");
                return;
            }
        }

        var playerDataForCheck = Object.FindFirstObjectByType<PlayerDataManager>();
        bool canAfford = playerDataForCheck != null && playerDataForCheck.Gold >= itemData.price;

        var sb = new StringBuilder();
        sb.AppendLine($"{itemData.itemName}");
        sb.Append("\n구매하시겠습니까?");
        if (!canAfford)
            sb.Append("\n\n골드가 부족합니다.");


        confirmDialog.Show(sb.ToString(),
            onConfirm: () =>
            {
                var playerData = playerDataForCheck ?? Object.FindFirstObjectByType<PlayerDataManager>();
                if (playerData == null)
                {
                    Debug.LogWarning("PlayerDataManager not found.");
                    return;
                }

                // 비용 결제
                if (!playerData.SpendGold(itemData.price))
                {
                    Debug.Log("골드가 부족합니다.");
                    return;
                }

                // 효과 적용
                var playerGo = GameObject.FindGameObjectWithTag("Player");
                if (itemData.effects != null)
                {
                    foreach (var effect in itemData.effects)
                    {
                        effect?.Apply(playerGo);
                    }
                }

                // 골드 UI 갱신
                var playerUI = Object.FindFirstObjectByType<PlayerUI>();
                playerUI?.UpdateGold();

                // 슬롯 비활성 처리
                if (disableAfterPurchase)
                {
                    if (button != null) button.interactable = false;
                    else gameObject.SetActive(false);
                }

                Debug.Log($"[SHOP] 구매 완료: {itemData.itemName}");
            },
            onCancel: () => { /* 취소: 아무 동작 없음 */ },
            confirmEnabled: canAfford);
    }
}
