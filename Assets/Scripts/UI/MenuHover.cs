using UnityEngine;
using UnityEngine.EventSystems;

public class MenuHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject hoverIcon;

    private void Awake()
    {
        if (hoverIcon != null)
            hoverIcon.SetActive(false); // 시작 시 비활성화
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverIcon != null)
            hoverIcon.SetActive(true); // 호버 시 활성화
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverIcon != null)
            hoverIcon.SetActive(false); // 호버 벗어날 때 비활성화
    }
}