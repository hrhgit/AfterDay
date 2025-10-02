using System;
using System.Collections.Generic;

public static class GameEvents
{
    public static event Action OnTurnStart;
    public static void TriggerTurnStart() => OnTurnStart?.Invoke();

    public static event Action OnTurnEnd;
    public static void TriggerTurnEnd() => OnTurnEnd?.Invoke();

    public static event Action<ActionRecipeData, List<CardData>> OnActionAssigned;
    public static void TriggerActionAssigned(ActionRecipeData recipe, List<CardData> pawns) => OnActionAssigned?.Invoke(recipe, pawns);

    public static event Action OnGameStateChanged;
    public static void TriggerGameStateChanged() => OnGameStateChanged?.Invoke();

    public static event Action OnSaveGame;
    public static void TriggerSaveGame() => OnSaveGame?.Invoke();

    public static event Action<GameState> OnLoadGame;
    public static void TriggerLoadGame(GameState state) => OnLoadGame?.Invoke(state);
}
