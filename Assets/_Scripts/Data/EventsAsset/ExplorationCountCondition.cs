using UnityEngine;

[CreateAssetMenu(fileName = "Cond_ExploreCount", menuName = "Game Data/Unlock Conditions/Exploration Count")]
public class ExplorationCountCondition : BaseUnlockCondition
{
    [Tooltip("要求的最小探索次数")]
    public int requiredCount;

    public override bool IsMet(LocationData locationData, RobotState explorer)
    {
        // 假设您的 LocationManager 有一个方法可以获取地点的探索次数
        // if (LocationManager.Instance == null) return false;
        
        // int currentExplorationCount = LocationManager.Instance.GetExplorationCount(locationData.UniqueID);
        // return currentExplorationCount >= requiredCount;
        
        // --- 示例代码 ---
        int currentExplorationCount = 5; // 假设从别处获取到当前探索了5次
        return currentExplorationCount >= requiredCount;
    }
}