using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ResourceCost
{
    public ResourceData resource;
    public int amount;
}

[CreateAssetMenu(fileName = "NewActionRecipe", menuName = "Game Data/Action Recipe")]
public class ActionRecipeData : GameAsset
{
    public string actionName;

    [Header("Requirements")]
    public LocationData requiredLocation;
    public List<PawnData> requiredPawns = new List<PawnData>();
    public List<ResourceCost> costs = new List<ResourceCost>();

    [Header("Execution")]
    public int turnsToComplete = 1;
    public List<GameAsset> rewards = new List<GameAsset>();
    [TextArea]
    public string successDescription;
}
