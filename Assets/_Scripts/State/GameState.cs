using System.Collections.Generic;

[System.Serializable]
public class GameState
{
    public WorldState worldState;
    
    // 对应 ItemManager.GetState() 的返回类型 List<ItemStack>
    public List<ItemStack> inventoryState;

    // 对应 CharacterManager.GetState() 的返回类型 CharactersState
    public CharactersState charactersState;
    
    public List<ActiveEventState> activeEvents;

    public GameState()
    {
        worldState = new WorldState();
        inventoryState=new List<ItemStack>();
        charactersState = new CharactersState(); 
        activeEvents = new List<ActiveEventState>();
    }
}