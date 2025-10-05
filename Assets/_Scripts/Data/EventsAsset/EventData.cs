using UnityEngine;
using System.Collections.Generic;

using UnityEngine;
using System.Collections.Generic;

// 用于定义探索事件可能带来的随机结果
[System.Serializable]
public class ExplorationOutcome
{
    [Tooltip("可能被触发解锁的事件")]
    public EventData eventToUnlock;
    
    [Tooltip("触发此事件的概率 (0到1之间)")]
    [Range(0f, 1f)]
    public float chance;
}

[CreateAssetMenu(fileName = "Event_", menuName = "Game Data/Event")]
public class EventData : GameAsset
{
    public string eventName;
    [TextArea] public string description;

    [Header("解锁条件")]
    [Tooltip("此事件出现必须满足的所有条件。留空则无条件。")]
    public List<BaseUnlockCondition> unlockConditions;

    [Header("探索事件专属设置")]
    [Tooltip("勾选此项，说明这是一个'探索'类型的事件。")]
    public bool isExplorationEvent;

    [Tooltip("如果这是一个探索事件，定义探索可能随机解锁的其他事件及其概率。")]
    public List<ExplorationOutcome> explorationOutcomes;
}

[System.Serializable]
public class EventRequirement
{
    public List<CardData> requiredPawns;
    public List<ItemRequirement> requiredItems;
}

[System.Serializable]
public struct ItemRequirement
{
    public ItemData item;
    public int amount;
}

[System.Serializable]
public class EventResultData
{
    public List<ItemReward> gainItems;
    public List<CardData> gainPawns;
    public List<ItemRequirement> loseItems;
    public HumanStateChange humanStateChange;
}

[System.Serializable]
public struct ItemReward { public ItemData item; public int quantity; }

[System.Serializable]
public struct HumanStateChange { }


