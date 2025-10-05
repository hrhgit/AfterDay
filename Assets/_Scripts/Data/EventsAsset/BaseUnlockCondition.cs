using UnityEngine;

/// <summary>
/// 所有事件解锁条件的抽象基类。
/// 每个继承它的 ScriptableObject 都代表一种独立的解锁逻辑。
/// </summary>
public abstract class BaseUnlockCondition : ScriptableObject
{
    /// <summary>
    /// 检查此条件当前是否被满足。
    /// </summary>
    /// <param name="locationData">事件发生的地点</param>
    /// <param name="explorer">执行探索的机器人状态</param>
    /// <returns>如果满足则为true，否则为false</returns>
    public abstract bool IsMet(LocationData locationData, RobotState explorer);
}