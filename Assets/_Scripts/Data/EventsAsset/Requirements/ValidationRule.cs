using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 所有验证规则的抽象基类。
/// (已集成了通用的、基于标签的验证功能)
/// </summary>
public abstract class ValidationRule : GameAsset
{
    [Header("基础验证：标签")]
    [Tooltip("此规则要求卡牌必须拥有的标签。如果列表为空，则不按标签筛选。")]
    public List<Tags> requiredTags = new List<Tags>();

    /// <summary>
    /// 验证一个被拖拽的物件是否有效。
    /// 子类在重写此方法时，应首先调用 base.IsValid()。
    /// </summary>
    public virtual bool IsValid(GameAsset data, object state)
    {
        var card = data as CardData;
        if (card == null) return false;

        // 步骤2：基础标签校验（避免 LINQ 分配）
        if (requiredTags != null && requiredTags.Count > 0)
        {
            for (int i = 0; i < requiredTags.Count; i++)
            {
                if (!card.HasTag(requiredTags[i]))
                    return false;
            }
        }

        // 步骤3：如果所有基础验证都通过，则返回true，由子类决定后续
        return IsValidCore(card, state);
    }

    /// <summary>
    /// 子类扩展点：在基础验证通过后，做更具体的判断。
    /// </summary>
    protected virtual bool IsValidCore(GameAsset card, object state) => true;
}