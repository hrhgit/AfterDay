using System.Collections.Generic;

[System.Serializable]
public class ActiveTaskState
{
    public string recipeID;
    public int turnsRemaining;
    public List<string> assignedPawnIDs;
}

