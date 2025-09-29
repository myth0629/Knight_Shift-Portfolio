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

    [Header("데이터")]
    public ShopItemData itemData;

    [Header("설명 패널")]
    [SerializeField] private ShopDescriptionPanel descriptionPanel;

    private void Reset()
    {
        // 자동 참조 시도 (에디터에서 컴포넌트 추가할 때 편의)
        nameText = GetComponentInChildren<TMP_Text>();
        descriptionPanel = FindFirstObjectByType<ShopDescriptionPanel>();
    }

    private void Awake()
    {
        ApplyData();
        if (descriptionPanel == null)
            descriptionPanel = FindFirstObjectByType<ShopDescriptionPanel>();
    }

    public void ApplyData()
    {
        if (itemData == null) return;
        if (nameText != null) nameText.text = itemData.itemName;
        if (priceText != null) priceText.text = itemData.price.ToString();
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
}
