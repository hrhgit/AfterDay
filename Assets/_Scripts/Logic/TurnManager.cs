using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public int CurrentTurn { get; private set; } = 1;

    public void EndTurn()
    {
        Debug.Log($"--- End of Turn {CurrentTurn} ---");
        GameEvents.TriggerTurnEnd();

        CurrentTurn++;
        Debug.Log($"--- Start of Turn {CurrentTurn} ---");
        GameEvents.TriggerTurnStart();

        GameEvents.TriggerGameStateChanged();
    }

    public void LoadState(WorldState state)
    {
        if (state == null)
        {
            Debug.LogWarning("Attempted to load a null WorldState; using defaults.");
            return;
        }

        CurrentTurn = state.currentTurn;
    }

    public WorldState GetState()
    {
        return new WorldState
        {
            currentTurn = CurrentTurn
        };
    }
    
}
