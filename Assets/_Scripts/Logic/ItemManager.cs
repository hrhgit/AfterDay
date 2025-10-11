using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// 您需要确保项目中存在这个 ItemStack 类



/// <summary>
/// 管理游戏中所有非角色单位的物品和资源 (已适配统一堆叠逻辑)。
/// </summary>
public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    // 【核心修改】所有物品，无论是否可堆叠或腐烂，都统一存储在这个列表中
    private List<ItemStack> _inventory = new List<ItemStack>();

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // GameEvents.OnTurnEnd += ProcessSpoilage; 
    }

    private void OnDestroy()
    {
        // GameEvents.OnTurnEnd -= ProcessSpoilage;
    }
    #endregion

    #region Initialization & Setup
    public void SetupNewGame(List<CardReward> startingItems)
    {
        _inventory.Clear(); // 只需清空一个列表

        if (startingItems != null)
        {
            foreach (var cardReward in startingItems)
            {
                AddItem(cardReward.card as ItemData, cardReward.quantity);
            }
        }
        
        Debug.Log("ItemManager: New game setup complete.");
        GameEvents.TriggerGameStateChanged();
    }
    #endregion
    
    #region Public API - 物品操作

    /// <summary>
    /// 【已重写】向库存添加物品，采用统一的堆叠逻辑。
    /// </summary>
    public void AddItem(ItemData item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return;

        if (item.isStackable)
        {
            // 对于可堆叠物品 (无论是否腐烂)
            // 查找已存在的、且未满的堆叠
            ItemStack existingStack = _inventory.FirstOrDefault(stack => 
                stack.Data == item && stack.Quantity < item.maxStackSize);

            if (existingStack != null)
            {
                // 找到了，增加数量直到堆叠满
                int spaceLeft = item.maxStackSize - existingStack.Quantity;
                int amountToAdd = Mathf.Min(quantity, spaceLeft);
                existingStack.AddQuantity(amountToAdd);
                quantity -= amountToAdd;
            }

            // 如果还有剩余 (因为填满了旧堆叠或没找到旧堆叠)
            // 则为剩余的物品创建新堆叠
            while (quantity > 0)
            {
                int amountInNewStack = Mathf.Min(quantity, item.maxStackSize);
                _inventory.Add(new ItemStack(item, amountInNewStack));
                quantity -= amountInNewStack;
            }
        }
        else
        {
            // 对于不可堆叠物品，每个都创建一个数量为1的新堆叠
            for (int i = 0; i < quantity; i++)
            {
                _inventory.Add(new ItemStack(item, 1));
            }
        }

        Debug.Log($"Added {item.name}.");
        GameEvents.TriggerGameStateChanged();
    }

    /// <summary>
    /// 【已重写】从库存消耗指定数量的物品。
    /// </summary>
    public void ConsumeItem(int itemID, int quantity = 1)
    {
        if (quantity <= 0) return;

        // 从后往前遍历，以便在移除空堆叠时不会出错
        for (int i = _inventory.Count - 1; i >= 0; i--)
        {
            if (quantity <= 0) break; // 已移除足够数量

            ItemStack stack = _inventory[i];
            if (stack.Data.UniqueID == itemID)
            {
                int amountToRemove = Mathf.Min(quantity, stack.Quantity);
                stack.RemoveQuantity(amountToRemove);
                quantity -= amountToRemove;

                if (stack.Quantity <= 0)
                {
                    _inventory.RemoveAt(i);
                }
            }
        }
        GameEvents.TriggerGameStateChanged();
    }
     
    public void RemoveItem(ItemData item, int quantity = 1)
    {
        ConsumeItem(item.UniqueID, quantity);
    }
    
    #endregion
 
    #region Public API - 查询

    /// <summary>
    /// 【已重写】检查是否有足够数量的特定物品。
    /// </summary>
    public bool HasItem(int itemID, int quantity = 1)
    {
        int totalCount = 0;
        // 遍历统一的库存列表，累加所有匹配ID的物品数量
        foreach(var stack in _inventory)
        {
            if (stack.Data.UniqueID == itemID)
            {
                totalCount += stack.Quantity;
            }
        }
        return totalCount >= quantity;
    }
    
    // CheckRequirements 方法无需修改，因为它依赖于 HasItem

    /// <summary>
    /// 【新增】获取统一库存的完整副本，供UI显示。
    /// </summary>
    public List<ItemStack> GetInventory()
    {
        return new List<ItemStack>(_inventory);
    }

    // GetStackedItems 和 GetItemInstances 方法已废弃，由 GetInventory() 替代

    #endregion

    #region Turn Logic

    /// <summary>
    /// 【已重写】处理物品腐烂。
    /// </summary>
    public void ProcessSpoilage()
    {
        bool inventoryChanged = false;
        for (int i = _inventory.Count - 1; i >= 0; i--)
        {
            var stack = _inventory[i];
            // 只处理需要腐烂的物品堆 (spoilageTurns > 0)
            if (stack.Data.spoilageTurns > 0 && stack.turnsRemaining > 0)
            {
                stack.turnsRemaining--;
                if (stack.turnsRemaining == 0)
                {
                    Debug.Log($"An item '{stack.Data.name}' has spoiled and the stack was removed.");
                    _inventory.RemoveAt(i);
                    inventoryChanged = true;
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
    
    // 存档和读档现在也变得更简单
    
    public List<ItemStack> GetState()
    {
        return _inventory;
    }

    public void SetState(List<ItemStack> state)
    {
        _inventory = state ?? new List<ItemStack>();
        Debug.Log("ItemManager state loaded.");
        GameEvents.TriggerGameStateChanged();
    }
    #endregion
}