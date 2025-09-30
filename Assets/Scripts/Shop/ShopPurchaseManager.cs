using System.Text;
using UnityEngine;

public class ShopPurchaseManager : MonoBehaviour
{
    [SerializeField] private ShopConfirmDialog confirmDialog;

    private PlayerDataManager playerData;
    private GameObject player;

    private void Awake()
    {
        playerData = FindFirstObjectByType<PlayerDataManager>();
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        player = playerGo != null ? playerGo : null;
        if (confirmDialog == null) confirmDialog = FindFirstObjectByType<ShopConfirmDialog>();
    }

    public void TryPurchase(ShopItemData itemData)
    {
        if (itemData == null || confirmDialog == null) return;

        // 확인창 메시지 구성
        var sb = new StringBuilder();
        sb.AppendLine($"{itemData.itemName}");
        if (!string.IsNullOrEmpty(itemData.description)) sb.AppendLine(itemData.description);
        sb.Append($"가격: {itemData.price} G\n구매하시겠습니까?");

        confirmDialog.Show(sb.ToString(),
            onConfirm: () => Purchase(itemData),
            onCancel: () => { /* 취소 시 아무 것도 안함 */ });
    }

    private void Purchase(ShopItemData itemData)
    {
        if (playerData == null)
            playerData = FindFirstObjectByType<PlayerDataManager>();

        if (playerData == null)
        {
            Debug.LogWarning("PlayerDataManager not found.");
            return;
        }

        if (!playerData.SpendGold(itemData.price))
        {
            Debug.Log("골드가 부족합니다.");
            return;
        }

        // 효과 적용
        if (itemData.effects != null)
        {
            foreach (var effect in itemData.effects)
            {
                effect?.Apply(player);
            }
        }

        // 골드 UI 갱신
        var playerUI = FindFirstObjectByType<PlayerUI>();
        playerUI?.UpdateGold();

        Debug.Log($"[SHOP] 구매 완료: {itemData.itemName}");
    }
}
