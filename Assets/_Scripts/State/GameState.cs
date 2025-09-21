using System.Collections.Generic;

[System.Serializable]
public class GameState
{
    public WorldState worldState;
    public ItemsState itemsState; 
    public CharactersState charactersState; 
    public List<ActiveEventState> activeEvents;

    public GameState()
    {
        worldState = new WorldState();
        itemsState = new ItemsState();
        charactersState = new CharactersState(); 
        activeEvents = new List<ActiveEventState>();
    }
}