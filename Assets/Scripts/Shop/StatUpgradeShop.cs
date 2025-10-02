using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StatUpgradeShop : MonoBehaviour
{
    [Header("슬롯")]
    [SerializeField] private StatUpgradeSlot[] slots;

    [Header("UI")]
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private float feedbackDuration = 2f;
    [SerializeField] private UpgradeConfirmPanel confirmPanel;

    [Header("설정")]
    [SerializeField] private string requiredSceneName = "Start";
    [SerializeField] private string saveKeyPrefix = "StatUpgrade_";
    [SerializeField] private bool applySavedBonusesOnLoad = true;

    private PlayerStatus playerStatus;
    private PlayerDataManager playerData;
    private PlayerUI playerUI;
    private StatusUI statusUI;

    private Coroutine feedbackRoutine;

    public int PlayerGold => playerData != null ? playerData.Gold : 0;

    private void Awake()
    {
        if (!string.IsNullOrEmpty(requiredSceneName))
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.name.Equals(requiredSceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                // 지정된 씬이 아니라면 상점을 비활성화
                gameObject.SetActive(false);
                return;
            }
        }

        if (slots == null || slots.Length == 0)
        {
            slots = GetComponentsInChildren<StatUpgradeSlot>(true);
        }
    }

    private void Start()
    {
        playerStatus = FindFirstObjectByType<PlayerStatus>();
        playerData = FindFirstObjectByType<PlayerDataManager>();
        playerUI = FindFirstObjectByType<PlayerUI>();
        statusUI = FindFirstObjectByType<StatusUI>();

        LoadSlots();
        RefreshAll();
    }

    private void LoadSlots()
    {
        if (slots == null) return;

        bool shouldApplySaved = applySavedBonusesOnLoad && FindFirstObjectByType<PlayerStatUpgradeApplier>() == null;

        foreach (var slot in slots)
        {
            if (slot == null) continue;

            // 계정별 데이터 로드
            string levelKey = StatUpgradeSlot.BuildKey(saveKeyPrefix, slot.Id, "Level");
            string bonusKey = StatUpgradeSlot.BuildKey(saveKeyPrefix, slot.Id, "Bonus");
            
            int savedLevel = AccountDataManager.GetInt(levelKey, 0);
            int savedBonus = AccountDataManager.GetInt(bonusKey, 0);
            slot.Initialize(this, savedLevel, savedBonus);

            if (shouldApplySaved && savedBonus > 0)
            {
                ApplyStatBonus(slot.StatType, savedBonus, false);
            }
        }

        UpdatePlayerAndStatusUI();
    }

    public void TryUpgrade(StatUpgradeSlot slot)
    {
        if (slot == null) return;

        if (!slot.CanUpgrade())
        {
            ShowFeedback("이미 최대 레벨입니다.");
            return;
        }

        if (playerData == null)
        {
            playerData = FindFirstObjectByType<PlayerDataManager>();
            if (playerData == null)
            {
                ShowFeedback("플레이어 데이터가 없습니다.");
                return;
            }
        }

        int cost = slot.GetCurrentCost();
        if (PlayerGold < cost)
        {
            ShowFeedback("골드가 부족합니다.");
            RefreshAll();
            return;
        }

        // 확인 패널 표시
        if (confirmPanel != null)
        {
            confirmPanel.Show(this, slot);
        }
        else
        {
            // 확인 패널이 없으면 바로 업그레이드
            ConfirmUpgrade(slot);
        }
    }

    public void ConfirmUpgrade(StatUpgradeSlot slot)
    {
        if (slot == null) return;

        if (playerData == null)
        {
            playerData = FindFirstObjectByType<PlayerDataManager>();
            if (playerData == null)
            {
                ShowFeedback("플레이어 데이터가 없습니다.");
                return;
            }
        }

        int cost = slot.GetCurrentCost();
        if (!playerData.SpendGold(cost))
        {
            ShowFeedback("골드가 부족합니다.");
            RefreshAll();
            return;
        }

        slot.ApplyUpgradeProgress();
        SaveSlot(slot);
        ApplyStatBonus(slot.StatType, slot.StatIncrease, true);

        ShowFeedback($"{slot.DisplayName} 업그레이드 완료!");
        RefreshAll();
    }

    private void ApplyStatBonus(PlayerStatType statType, int amount, bool updateUI)
    {
        if (amount <= 0) return;

        if (playerStatus == null)
        {
            playerStatus = FindFirstObjectByType<PlayerStatus>();
        }

        if (playerStatus == null) return;

        switch (statType)
        {
            case PlayerStatType.MaxHp:
                playerStatus.maxHp += amount;
                playerStatus.currentHp = Mathf.Min(playerStatus.maxHp, playerStatus.currentHp + amount);
                break;
            case PlayerStatType.MaxSp:
                playerStatus.maxSp += amount;
                playerStatus.currentSp = Mathf.Min(playerStatus.maxSp, playerStatus.currentSp + amount);
                break;
        }

        if (updateUI)
        {
            UpdatePlayerAndStatusUI();
        }
    }

    private void SaveSlot(StatUpgradeSlot slot)
    {
        // 계정별 데이터 저장
        string levelKey = StatUpgradeSlot.BuildKey(saveKeyPrefix, slot.Id, "Level");
        string bonusKey = StatUpgradeSlot.BuildKey(saveKeyPrefix, slot.Id, "Bonus");
        
        AccountDataManager.SetInt(levelKey, slot.CurrentLevel);
        AccountDataManager.SetInt(bonusKey, slot.AccumulatedBonus);
        AccountDataManager.Save();
    }

    private void RefreshAll()
    {
        if (slots != null)
        {
            foreach (var slot in slots)
            {
                slot?.RefreshUI(PlayerGold);
            }
        }

        UpdateGoldUI();
    }

    private void UpdatePlayerAndStatusUI()
    {
        if (playerUI == null)
            playerUI = FindFirstObjectByType<PlayerUI>();
        playerUI?.UpdateUI();

        if (statusUI == null)
            statusUI = FindFirstObjectByType<StatusUI>();
        statusUI?.UpdateStatusDisplay();

        UpdateGoldUI();
    }

    private void UpdateGoldUI()
    {
        if (goldText != null)
        {
            goldText.text = $"골드 : {PlayerGold}";
        }
    }

    private void ShowFeedback(string message)
    {
        if (feedbackText == null) return;

        if (feedbackRoutine != null)
            StopCoroutine(feedbackRoutine);

        feedbackRoutine = StartCoroutine(FeedbackCoroutine(message));
    }

    private IEnumerator FeedbackCoroutine(string message)
    {
        feedbackText.gameObject.SetActive(true);
        feedbackText.text = message;
        yield return new WaitForSeconds(feedbackDuration);
        feedbackText.gameObject.SetActive(false);
    }
}
