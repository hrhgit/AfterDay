using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "IdRule", menuName = "Game/Rule/ID")]
public class IdRule : ValidationRule
{
    [Tooltip("在表格中定义的、允许放入的卡牌的数字ID列表")]
    public List<int> requiredCardIDs= new List<int>(); 

    public override bool IsValid(GameAsset data, object state)
    {
        if (!base.IsValid(data, state))
        {
            return false;
        }
        if (!requiredCardIDs.Any()) return true;
        return requiredCardIDs.Contains(data.UniqueID);
    }
}