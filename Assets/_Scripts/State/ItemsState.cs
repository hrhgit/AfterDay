using System.Collections.Generic;

// 用于存档的整个物品库存的状态 (原 InventoryState)
[System.Serializable]
public class ItemsState
{
    public Dictionary<int, int> stackedItems;
    public List<ItemInstance> itemInstances;

    public ItemsState()
    {
        stackedItems = new Dictionary<int, int>();
        itemInstances = new List<ItemInstance>();
    }
}

// 用于追踪单个物品实例 (原 InventoryItemInstance)
[System.Serializable]
public class ItemInstance
{
    public int itemID;
    public int turnsRemaining;

    public ItemInstance(int id, int spoilageTurns)
    {
        itemID = id;
        turnsRemaining = spoilageTurns > 0 ? spoilageTurns : -1;
    }
}