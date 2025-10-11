using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 代表一个“物品堆”，包含物品的数据和数量。
/// </summary>
[System.Serializable]
public class ItemStack
{
    public ItemData Data { get; private set; }
    public int Quantity { get; private set; }
    public int turnsRemaining; // 用于处理腐烂

    public ItemStack(ItemData data, int quantity = 1)
    {
        Data = data;
        Quantity = quantity;
        turnsRemaining = data.spoilageTurns; // 从蓝图中初始化腐烂回合数
    }

    public void AddQuantity(int amount) { Quantity += amount; }
    public void RemoveQuantity(int amount) { Quantity -= amount; }
}

public class ItemData : CardData // 继承自GameAsset以获得UniqueID
{
    

    [Header("核心特性")]

    public bool isConsumable = false; // 这个物品是否可以被消耗？
    public bool isStackable = true; // 这个物品是否可以堆叠？
    public int maxStackSize = 99; // 如果可以堆叠，最大堆叠数量是多少？

    [Header("时间限制")]
    [Tooltip("物品的保质期（单位：回合）。-1表示永不过期。")]
    public int spoilageTurns = -1; // -1 = 不过期

    // 你甚至可以添加一个方法来方便地检查物品是否会腐烂
    public bool DoesSpoil()
    {
        return spoilageTurns > 0;
    }
}