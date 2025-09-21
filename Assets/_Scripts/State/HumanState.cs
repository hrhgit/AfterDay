using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HumanState // 从 EmmaState 改名为 HumanState
{
    // 关键链接：这个状态对应的是哪个 HumanPawnData 蓝图？
    // 虽然目前只有一个艾玛，但未来可能会有其他人类
    public string pawnDataID; 

    // 实例唯一ID：用来区分不同的“人类”实例。
    // 对于艾玛来说，这个ID就是她自己的ID。
    public string instanceID; 

    [Header("动态属性")]
    public int currentHealth; // 当前健康值 (0-initialHealth)
    public int currentMorale; // 当前情绪值 (0-initialMorale)
    public float currentHunger; // 当前饥饿值 (0-100，数值越高越饿)

    // 可以在这里添加其他需要存档的人类状态，例如：
    public enum Sickness { None, Cold, Injured }
    public Sickness sicknessState; // 人类是否生病或受伤
    
    // public List<string> activeTraits; // 比如 "勇敢的", "悲观的" 等特性
    
    // 构造函数，方便初始化人类的状态
    public HumanState(HumanPawnData blueprint, float initialHunger = 0f) // 接收 HumanPawnData 蓝图
    {
        this.pawnDataID = blueprint.UniqueID;
        this.instanceID = blueprint.UniqueID; // 对于艾玛，她的实例ID就是她的蓝图ID
        // 如果未来有多个相同蓝图的人类，则需要生成Guid

        this.currentHealth = blueprint.initialHealth; // 从蓝图获取初始健康值
        this.currentMorale = blueprint.initialMorale; // 从蓝图获取初始情绪值
        this.currentHunger = initialHunger;
        this.sicknessState = Sickness.None;
    }

    // 可以在这里添加一些辅助方法，例如：
    public bool IsAlive() => currentHealth > 0;
    public bool IsStarving() => currentHunger >= 70; // 假设阈值
}