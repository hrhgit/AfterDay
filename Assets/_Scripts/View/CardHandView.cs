using UnityEngine;
using System.Collections.Generic;

// 这个脚本是“视图(View)”，它只负责显示，不存储任何游戏状态
public class CardHandView : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Transform cardContainer; // 所有卡牌UI的父对象
    [SerializeField] private GameObject cardPrefab;   // 单个卡牌的Prefab

    // 假设这些管理器都是单例
    private ItemManager _itemManager;
    private CharacterManager _characterManager;

    private void Start()
    {
        _itemManager = ItemManager.Instance;
        _characterManager = CharacterManager.Instance;
        
        // 订阅游戏状态变化事件
        GameEvents.OnGameStateChanged += RefreshHandView;
        
        // 游戏开始时立即刷新一次
        RefreshHandView();
    }

    private void OnDestroy()
    {
        // 务必在对象销毁时取消订阅，防止内存泄漏
        GameEvents.OnGameStateChanged -= RefreshHandView;
    }

    // 核心刷新函数
    private void RefreshHandView()
    {
        // 1. 清理旧的卡牌UI
        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. 获取最新数据并生成棋子卡牌
        List<RobotState> robots = _characterManager.GetAllRobotStates();
        foreach (RobotState robot in robots)
        {
            // 注意：我们需要一个方法从ID获取到PawnData蓝图
            RobotPawnData robotData = DataManager.Instance.GetPawnData(robot.pawnDataID) as RobotPawnData;
            InstantiateCard(robotData, robot); // 传入蓝图和实时状态
            Debug.Log(1);
        }
        
        // ... (同样的方式生成艾玛的卡牌) ...

        // 3. 获取最新数据并生成可堆叠物品的卡牌
        Dictionary<string, int> stackedItems = _itemManager.GetStackedItems(); // <-- 现在这个函数存在了
        foreach (var itemEntry in stackedItems)
        {
            ItemData itemData = DataManager.Instance.GetItemData(itemEntry.Key);
            if (itemData != null)
            {
                // 传入物品数据和数量
                InstantiateCard(itemData, null, itemEntry.Value); 
            }
        }
    
        // 4. 获取最新数据并生成独特/会腐烂物品的卡牌 (补完逻辑)
        List<ItemInstance> itemInstances = _itemManager.GetItemInstances(); // <-- 使用新增的函数
        foreach (var instance in itemInstances)
        {
            ItemData itemData = DataManager.Instance.GetItemData(instance.itemID);
            if (itemData != null)
            {
                // 传入物品数据和它的实时状态实例 (state)
                // 数量总是1，所以stackCount为0或1
                InstantiateCard(itemData, instance, 1);
            }
        }
    }

    // 实例化并设置单个卡牌
    private void InstantiateCard(GameAsset data, object state, int stackCount = 0)
    {
        GameObject cardObject = Instantiate(cardPrefab, cardContainer);
        CardView cardView = cardObject.GetComponent<CardView>();
        
        // 调用卡牌自己的脚本来填充数据
        cardView.Populate(data, state, stackCount);
    }
}