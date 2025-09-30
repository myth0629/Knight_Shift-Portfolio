using UnityEngine;

[CreateAssetMenu(fileName = "ShopItem", menuName = "DMU/Shop/Shop Item Data", order = 0)]
public class ShopItemData : ScriptableObject
{
    [Header("기본 정보")]
    public string itemName;
    [TextArea]
    public string description;
    [Min(0)]
    public int price;

    [Header("표시용 (선택)")]
    public Sprite icon;

    // 구매 시 적용할 효과들
    public ShopEffect[] effects;
}
