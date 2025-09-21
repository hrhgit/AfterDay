using UnityEngine;

public class PlayerInventoryView : MonoBehaviour
{
    private void OnEnable() => GameEvents.OnGameStateChanged += UpdateView;
    private void OnDisable() => GameEvents.OnGameStateChanged -= UpdateView;

    private void Start() => UpdateView();

    private void UpdateView()
    {
        Debug.Log("UI is updating based on new game state...");
        // TODO: 刷新UI的具体逻辑
    }
}


