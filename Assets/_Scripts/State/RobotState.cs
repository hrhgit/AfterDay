using UnityEngine;

[System.Serializable]
public class RobotState
{
    public string pawnDataID;
    public string instanceID;

    // 移除 currentEnergy，添加四个核心属性
    public int movement;
    public int calculation;
    public int search;
    public int art;
    
    public RobotCondition condition;
    public enum RobotCondition { Operational, Damaged, Destroyed }

    // 构造函数：从蓝图复制初始属性值
    public RobotState(RobotPawnData blueprint)
    {
        pawnDataID = blueprint.UniqueID;
        instanceID = System.Guid.NewGuid().ToString();
        
        // 将蓝图中的基础属性作为这个实例的初始属性
        movement = blueprint.movement;
        calculation = blueprint.calculation;
        search = blueprint.search;
        art = blueprint.art;
        
        condition = RobotCondition.Operational;
    }
}