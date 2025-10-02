using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "OrRule", menuName = "Game/Rule/Logic/OR")]
public class OrRule : ValidationRule
{
    public List<ValidationRule> nestedRules = new List<ValidationRule>();

    public override bool IsValid(GameAsset data, object state)
    {
        if (!base.IsValid(data, state))
        {
            return false;
        }
        foreach (var rule in nestedRules)
        {
            if (rule != null && rule.IsValid(data, state)) return true;
        }
        return false;
    }
}