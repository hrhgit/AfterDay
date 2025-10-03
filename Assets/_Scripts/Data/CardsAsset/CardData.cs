using System.Collections.Generic;
using UnityEngine;


// 这是所有棋子的“共同祖先”，只包含最基础的通用信息。
public abstract class CardData : GameAsset 
{
    
    [Header("基本信息")]
    public string name;
    [TextArea] public string description;
    public Sprite icon;
    [Header("分类")]
    public List<TagData> tags = new List<TagData>();

    /// <summary>
    /// 按枚举检查是否拥有该标签（含父级链）。
    /// </summary>
    public bool HasTag(Tags tag) => HasTag((int)tag);

    /// <summary>
    /// 按ID检查是否拥有该标签（含父级链）。
    /// 标签集合是固定的，这里直接遍历 + 沿 parent 向上查找即可。
    /// </summary>
    public bool HasTag(int tagId)
    {
        if (tags == null || tags.Count == 0) return false;

        foreach (var t in tags)
        {
            if (MatchesTagOrAncestor(t, tagId)) return true;
        }
        return false;
    }

    /// <summary>
    /// 判断某个标签或其任何祖先是否匹配 targetId。
    /// 使用小步数上限以避免异常数据导致的循环引用；不分配额外内存。
    /// </summary>
    private static bool MatchesTagOrAncestor(TagData tag, int targetId)
    {
        const int MaxHops = 32; // 足够深的安全上限
        var cur = tag;
        int hops = 0;

        while (cur != null && hops++ < MaxHops)
        {
            if (cur.UniqueID == targetId) return true;
            cur = cur.parent;
        }
        return false;
    }
}