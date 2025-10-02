using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// 一个通用的、可配置的手牌面板视图。
/// 它可以被设置为显示角色卡牌或物品令牌。
/// </summary>
public class HandPanelView : MonoBehaviour
{
    // 定义这个面板要显示的内容类型
    public enum DisplayType { Pawns, Items }

    [Header("配置")]
    [Tooltip("设置此面板用于显示棋子(Pawns)还是物品(Items)")]
    [SerializeField] private DisplayType panelDisplayType;
    [Tooltip("此面板生成物件时使用的Prefab（Card或Token）")]
    [SerializeField] private GameObject objectPrefab;
    
    [Header("UI 引用")]
    [Tooltip("所有卡牌/令牌UI的父对象")]
    [SerializeField] private Transform container;

    // 管理器引用
    private ItemManager _itemManager;
    private CharacterManager _characterManager;
    private DataManager _dataManager;

    // 拖拽相关运行时状态 (与之前CardHandView相同)
    private GameObject _placeholder = null;
    private AnimatedLayoutElement _animatedPlaceholder = null;
    private DraggableObject _draggingObject = null;
    public int PlaceholderSiblingIndex { get; private set; }

    void Awake()
    {
        // 在Awake时获取引用，因为它可能会在Start之前被HandManager激活
        _itemManager = ItemManager.Instance;
        _characterManager = CharacterManager.Instance;
        _dataManager = DataManager.Instance;
    }

    /// <summary>
    /// 核心刷新函数，根据配置的类型来获取数据并生成UI
    /// </summary>
    public void RefreshView()
    {
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        // 根据面板类型，决定从哪个管理器获取数据
        switch (panelDisplayType)
        {
            case DisplayType.Pawns:
                // 显示所有棋子
                var humanState = _characterManager.GetHumanState();
                if (humanState != null)
                {
                    var humanData = _dataManager.GetCardData(humanState.pawnDataID);
                    InstantiateHandObject(humanData, humanState);
                }
                List<RobotState> robots = _characterManager.GetAllRobotStates();
                foreach (var robot in robots)
                {
                    var robotData = _dataManager.GetCardData(robot.pawnDataID);
                    InstantiateHandObject(robotData, robot);
                }
                break;

            case DisplayType.Items:
                // 显示所有物品
                Dictionary<int, int> stackedItems = _itemManager.GetStackedItems();
                foreach (var itemEntry in stackedItems)
                {
                    var itemData = _dataManager.GetItemData(itemEntry.Key);
                    for (int i = 0; i < itemEntry.Value; i++)
                    {
                        InstantiateHandObject(itemData, null);
                    }
                }
                List<ItemInstance> itemInstances = _itemManager.GetItemInstances();
                foreach (var instance in itemInstances)
                {
                    var itemData = _dataManager.GetItemData(instance.itemID);
                    InstantiateHandObject(itemData, instance);
                }
                break;
        }
    }

    private void InstantiateHandObject(GameAsset data, object state)
    {
        GameObject newObject = Instantiate(objectPrefab, container);
        var draggable = newObject.GetComponent<DraggableObject>();
        if (draggable != null)
        {
            draggable.Populate(data, state);
        }
    }
    
    // --- (此处省略与之前 CardHandView 完全相同的 OnCardBeginDrag, OnCardDrag, OnCardEndDrag 拖拽交互方法) ---
    // 只需要将 CardView, draggingCard 等类型改为 DraggableObject, draggingObject 即可
    #region Drag & Drop Interaction
    
    public void OnCardBeginDrag(DraggableObject draggingObject)
    {
        _draggingObject = draggingObject;

        _placeholder = new GameObject("CardPlaceholder");
        _placeholder.transform.SetParent(container, false);
        
        // 在拖拽开始时，设置初始的索引
        PlaceholderSiblingIndex = draggingObject.transform.GetSiblingIndex();
        _placeholder.transform.SetSiblingIndex(PlaceholderSiblingIndex);

        LayoutElement le = _placeholder.AddComponent<LayoutElement>();
        var originalLE = draggingObject.GetComponent<LayoutElement>();
        if (originalLE != null)
        {
            le.preferredWidth = originalLE.preferredWidth;
            le.preferredHeight = originalLE.preferredHeight;
        }
        _animatedPlaceholder = _placeholder.AddComponent<AnimatedLayoutElement>();
    }

    public void OnCardDrag(PointerEventData eventData)
    {
        if (_placeholder == null || _draggingObject == null) return;

        if (RectTransformUtility.RectangleContainsScreenPoint(container as RectTransform, eventData.position))
        {
            int newSiblingIndex = 0;
            for (int i = 0; i < container.childCount; i++)
            {
                if (container.GetChild(i) == _placeholder.transform) continue;
                if (eventData.position.x > container.GetChild(i).position.x)
                {
                    newSiblingIndex = i + 1;
                }
            }
            _placeholder.transform.SetSiblingIndex(newSiblingIndex);
            
            // 关键！在拖拽过程中持续更新索引值
            PlaceholderSiblingIndex = newSiblingIndex;
            
            _animatedPlaceholder.AnimateToWidth(_draggingObject.GetComponent<LayoutElement>().preferredWidth);
        }
        else
        {
            _animatedPlaceholder.AnimateToWidth(0);
        }
    }
    
    public void OnCardEndDrag()
    {
        if (_placeholder != null)
        {
            Destroy(_placeholder);
        }
        _placeholder = null;
        _animatedPlaceholder = null;
        _draggingObject = null;
    }


    #endregion
}