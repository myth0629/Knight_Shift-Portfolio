using UnityEngine;

public class ShopNpc : MonoBehaviour
{
    [SerializeField] float interactionDistance = 3f; // 상호작용 거리
    
    Transform playerTransform;
    
    UIManager uiManager;
    
    private void Start()
    {
        uiManager = FindFirstObjectByType<UIManager>();
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }
    
    private void Update()
    {
        if (Vector3.Distance(transform.position, playerTransform.position) < interactionDistance)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                uiManager.ToggleShopUIPanel();
            }
        }
    }
}
