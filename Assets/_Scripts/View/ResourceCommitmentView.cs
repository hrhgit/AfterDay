using UnityEngine;
using UnityEngine.UI;
using System;

public class ResourceCommitmentView : MonoBehaviour
{
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMPro.TextMeshProUGUI requirementText;

    private Action _onSuccess;
    private Action _onCancel;

    public void Show(EventRequirement reqData, Action onSuccess, Action onCancel)
    {
        _onSuccess = onSuccess;
        _onCancel = onCancel;

        requirementText.text = "需求: ...";

        bool canAfford = ItemManager.Instance.CheckRequirements(reqData);
        if (confirmButton != null) confirmButton.interactable = canAfford;

        gameObject.SetActive(true);
        if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
        if (cancelButton != null) cancelButton.onClick.AddListener(Cancel);
    }

    private void Confirm()
    {
        _onSuccess?.Invoke();
        Hide();
    }

    private void Cancel()
    {
        _onCancel?.Invoke();
        Hide();
    }

    private void Hide()
    {
        if (confirmButton != null) confirmButton.onClick.RemoveAllListeners();
        if (cancelButton != null) cancelButton.onClick.RemoveAllListeners();
        gameObject.SetActive(false);
    }
}


