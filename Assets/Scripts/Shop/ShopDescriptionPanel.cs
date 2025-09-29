using TMPro;
using UnityEngine;

public class ShopDescriptionPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private GameObject root; // 패널 루트 (옵션)

    private void Awake()
    {
        if (root == null) root = gameObject;
        Hide();
    }

    public void Show(string desc)
    {
        if (descriptionText != null)
            descriptionText.text = desc;
        if (root != null)
            root.SetActive(true);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }
}
