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

    public bool HasTag(Tags tag)
    {
        // 将传入的enum转换为其对应的整数值，然后进行比较
        return tags.Exists(t => t.id == (int)tag);
    }

    /// <summary>
    /// （可选）按ID检查标签，主要供内部或特殊情况使用。
    /// </summary>
    public bool HasTag(int tagId)
    {
        return tags.Exists(t => t.id == tagId);
    }
}