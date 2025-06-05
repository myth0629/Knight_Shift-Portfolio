using System;
using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    PlayerUI playerUI;
    
    public static PlayerDataManager Instance { get; private set; }

    public int Gold { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 이동에도 유지
            LoadData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        playerUI = FindFirstObjectByType<PlayerUI>();
    }

    public void AddGold(int amount)
    {
        Gold += amount;
        Debug.Log(amount + " gold added. Total gold: " + Gold);
        playerUI.UpdateGold();
        SaveData();
    }

    public bool SpendGold(int amount)
    {
        if (Gold < amount)
            return false;

        Gold -= amount;
        SaveData();
        return true;
    }

    private void LoadData()
    {
        // TODO: DB 연동 혹은 로컬 저장소에서 불러오기
        // 예시: Gold = PlayerPrefs.GetInt("Gold", 0);
    }

    private void SaveData()
    {
        // TODO: DB 저장 혹은 로컬 저장소로 저장
        // 예시: PlayerPrefs.SetInt("Gold", Gold);
    }
}