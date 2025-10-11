using UnityEngine;
using UnityEngine.UI;
using TMPro;
// TokenView也继承自DraggableObject
public class TokenView : DraggableObject
{
    [Header("UI 引用")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI quantityText; // 已将public改为SerializeField，更规范

    private ItemStack _currentItem; // 用于存储当前Token代表的物品堆

    /// <summary>
    /// 【已重写】使用 ItemStack 来填充Token的显示。
    /// 这个方法取代了旧的 Populate 方法。
    /// </summary>
    public void Populate(ItemStack itemStack)
    {
        _currentItem = itemStack;

        if (_currentItem == null || _currentItem.Data == null) 
        {
            // 如果数据无效，隐藏整个Token
            gameObject.SetActive(false);
            return;
        }

        // 更新图标
        iconImage.sprite = _currentItem.Data.icon;

        // 【核心修改】根据物品是否可堆叠以及数量来更新 quantityText
        if (_currentItem.Data.isStackable && _currentItem.Quantity > 1)
        {
            quantityText.text = _currentItem.Quantity.ToString();
            quantityText.gameObject.SetActive(true); // 显示数量文本
        }
        else
        {
            // 如果物品不可堆叠或数量为1，则隐藏数量文本
            quantityText.gameObject.SetActive(false);
        }
    }

    // 为了兼容 DraggableObject 基类，我们可以保留旧的 Populate 方法，
    // 或者更好地，修改基类使其更灵活。
    // 这里我们暂时保留它，并让它内部调用新的方法（但不推荐，因为类型不匹配）。
    // 在您的项目中，最好的做法是评估是否需要 DraggableObject 的 Populate 方法。
    
}