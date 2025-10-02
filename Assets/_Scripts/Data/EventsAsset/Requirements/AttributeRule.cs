using UnityEngine;
using System;

[CreateAssetMenu(fileName = "AttributeRule", menuName = "Game/Rule/Attribute")]
public class AttributeRule : ValidationRule
{
    public enum ComparisonType 
    { 
        GreaterThan, 
        LessThan, 
        EqualTo, 
        NotEqualTo,
        GreaterThanOrEqual, 
        LessThanOrEqual     
    }

    public string attributeName;
    public ComparisonType comparison;
    public float targetValue;

    public override bool IsValid(GameAsset data, object state)
    {
        if (!base.IsValid(data, state))
        {
            return false;
        }
        
        // 优先在 state 中查找
        if (state != null && TryGetAttributeValue(state, out float stateValue))
        {
            return Compare(stateValue);
        }
        // 其次在 data 中查找
        if (data != null && TryGetAttributeValue(data, out float dataValue))
        {
            return Compare(dataValue);
        }
        return false;
    }

    private bool TryGetAttributeValue(object obj, out float value)
    {
        value = 0;
        try
        {
            var type = obj.GetType();
            var field = type.GetField(attributeName);
            if (field != null)
            {
                value = Convert.ToSingle(field.GetValue(obj));
                return true;
            }
            var property = type.GetProperty(attributeName);
            if (property != null)
            {
                value = Convert.ToSingle(property.GetValue(obj));
                return true;
            }
        }
        catch { return false; }
        return false;
    }

    private bool Compare(float actualValue)
    {
        switch (comparison)
        {
            case ComparisonType.GreaterThan: return actualValue > targetValue;
            case ComparisonType.LessThan:    return actualValue < targetValue;
            case ComparisonType.EqualTo:     return Mathf.Approximately(actualValue, targetValue);
            case ComparisonType.NotEqualTo:  return !Mathf.Approximately(actualValue, targetValue);
            case ComparisonType.GreaterThanOrEqual: return actualValue >= targetValue;
            case ComparisonType.LessThanOrEqual:    return actualValue <= targetValue;

            default: return false;
        }
    }
}