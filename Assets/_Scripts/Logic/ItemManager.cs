using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 管理游戏中所有非角色单位的物品和资源。
/// (原 InventoryManager)
/// </summary>
public class ItemManager : MonoBehaviour // <--- 名称已更改
{
    public static ItemManager Instance { get; private set; } // <--- 名称已更改

    // 运行时的数据存储
    private Dictionary<int, int> _stackedItems = new Dictionary<int, int>();
    private List<ItemInstance> _itemInstances = new List<ItemInstance>(); // <--- 名称已更改

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // 假设TurnManager会在启动后才触发事件
        // GameEvents.OnTurnEnd += ProcessSpoilage; 
    }

    private void OnDestroy()
    {
        // GameEvents.OnTurnEnd -= ProcessSpoilage;
    }

    #region Initialization & Setup

    /// <summary>
    /// 初始化新游戏的物品库存。
    /// </summary>
    public void SetupNewGame(List<ItemReward> startingItems)
    {
        // 1. 清空旧的库存
        _stackedItems.Clear();
        _itemInstances.Clear();

        // 2. 根据剧本添加初始物品
        if (startingItems != null)
        {
            foreach (var itemReward in startingItems)
            {
                AddItem(itemReward.item, itemReward.quantity);
            }
        }
        
        Debug.Log("ItemManager: New game setup complete.");
        GameEvents.TriggerGameStateChanged(); // 在所有物品添加完毕后，通知一次UI即可
    }

    #endregion
    
     #region Public API - 物品操作

     /// <summary>
     /// 向库存添加物品。
     /// </summary>
     public void AddItem(ItemData item, int quantity = 1)
     {
         if (item == null || quantity <= 0) return;
 
         if (item.isStackable && !item.DoesSpoil())
         {
             // 对于可堆叠且不腐烂的物品，存入字典
             if (!_stackedItems.ContainsKey(item.UniqueID))
             {
                 _stackedItems[item.UniqueID] = 0;
             }
             _stackedItems[item.UniqueID] += quantity;
         }
         else
         {
             // 对于不可堆叠或会腐烂的物品，创建实例
             for (int i = 0; i < quantity; i++)
             {
                 _itemInstances.Add(new ItemInstance(item.UniqueID, item.spoilageTurns));
             }
         }
 
         Debug.Log($"Added {quantity} of '{item.name}' to inventory.");
         GameEvents.TriggerGameStateChanged(); // 通知UI和其他系统
     }
 
     /// <summary>
     /// 从库存消耗指定数量的物品。
     /// </summary>
     public void ConsumeItem(int itemID, int quantity = 1)
     {
         if (quantity <= 0 || !HasItem(itemID, quantity))
         {
             Debug.LogWarning($"Attempted to consume {quantity} of '{itemID}', but not enough in inventory.");
             return;
         }
         
         ItemData itemData = DataManager.Instance.GetItemData(itemID);
 
         if (itemData.isStackable && !itemData.DoesSpoil())
         {
             _stackedItems[itemID] -= quantity;
             if (_stackedItems[itemID] <= 0)
             {
                 _stackedItems.Remove(itemID);
             }
         }
         else
         {
             int countToRemove = quantity;
             // 从后往前遍历以安全地移除元素
             for (int i = _itemInstances.Count - 1; i >= 0; i--)
             {
                 if (countToRemove > 0 && _itemInstances[i].itemID == itemID)
                 {
                     _itemInstances.RemoveAt(i);
                     countToRemove--;
                 }
             }
         }
 
         Debug.Log($"Consumed {quantity} of '{itemData.name}'.");
         GameEvents.TriggerGameStateChanged();
     }
     /// <summary>
     /// 消耗一组物品需求。
     /// </summary>
     public void ConsumeRequirements(EventRequirement requirement)
     {
         if (requirement?.requiredItems == null) return;

         foreach (var reqItem in requirement.requiredItems)
         {
             // 注意：我们之前的方法名叫 ConsumeItem
             ConsumeItem(reqItem.item.UniqueID, reqItem.amount);
         }
     }
     
     /// <summary>
     /// RemoveItem的别名，或ConsumeItem的别名。确保命名统一。
     /// 如果事件结果中使用RemoveItem，我们可以让它直接调用ConsumeItem。
     /// </summary>
     public void RemoveItem(ItemData item, int quantity = 1)
     {
         ConsumeItem(item.UniqueID, quantity);
     }

 
     #endregion
 
    #region Public API - 查询

    /// <summary>
    /// 检查是否有足够数量的特定物品。
    /// </summary>
    public bool HasItem(int itemID, int quantity = 1)
    {
        int totalCount = 0;
        
        // 检查可堆叠物品
        if (_stackedItems.TryGetValue(itemID, out int stackedCount))
        {
            totalCount += stackedCount;
        }

        // 检查实例物品
        // totalCount += _itemInstances.Count(instance => instance.itemID == itemID);
        foreach(var instance in _itemInstances)
        {
            if (instance.itemID == itemID) totalCount++;
        }

        return totalCount >= quantity;
    }
    
    /// <summary>
    /// 检查是否满足一组物品需求。
    /// </summary>
    public bool CheckRequirements(EventRequirement requirement)
    {
        if (requirement?.requiredItems == null) return true;

        foreach (var reqItem in requirement.requiredItems)
        {
            if (!HasItem(reqItem.item.UniqueID, reqItem.amount))
            {
                return false; // 只要有一个不满足，就返回false
            }
        }
        return true;
    }
    
    /// <summary>
    /// 获取所有可堆叠物品的副本字典。
    /// 返回副本是为了防止外部脚本直接修改库存数据。
    /// </summary>
    public Dictionary<int, int> GetStackedItems()
    {
        return new Dictionary<int, int>(_stackedItems);
    }

    /// <summary>
    /// 获取所有独特或会腐烂物品的实例副本列表。
    /// 返回副本是为了保护内部列表的完整性。
    /// </summary>
    public List<ItemInstance> GetItemInstances()
    {
        return new List<ItemInstance>(_itemInstances);
    }

    #endregion

    #region Turn Logic

    /// <summary>
    /// 处理物品腐烂，应在每回合结束时由TurnManager调用。
    /// </summary>
    public void ProcessSpoilage()
    {
        bool inventoryChanged = false;
        // 从后往前遍历以安全地移除元素
        for (int i = _itemInstances.Count - 1; i >= 0; i--)
        {
            var instance = _itemInstances[i];
            if (instance.turnsRemaining > 0) // -1表示永不腐烂
            {
                instance.turnsRemaining--;
                if (instance.turnsRemaining == 0)
                {
                    Debug.Log($"An item '{instance.itemID}' has spoiled and was removed.");
                    _itemInstances.RemoveAt(i);
                    inventoryChanged = true;
                    // 可选：在这里添加一个“腐烂的食物”到库存
                    // AddItem(DataManager.Instance.GetItemData("item_food_spoiled"), 1);
                }
            }
        }

        if (inventoryChanged)
        {
            GameEvents.TriggerGameStateChanged();
        }
    }

    #endregion
    
    #region Save & Load
    /// <summary>
    /// 获取当前库存状态以供存档。
    /// </summary>
    public ItemsState GetState() // <--- 返回类型已更改
    {
        return new ItemsState // <--- 返回类型已更改
        {
            stackedItems = new Dictionary<int, int>(_stackedItems),
            itemInstances = new List<ItemInstance>(_itemInstances) // <--- 名称已更改
        };
    }

    /// <summary>
    /// 从存档数据中恢复库存状态。
    /// </summary>
    public void SetState(ItemsState state) // <--- 参数类型已更改
    {
        _stackedItems = state.stackedItems ?? new Dictionary<int, int>();
        _itemInstances = state.itemInstances ?? new List<ItemInstance>(); // <--- 名称已更改
        
        Debug.Log("ItemManager state loaded.");
        GameEvents.TriggerGameStateChanged();
    }
    #endregion
}