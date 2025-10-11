using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

[System.Serializable]
public class CardReward
{
    public CardData card; // 引用通用的 CardData 基类
    public int quantity;
}
[System.Serializable]
public class RandomRewardDrop
{
    public CardReward cardReward;
    [Tooltip("获取到此奖励的概率 (0到1)")]
    [Range(0f, 1f)]
    public float dropChance;
}
[CreateAssetMenu(fileName = "Location_", menuName = "Game Data/Location")]
public class LocationData : GameAsset // 假设继承自GameAsset以获得UniqueID
{
    public string name;
    [TextArea] public string description;
    public Sprite locationImage;

    [Header("地点事件")]
    [Tooltip("此地点固有的、始终可用的事件，通常是'探索'。")]
    public EventData inherentEvent;

    [Header("探索设置")]
    [Tooltip("此地点可能发生的、需要条件解锁的隐藏事件列表。")]
    public List<EventData> potentialEvents;
    
    [Header("探索专属设置")]
    [Tooltip("此探索事件固定的总探索次数")]
    public int totalExplorations = 18;

    [FormerlySerializedAs("fixedDistributionRewards")]
    [FormerlySerializedAs("fixedRewards")]
    [Header("探索奖励池")]
    [Tooltip("所有奖励：在最后一次探索时全部获得")]
    public List<RewardPlanItem> allRewards = new List<RewardPlanItem>();
    
    [Tooltip("随机奖励池：每次探索时都有几率获得，且每个只会被获得一次")]
    public List<RandomRewardDrop> randomRewardPool;
}

