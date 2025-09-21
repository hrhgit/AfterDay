using UnityEngine;

[CreateAssetMenu(fileName = "NewResource", menuName = "Game Data/Items/Resource")]
public class ResourceData : ItemData
{
    private void OnValidate() {
        // 设定默认值，可以在Inspector中覆盖
        category = ItemCategory.Resource;
        isStackable = true;
        isConsumable = false; // 材料通常是用来制作的，而不是直接消耗产生效果
        spoilageTurns = 0; // 材料通常不会腐烂
    }
}
