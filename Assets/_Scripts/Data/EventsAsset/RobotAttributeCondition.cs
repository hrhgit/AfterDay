using UnityEngine;

// 定义一个枚举，方便在Inspector中选择要检查哪个属性
public enum RobotAttribute
{
    Movement,
    Calculation,
    Search,
    Art
}

[CreateAssetMenu(fileName = "Cond_RobotAttribute", menuName = "Game Data/Unlock Conditions/Robot Attribute")]
public class RobotAttributeCondition : BaseUnlockCondition
{
    public RobotAttribute attributeToCheck;
    public int requiredValue;

    public override bool IsMet(LocationData locationData, RobotState explorer)
    {
        if (explorer == null) return false; // 如果没有探索者，条件不满足

        switch (attributeToCheck)
        {
            case RobotAttribute.Movement:
                return explorer.movement >= requiredValue;
            case RobotAttribute.Calculation:
                return explorer.calculation >= requiredValue;
            case RobotAttribute.Search:
                return explorer.search >= requiredValue;
            case RobotAttribute.Art:
                return explorer.art >= requiredValue;
            default:
                return false;
        }
    }
}