using System.Collections.Generic;

[System.Serializable]
public class ActiveTaskState
{
    public int recipeID;
    public int turnsRemaining;
    public List<int> assignedPawnIDs;
}

