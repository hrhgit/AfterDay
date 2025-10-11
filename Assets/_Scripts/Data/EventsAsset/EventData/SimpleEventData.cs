using UnityEngine;

/// <summary>
/// 一个最基础的具体事件。
/// 它的唯一功能就是在执行时，将其携带的固定奖励（fixedReward）交给 EventManager 处理。
/// </summary>
[CreateAssetMenu(fileName = "Event_Simple_", menuName = "Game Data/Events/Simple Event")]
public class SimpleEventData : EventData // 继承自 EventData
{
    /// <summary>
    /// 实现父类的 Execute 方法。
    /// </summary>
    public override void Execute(EventManager manager, object context)
    {
        Debug.Log($"执行简单事件: '{name}'...");
        
        // 【核心】调用 EventManager 的专业方法来处理奖励，并将自己的 fixedReward 列表作为参数传入
        if (manager != null)
        {
            manager.GrantReward(fixedReward);
        }
    }
}