using UnityEngine;
public enum ItemCategory
{
    Resource,   // 通用资源，如零件
    Food,       // 食物类
    Energy,     // 能源类，如电池
    Module,     // 机器人模块
    Special,    // 特殊/剧情物品
    Medical     // 医疗用品 (未来扩展)
}
public abstract class ItemData : GameAsset // 继承自GameAsset以获得UniqueID
{
    [Header("物品基本信息")]
    public string itemName;
    [TextArea] public string description;
    public Sprite icon; // 作为卡牌时的图标

    [Header("核心特性")]
    public ItemCategory category; // 物品的类别
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
}