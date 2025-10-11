using UnityEngine;

/// <summary>
/// 一个特殊的验证规则，它永远返回 true。
/// 用于表示“无任何限制”或“接受任何卡牌”。
/// </summary>
[CreateAssetMenu(fileName = "Rule_True", menuName = "Game Data/Rules/True Rule")]
public class TrueRule : ValidationRule
{
    /// <summary>
    /// 重写核心验证方法，直接返回true。
    /// </summary>
    protected override bool IsValidCore(GameAsset card, object state)
    {
        return true;
    }
}