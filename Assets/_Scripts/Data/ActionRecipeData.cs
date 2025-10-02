using System.Collections.Generic;
using UnityEngine;



[CreateAssetMenu(fileName = "NewActionRecipe", menuName = "Game Data/Action Recipe")]
public class ActionRecipeData : GameAsset
{
    public string actionName;

    [Header("Requirements")]
    public LocationData requiredLocation;
    public List<CardData> requiredPawns = new List<CardData>();
    

    [Header("Execution")]
    public int turnsToComplete = 1;
    public List<GameAsset> rewards = new List<GameAsset>();
    [TextArea]
    public string successDescription;
}
