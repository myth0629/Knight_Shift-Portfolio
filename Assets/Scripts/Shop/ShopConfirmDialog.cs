using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopConfirmDialog : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private Action onConfirm;
    private Action onCancel;

    private bool initialized;

    private void Awake()
    {
        Initialize();
        Hide();
    }

    private void Initialize()
    {
        if (initialized) return;

        if (root == null) root = gameObject;

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(Confirm);
            confirmButton.onClick.AddListener(Confirm);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(Cancel);
            cancelButton.onClick.AddListener(Cancel);
        }

        initialized = true;
    }

    public void PanelEnable()
    {
        Initialize();
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        if (root != null && !root.activeSelf)
            root.SetActive(true);
    }

    public void Show(string message, Action onConfirm, Action onCancel, bool confirmEnabled = true)
    {
        Initialize();

        this.onConfirm = onConfirm;
        this.onCancel = onCancel;
        if (messageText != null) messageText.text = message;
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        if (root != null && !root.activeSelf)
            root.SetActive(true);
        if (confirmButton != null)
            confirmButton.interactable = confirmEnabled;
    }

    public void Hide()
    {
        Initialize();
        if (root != null) root.SetActive(false);
        if (confirmButton != null)
            confirmButton.interactable = true;
        onConfirm = null;
        onCancel = null;
    }

    private void Confirm()
    {
        onConfirm?.Invoke();
        Hide();
    }

    private void Cancel()
    {
        onCancel?.Invoke();
        Hide();
    }
}
