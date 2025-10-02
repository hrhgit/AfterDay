using System.Collections.Generic;
using UnityEngine;

public class Workstation : MonoBehaviour
{
    private class ActiveTask
    {
        public ActionRecipeData recipe;
        public int turnsRemaining;
        public List<CardData> assignedPawns;

        public ActiveTask(ActionRecipeData recipe, List<CardData> pawns)
        {
            this.recipe = recipe;
            this.turnsRemaining = Mathf.Max(1, recipe.turnsToComplete);
            this.assignedPawns = new List<CardData>(pawns);
        }
    }

    private readonly List<ActiveTask> _activeTasks = new List<ActiveTask>();

    private void OnEnable()
    {
        GameEvents.OnActionAssigned += AssignAction;
        GameEvents.OnTurnEnd += ProcessTurn;
    }

    private void OnDisable()
    {
        GameEvents.OnActionAssigned -= AssignAction;
        GameEvents.OnTurnEnd -= ProcessTurn;
    }

    private void AssignAction(ActionRecipeData recipe, List<CardData> pawns)
    {
        _activeTasks.Add(new ActiveTask(recipe, pawns));
        Debug.Log($"Action '{recipe.actionName}' assigned.");
        GameEvents.TriggerGameStateChanged();
    }

    private void ProcessTurn()
    {
        for (int i = _activeTasks.Count - 1; i >= 0; i--)
        {
            _activeTasks[i].turnsRemaining--;
            if (_activeTasks[i].turnsRemaining <= 0)
            {
                ResolveTask(_activeTasks[i]);
                _activeTasks.RemoveAt(i);
            }
        }
    }

    private void ResolveTask(ActiveTask task)
    {
        Debug.Log($"Action '{task.recipe.actionName}' resolved!");
        // TODO: 发放奖励到库存，并抛出状态变化事件
    }
}


