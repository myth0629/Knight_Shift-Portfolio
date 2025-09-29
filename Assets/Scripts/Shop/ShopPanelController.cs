using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShopPanelController : MonoBehaviour
{
    [Header("슬롯 관련")]
    [SerializeField] private Transform slotsParent; // 슬롯들이 배치될 부모 (Grid/Layout)
    [SerializeField] private ShopItemSlot slotPrefab; // 슬롯 프리팹

    [Header("데이터")]
    [SerializeField] private List<ShopItemData> items = new List<ShopItemData>();

    private void Awake()
    {
        BuildSlots();
    }

    public void SetItems(List<ShopItemData> newItems)
    {
        items = newItems;
        Rebuild();
    }

    public void Rebuild()
    {
        ClearChildren();
        BuildSlots();
    }

    private void BuildSlots()
    {
        if (slotsParent == null || slotPrefab == null || items == null) return;
        foreach (var item in items)
        {
            var slot = Instantiate(slotPrefab, slotsParent);
            slot.itemData = item;
            slot.ApplyData();
        }
    }

    private void ClearChildren()
    {
        if (slotsParent == null) return;
        for (int i = slotsParent.childCount - 1; i >= 0; i--)
        {
            Destroy(slotsParent.GetChild(i).gameObject);
        }
    }
}
