using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 所有验证规则的抽象基类。
/// (已集成了通用的、基于标签的验证功能)
/// </summary>
public abstract class ValidationRule : ScriptableObject
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
        // 步骤1：检查传入的是否为 CardData
        if (!(data is CardData card))
        {
            return false; // 如果拖拽的不是卡牌，则不符合任何基于标签的规则
        }

        // 步骤2：执行内置的标签检测
        if (requiredTags.Any()) // 仅当列表不为空时才进行检测
        {
            // 遍历所有必需的标签
            foreach (var requiredTag in requiredTags)
            {
                // 只要有一个必需的标签卡牌没有，验证就失败
                if (!card.HasTag(requiredTag))
                {
                    return false;
                }
            }
        }

        // 步骤3：如果所有基础验证都通过，则返回true，由子类决定后续
        return true;
    }
}