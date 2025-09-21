using System.Collections.Generic;

// 用于存档的整个物品库存的状态 (原 InventoryState)
[System.Serializable]
public class ItemsState
{
    public Dictionary<string, int> stackedItems;
    public List<ItemInstance> itemInstances;

    public ItemsState()
    {
        stackedItems = new Dictionary<string, int>();
        itemInstances = new List<ItemInstance>();
    }
}

// 用于追踪单个物品实例 (原 InventoryItemInstance)
[System.Serializable]
public class ItemInstance
{
    public string itemID;
    public int turnsRemaining;

    public ItemInstance(string id, int spoilageTurns)
    {
        itemID = id;
        turnsRemaining = spoilageTurns > 0 ? spoilageTurns : -1;
    }
}