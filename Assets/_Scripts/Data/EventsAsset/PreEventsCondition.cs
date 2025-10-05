using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Cond_PrereqEvents", menuName = "Game Data/Unlock Conditions/Prerequisite Events")]
public class PrereEventsCondition : BaseUnlockCondition
{
    [Tooltip("要求必须完成的所有事件的ID")]
    public List<int> requiredEventIDs;

    public override bool IsMet(LocationData locationData, RobotState explorer)
    {
        // 假设您的 EventManager 有一个方法可以检查事件是否已完成
        if (EventManager.Instance == null) return false;
        
        foreach (int eventId in requiredEventIDs)
        {
            if (!EventManager.Instance.IsEventCompleted(eventId))
            {
                return false; // 只要有一个没完成，条件就不满足
            }
        }
        return true; // 所有都完成了
    }
}