using UnityEngine;
using UnityEngine.UI;
using TMPro;

// CardView现在继承自DraggableObject
public class CardView : DraggableObject
{
    [Header("UI 引用")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image iconImage;

    // 重写Populate方法，实现自己的UI填充逻辑
    public override void Populate(GameAsset data, object state)
    {
        this.cardData = data;
        this.state = state;
        if (data is CardData pawnData)
        {
            nameText.text = pawnData.name;
            iconImage.sprite = pawnData.icon;
        }
    }
}