using System;
using Unity.VisualScripting;
using UnityEngine;

public class BattleSceneManager : MonoBehaviour
{
    [SerializeField] GameObject portalGenerator;
    
    UIManager uIManager;
    
    private bool isBattleCleared = false;

    private void Awake()
    {
        uIManager = FindFirstObjectByType<UIManager>();
    }

    void Start()
    {
        if (portalGenerator == null)
        {
            Debug.LogError("PortalGenerator 컴포넌트를 찾을 수 없습니다.");
            return;
        }
    }

    void Update()
    {
        if (isBattleCleared) return;
        
        int monsterCount = GameObject.FindGameObjectsWithTag("Enemy").Length;
        
        if (monsterCount == 0)
        {
            isBattleCleared = true;
            OnBattleCleared();
        }
    }

    private void OnBattleCleared()
    {
        Debug.Log("Battle Cleared!");

        portalGenerator.SetActive(true);
        uIManager.ToggleUpgradeUIPanel();
    }
}