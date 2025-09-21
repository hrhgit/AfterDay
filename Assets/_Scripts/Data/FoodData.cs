using UnityEngine;

[CreateAssetMenu(fileName = "NewFood", menuName = "Game Data/Items/Food")]
public class FoodData : ItemData
{
    [Header("食物属性")]
    public float hungerRestored = 20f; // 吃掉后能恢复多少饥饿值

    private void OnValidate() {
        category = ItemCategory.Food;
        isStackable = true;
        isConsumable = true; // 食物当然是可以消耗的
        if (spoilageTurns == 0) spoilageTurns = 10; // 默认食物有10回合保质期
    }
}


