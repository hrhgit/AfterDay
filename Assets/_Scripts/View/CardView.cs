using UnityEngine;
using TMPro; // 如果使用TextMeshPro
using UnityEngine.UI;

// 这个脚本负责单个卡牌的显示和交互 (比如拖拽)
public class CardView : MonoBehaviour // ...可以实现IDragHandler等接口
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image iconImage;

    public GameAsset cardData { get; private set; } // 保存这张卡牌对应的数据资产

    // 外部调用的填充方法
    public void Populate(GameAsset data, object state, int stackCount)
    {
        this.cardData = data;

        if (data is PawnData pawnData)
        {
            nameText.text = pawnData.pawnName;
            iconImage.sprite = pawnData.icon;
            // 还可以根据 state (RobotState/HumanState) 更新UI，比如显示能量条
        }
        else if (data is ItemData itemData)
        {
            nameText.text = itemData.itemName;
            iconImage.sprite = itemData.icon;
            
        }
    }
}