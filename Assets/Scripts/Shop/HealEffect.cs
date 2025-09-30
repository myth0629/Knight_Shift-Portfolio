using UnityEngine;

[CreateAssetMenu(fileName = "HealEffect", menuName = "DMU/Shop/Effects/Heal", order = 0)]
public class HealEffect : ShopEffect
{
    [SerializeField] private int healHp = 0;
    [SerializeField] private int healSp = 0;

    public override void Apply(GameObject player)
    {
        if (player == null) return;
        var status = player.GetComponent<PlayerStatus>();
        if (status == null) status = player.GetComponentInChildren<PlayerStatus>();
        if (status == null) return;

        if (healHp > 0)
            status.currentHp = Mathf.Min(status.maxHp, status.currentHp + healHp);
        if (healSp > 0)
            status.currentSp = Mathf.Min(status.maxSp, status.currentSp + healSp);

        var ui = Object.FindFirstObjectByType<PlayerUI>();
        ui?.UpdateUI();
    }
}
