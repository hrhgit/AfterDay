using System.Collections.Generic;
using UnityEngine;

public class ItemData : CardData // 继承自GameAsset以获得UniqueID
{
    

    [Header("核心特性")]
    public List<TagData> tags = new List<TagData>();

    public bool isConsumable = false; // 这个物品是否可以被消耗？
    public bool isStackable = true; // 这个物品是否可以堆叠？
    public int maxStackSize = 99; // 如果可以堆叠，最大堆叠数量是多少？

    [Header("时间限制")]
    [Tooltip("物品的保质期（单位：回合）。0表示永不过期。")]
    public int spoilageTurns = 0; // 0 = 不过期

    // 你甚至可以添加一个方法来方便地检查物品是否会腐烂
    public bool DoesSpoil()
    {
        return spoilageTurns > 0;
    }
    
    public bool HasTag(Tags tag)
    {
        // 将传入的enum转换为其对应的整数值，然后进行比较
        return tags.Exists(t => t.id == (int)tag);
    }

    /// <summary>
    /// （可选）按ID检查标签，主要供内部或特殊情况使用。
    /// </summary>
    public bool HasTag(int tagId)
    {
        return tags.Exists(t => t.id == tagId);
    }
}