using UnityEngine;

[CreateAssetMenu(fileName = "NewModule", menuName = "Game Data/Items/Robot Module")]
public class ModuleData : ItemData
{
    [Header("模块属性")]
    public int movementBonus;
    // ... 其他加成 ...

    private void OnValidate() {
        category = ItemCategory.Module;
        isStackable = false; // 模块不可堆叠
        isConsumable = false; // 模块是用来装备的，不是消耗的
        spoilageTurns = 0; // 模块不会腐烂
        maxStackSize = 1;
    }
}


