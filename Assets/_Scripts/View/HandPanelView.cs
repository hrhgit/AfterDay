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
    public enum DisplayType { Pawns, Items }

    [Header("配置")]
    [Tooltip("设置此面板用于显示棋子(Pawns)还是物品(Items)")]
    [SerializeField] private DisplayType panelDisplayType;
    [Tooltip("此面板生成物件时使用的Prefab（Card或Token）")]
    [SerializeField] private GameObject objectPrefab;
    
    [Header("UI 引用")]
    [SerializeField] private Transform container;

    // 管理器引用
    private ItemManager _itemManager;
    private CharacterManager _characterManager;
    private DataManager _dataManager;

    // ... (拖拽相关状态变量保持不变) ...
    private GameObject _placeholder = null;

    private AnimatedLayoutElement _animatedPlaceholder = null;

    private DraggableObject _draggingObject = null;

    public int PlaceholderSiblingIndex { get; private set; }


    void Awake()
    {
        _itemManager = ItemManager.Instance;
        _characterManager = CharacterManager.Instance;
        _dataManager = DataManager.Instance;
    }

    /// <summary>
    /// 核心刷新函数，根据配置的类型来获取数据并生成UI
    /// </summary>
    public void RefreshView()
    {
        // 1. 清理旧的UI对象
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        // 2. 根据面板类型，决定从哪个管理器获取数据并调用对应的实例化方法
        switch (panelDisplayType)
        {
            case DisplayType.Pawns:
                var humanState = _characterManager.GetHumanState();
                if (humanState != null)
                {
                    var humanData = _dataManager.GetCardData(humanState.pawnDataID);
                    // 【已修改】调用专门为棋子准备的方法
                    InstantiatePawnCard(humanData, humanState);
                }
                List<RobotState> robots = _characterManager.GetAllRobotStates();
                foreach (var robot in robots)
                {
                    var robotData = _dataManager.GetCardData(robot.pawnDataID);
                    // 【已修改】调用专门为棋子准备的方法
                    InstantiatePawnCard(robotData, robot);
                }
                break;

            case DisplayType.Items:
                // 【已修改】这里的逻辑需要适配我们之前重构好的 ItemManager
                List<ItemStack> inventory = _itemManager.GetInventory();
                foreach (var itemStack in inventory)
                {
                    // 【已修改】调用专门为物品准备的方法
                    InstantiateItemToken(itemStack);
                }
                break;
        }
    }

    // -----------------------------------------------------------------
    // 【核心修改】将通用的 InstantiateHandObject 拆分为两个具体方法
    // -----------------------------------------------------------------

    /// <summary>
    /// 实例化并填充一个“棋子”卡牌。
    /// </summary>
    private void InstantiatePawnCard(CardData data, object state)
    {
        if (objectPrefab == null) return;
        GameObject newObject = Instantiate(objectPrefab, container);
        
        // 假设您的棋子Prefab上挂载的是 CardView 脚本
        var cardView = newObject.GetComponent<CardView>(); 
        if (cardView != null)
        {
            // 直接调用 CardView 特有的 Populate 方法
            cardView.Populate(data, state);
        }
        else
        {
            Debug.LogWarning($"棋子预制体 '{objectPrefab.name}' 上缺少 CardView 脚本！");
        }
    }

    /// <summary>
    /// 实例化并填充一个“物品”令牌。
    /// </summary>
    private void InstantiateItemToken(ItemStack stack)
    {
        if (objectPrefab == null) return;
        GameObject newObject = Instantiate(objectPrefab, container);
        
        // 假设您的物品Prefab上挂载的是 TokenView 脚本
        var tokenView = newObject.GetComponent<TokenView>();
        if (tokenView != null)
        {
            // 直接调用 TokenView 特有的 Populate 方法
            tokenView.Populate(stack);
        }
        else
        {
            Debug.LogWarning($"物品预制体 '{objectPrefab.name}' 上缺少 TokenView 脚本！");
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