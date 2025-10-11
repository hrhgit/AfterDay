using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 【抽象基类】所有游戏事件的“共同祖先”。
/// 它定义了所有事件都必须具备的通用属性。
/// </summary>
public abstract class EventData : GameAsset 
{
    [Header("事件通用属性")]
    public string name;
    public string title;
    [TextArea] public string description;

    [Header("事件验证规则")]
    [Tooltip("【必须满足】才能执行事件的验证规则列表")]
    public List<ValidationRule> mandatoryValidations;

    [Tooltip("【非必须满足】的验证规则列表。满足后可能会有额外奖励或效果")]
    public List<ValidationRule> optionalValidations;

    [Tooltip("完成此事件后的固定奖励")]
    public List<CardReward> fixedReward;

    /// <summary>
    /// 【核心】一个抽象方法，供子类实现各自独特的事件执行逻辑。
    /// </summary>
    public abstract void Execute(EventManager manager, object context); 
}


