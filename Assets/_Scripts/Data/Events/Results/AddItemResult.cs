using UnityEngine;

[CreateAssetMenu(fileName = "AddItemResult", menuName = "Game Data/Results/Add Item")]
public class AddItemResult : Result
{
    public ItemData itemToAdd;
    public int quantity = 1;

    public override void Execute(MonoBehaviour controller)
    {
        ItemManager.Instance.AddItem(itemToAdd, quantity);
        Debug.Log($"[Result] Added {quantity} of {itemToAdd.itemName} to inventory.");
    }
}


