using System;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    PlayerUI playerUI;
    FirebaseGoldManager goldManager;
    
    public static PlayerDataManager Instance { get; private set; }

    public int Gold { get; private set; }

    private async void Awake()
    {
        goldManager = FindFirstObjectByType<FirebaseGoldManager>();

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 이동에도 유지

            while (!FirebaseInit.IsReady)
                await Task.Yield(); // Firebase 초기화 대기

            await LoadData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        playerUI = FindFirstObjectByType<PlayerUI>();
        goldManager = FindFirstObjectByType<FirebaseGoldManager>();
    }

    public void AddGold(int amount)
    {
        Gold += amount;
        Debug.Log(amount + " gold added. Total gold: " + Gold);
        playerUI.UpdateGold();
        // db에서 금액 업데이트
        goldManager.SaveGold(Gold);
    }

    public bool SpendGold(int amount)
    {
        if (Gold < amount)
            return false;

        Gold -= amount;
        // db에서 금액 업데이트
        goldManager.SaveGold(Gold);
        return true;
    }
    
    private async Task LoadData()
    {
        while (FirebaseGoldManager.Instance == null || !FirebaseGoldManager.Instance.IsGoldLoaded)
            await Task.Yield();

        Gold = FirebaseGoldManager.Instance.CurrentGold;
        playerUI.UpdateGold();

        Debug.Log("Gold loaded: " + Gold);
    }
}