using UnityEngine;

public class PlayerStatUpgradeApplier : MonoBehaviour
{
    [SerializeField] private PlayerStatus playerStatus;
    [SerializeField] private string saveKeyPrefix = "StatUpgrade_";
    [SerializeField, Tooltip("씬 로드 시 자동 적용")] private bool applyOnStart = true;
    [SerializeField, Tooltip("HP 보너스를 적용한 뒤 현재 HP를 최대치로 맞춥니다.")] private bool refillHpOnApply = true;
    [SerializeField, Tooltip("SP 보너스를 적용한 뒤 현재 SP를 최대치로 맞춥니다.")] private bool refillSpOnApply = true;

    private bool applied;

    private void Awake()
    {
        if (playerStatus == null)
            playerStatus = GetComponent<PlayerStatus>() ?? GetComponentInChildren<PlayerStatus>();
    }

    private void Start()
    {
        if (applyOnStart)
        {
            ApplyBonuses();
        }
    }

    public void ApplyBonuses()
    {
        if (applied) return;
        if (playerStatus == null) return;

        ApplyBonus(PlayerStatType.MaxHp, refillHpOnApply);
        ApplyBonus(PlayerStatType.MaxSp, refillSpOnApply);

        var playerUI = FindFirstObjectByType<PlayerUI>();
        playerUI?.UpdateUI();

        var statusUI = FindFirstObjectByType<StatusUI>();
        statusUI?.UpdateStatusDisplay();

        applied = true;
    }

    private void ApplyBonus(PlayerStatType statType, bool refill)
    {
        string suffix = statType switch
        {
            PlayerStatType.MaxHp => "MaxHp",
            PlayerStatType.MaxSp => "MaxSp",
            _ => statType.ToString()
        };

        // 계정별 데이터 로드
        string bonusKey = $"{saveKeyPrefix}{suffix}_Bonus";
        int bonus = AccountDataManager.GetInt(bonusKey, 0);
        if (bonus <= 0) return;

        switch (statType)
        {
            case PlayerStatType.MaxHp:
                playerStatus.maxHp += bonus;
                if (refill)
                {
                    playerStatus.currentHp = playerStatus.maxHp;
                }
                else
                {
                    playerStatus.currentHp = Mathf.Min(playerStatus.currentHp, playerStatus.maxHp);
                }
                break;
            case PlayerStatType.MaxSp:
                playerStatus.maxSp += bonus;
                if (refill)
                {
                    playerStatus.currentSp = playerStatus.maxSp;
                }
                else
                {
                    playerStatus.currentSp = Mathf.Min(playerStatus.currentSp, playerStatus.maxSp);
                }
                break;
        }
    }
}
