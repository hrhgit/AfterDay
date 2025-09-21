using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewEvent", menuName = "Game Data/Event")]
public class EventData : GameAsset
{
    [Header("事件基本信息")]
    public string eventName;
    [TextArea] public string description;
    public Sprite icon;
    public enum EventType { Instant, Ongoing }
    [Header("事件类型与耗时")]
    
    public EventType type = EventType.Instant;
    [Tooltip("对于“持续事件”，需要多少回合来完成")]
    public int durationInTurns = 0;

    [Header("触发条件")]
    public EventRequirement requirements;

    [Header("交互式对话 (Ink)")]
    [Tooltip("如果事件包含对话，引用对应的Ink JSON文件")]
    public TextAsset inkStoryJson;

    [Header("产生的结果")]
    [Tooltip("用于处理80%的通用、固定结果")]
    public EventResultData fixedResults;
    [Tooltip("用于处理少数特别复杂、独特的事件结果")]
    public List<Result> customResults;
}

[System.Serializable]
public class EventRequirement
{
    public List<PawnData> requiredPawns;
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
    public List<PawnData> gainPawns;
    public List<ItemRequirement> loseItems;
    public HumanStateChange humanStateChange;
}

[System.Serializable]
public struct ItemReward { public ItemData item; public int quantity; }

[System.Serializable]
public struct HumanStateChange { }


