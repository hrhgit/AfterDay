using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }
    
    private ItemManager _itemManager;
    private CharacterManager _characterManager;
    private LocationManager _locationManager; 

    private void Awake() { Instance = this; }
    private void Start()
    {
        // 【核心修改】通过单例的 Instance 属性自动获取引用
        _itemManager = ItemManager.Instance;
        _characterManager = CharacterManager.Instance;
        _locationManager = LocationManager.Instance;

        // 健壮性检查
        if (_itemManager == null || _characterManager == null || _locationManager == null)
        {
            Debug.LogError("[EventManager] 依赖的一个或多个管理器实例未找到！请确保场景中存在这些管理器。");
        }
    }

    /// <summary>
    /// 【入口】通用的事件执行方法。
    /// </summary>
    public void TriggerEvent(EventData eventData, object context = null)
    {
        if (eventData == null) return;
        
        // 调用事件自己的Execute方法，并将EventManager自身作为参数传入
        eventData.Execute(this, context);
    }

    /// <summary>
    /// 【分派】专门处理探索事件的方法，由 ExplorationEventData 调用。
    /// </summary>
    public void HandleExplorationEvent(ExplorationEventData eventData, RobotState explorer)
    {
        // 这里需要找到探索事件对应的地点
        // 这个逻辑取决于您的设计（例如，事件是否与特定地点强绑定）
        // 假设我们能通过某种方式找到地点
        LocationData targetLocation = FindLocationForEvent(eventData);

        if (_locationManager != null && targetLocation != null)
        {
            // 将请求转发给 LocationManager
            _locationManager.PerformExploration(targetLocation, explorer);
        }
    }

    // 一个辅助方法，用于演示如何找到事件所属的地点
    private LocationData FindLocationForEvent(EventData eventData)
    {
        // 实际项目中，您需要一个 DataManager 来快速查找
        // 这里仅为示例
        // foreach(var location in AllLocations)
        // {
        //    if(location.events.Contains(eventData)) return location;
        // }
        return null; 
    }
    
    /// <summary>
    /// 【已重构】一个通用的奖励发放方法。
    /// 现在可以同时处理物品(ItemData)和角色(RobotPawnData等)奖励。
    /// </summary>
    /// <param name="rewards">包含奖励资产的 CardData 列表。</param>
    public void GrantReward(List<CardReward> rewards)
    {
        if (rewards == null || rewards.Count == 0)
        {
            Debug.Log("奖励列表为空，无需发放。");
            return;
        }

        Debug.Log($"开始发放 {rewards.Count} 种奖励...");

        foreach (var reward in rewards)
        {
            if (reward == null || reward.card == null || reward.quantity <= 0) continue;

            // --- 使用类型判断来分派奖励 ---

            if (reward.card is ItemData item)
            {
                if (_itemManager != null)
                {
                    // 将奖励的数量传递给 AddItem 方法
                    _itemManager.AddItem(item, reward.quantity);
                    Debug.Log($" -> 已发放物品奖励: {item.name} x{reward.quantity}");
                }
            }
            else if (reward.card is RobotPawnData robot)
            {
                if (_characterManager != null)
                {
                    // 根据数量，多次调用 AddPawn 方法
                    for (int i = 0; i < reward.quantity; i++)
                    {
                        _characterManager.AddPawn(robot);
                    }

                    Debug.Log($" -> 已发放机器人奖励: {robot.name} x{reward.quantity}");
                }
            }
        }
    }
}