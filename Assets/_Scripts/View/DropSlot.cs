using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

public class DropSlot : MonoBehaviour, IDropHandler
{
    [Header("验证规则")]
    [SerializeField] private List<ValidationRule> validationRules;

    [Header("UI 引用")]
    [SerializeField] private Image iconImage;
    
    [Header("状态与事件")]
    public GameAsset HeldCardData { get; private set; }
    public object HeldCardState { get; private set; }

    [System.Serializable]
    public class CardEvent : UnityEvent<GameAsset, object> {} // 事件现在可以传递状态

    public CardEvent OnCardPlaced;
    public CardEvent OnCardRemoved;


    public void OnDrop(PointerEventData eventData)
    {
        var draggedObject = eventData.pointerDrag.GetComponent<DraggableObject>();
        if (draggedObject != null && IsCardValid(draggedObject))
        {
            PlaceCard(draggedObject);
        }
    }

    private bool IsCardValid(DraggableObject draggable)
    {
        if (HeldCardData != null) return false;
        foreach (var rule in validationRules)
        {
            if (rule == null || !rule.IsValid(draggable.cardData, draggable.state))
            {
                return false;
            }
        }
        return true;
    }
    
    private void PlaceCard(DraggableObject draggable)
    {
        // 1. 存储卡牌数据和状态
        HeldCardData = draggable.cardData;
        HeldCardState = draggable.state; // 存储实例状态

        // 2. 更新UI显示
        if (iconImage != null)
        {
            Sprite spriteToShow = null;
            if (HeldCardData is CardData pawn) spriteToShow = pawn.icon;
            else if (HeldCardData is ItemData item) spriteToShow = item.icon;
            
            iconImage.sprite = spriteToShow;
            iconImage.color = Color.white;
        }

        // 3. 通知逻辑层消耗这张卡牌 (核心修改)
        if (HeldCardData is CardData)
        {
            // 从实例状态中获取 instanceID 来移除棋子
            if (HeldCardState is HumanState human)
            {
                CharacterManager.Instance.RemovePawn(human.instanceID);
            }
            else if (HeldCardState is RobotState robot)
            {
                CharacterManager.Instance.RemovePawn(robot.instanceID);
            }
        }
        else if (HeldCardData is ItemData item)
        {
            // 物品可以直接用 UniqueID 消耗
            ItemManager.Instance.ConsumeItem(item.UniqueID, 1);
        }

        // 4. 触发事件
        OnCardPlaced?.Invoke(HeldCardData, HeldCardState);

        // 5. 销毁被拖拽的UI对象
        Destroy(draggable.gameObject);
    }

    public void ClearSlot()
    {
        var cardToRemove = HeldCardData;
        var stateToRemove = HeldCardState;
        HeldCardData = null;
        HeldCardState = null;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.color = new Color(1, 1, 1, 0.5f);
        }
        
        OnCardRemoved?.Invoke(cardToRemove, stateToRemove);
    }
}