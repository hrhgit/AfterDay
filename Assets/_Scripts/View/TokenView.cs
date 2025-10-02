using UnityEngine;
using UnityEngine.UI;

// TokenView也继承自DraggableObject
public class TokenView : DraggableObject
{
    [Header("UI 引用")]
    [SerializeField] private Image iconImage;
    // 令牌可能不需要名字，只需要一个图标

    public override void Populate(GameAsset data, object state)
    {
        this.cardData = data;
        this.state = state;
        if (data is ItemData itemData)
        {
            iconImage.sprite = itemData.icon;
        }
    }
}