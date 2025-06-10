using System;
using StarterAssets;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public GameObject slotPanel;  // 무기 업그레이드 패널
    public GameObject shopPanel; // 상점 UI 패널 (Start Scene에서 사용)
    public CinemachineCamera vcam;
    
    PlayerInput input;
    private bool isPanelOpen = false;

    private void Start()
    {
        slotPanel = FindInactiveObjectByName("SlotPanel");
        vcam = FindFirstObjectByType<CinemachineCamera>();
        input = FindFirstObjectByType<PlayerInput>();
    }

    private GameObject FindInactiveObjectByName(string name)
    {
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform t in allTransforms)
        {
            if (t.hideFlags == HideFlags.None && t.name == name)
            {
                return t.gameObject;
            }
        }
        return null;
    }
    
    // 테스트용 
    // void Update()
    // {
    //     if (Input.GetKeyDown(KeyCode.E))
    //     {
    //         ToggleUpgradeUIPanel();
    //     }
    // }

    public void ToggleUpgradeUIPanel()
    {
        isPanelOpen = !isPanelOpen;
        slotPanel.SetActive(isPanelOpen);

        if (isPanelOpen)
        {
            // 마우스 커서 보이게 & 잠금 해제
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 게임 일시정지 (필요 시)
            Time.timeScale = 0f;
            
            // 카메라 시점 이동 금지
            vcam.gameObject.SetActive(false);
        }
        else
        {
            // 마우스 커서 숨기고 잠금
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // 게임 재시작
            Time.timeScale = 1f;
            
            // 카메라 시점 이동 허용
            vcam.gameObject.SetActive(true);
        }
    }

    public void ToggleShopUIPanel()
    {
        isPanelOpen = !isPanelOpen;
        shopPanel.SetActive(isPanelOpen);
        
        if (isPanelOpen)
        {
            // 마우스 커서 보이게 & 잠금 해제
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            input.enabled = false;
            
            // 카메라 시점 이동 금지
            vcam.gameObject.SetActive(false);
        }
        else
        {
            // 마우스 커서 숨기고 잠금
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            input.enabled = true;
            
            // 카메라 시점 이동 허용
            vcam.gameObject.SetActive(true);
        }
    }
    
    // 공통 패널 닫기 함수
    public void ClosePanel(GameObject panel)
    {
        panel.SetActive(false);
    }
}