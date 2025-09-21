using UnityEngine;
using UnityEngine.UI;

public class EndTurnButton : MonoBehaviour
{
    [SerializeField] private TurnManager turnManager;
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        if (_button != null && turnManager != null)
        {
            _button.onClick.AddListener(turnManager.EndTurn);
        }
    }
}


