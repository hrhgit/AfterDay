using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 负责触发和管理游戏事件的生命周期。
/// </summary>
public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    // 依赖的其他管理器
    private DataManager _dataManager;
    private ItemManager _itemManager;
    private DialogueManager _dialogueManager;

    // 运行时状态
    private readonly List<ActiveEventState> _activeEvents = new List<ActiveEventState>();
    private IReadOnlyDictionary<string, EventData> _eventDatabase; // 从DataManager获取，设为只读

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // 1. 在Start中获取依赖，确保其他管理器的Instance已经准备好
        _dataManager = DataManager.Instance;
        _itemManager = ItemManager.Instance;
        _dialogueManager = DialogueManager.Instance;

        // 2. 从DataManager获取完整的事件数据库
        _eventDatabase = _dataManager.GetEventDatabase();

        // 订阅回合结束事件
        GameEvents.OnTurnEnd += ProcessTurn;
    }

    private void OnDestroy()
    {
        // 在OnDestroy中取消订阅
        GameEvents.OnTurnEnd -= ProcessTurn;
        
    }

    public void TryTriggerEvent(string eventID)
    {
        if (!_eventDatabase.TryGetValue(eventID, out var eventData))
        {
            Debug.LogWarning($"Event '{eventID}' not found in database.");
            return;
        }

        if (!_itemManager.CheckRequirements(eventData.requirements))
        {
            Debug.Log($"Cannot trigger event '{eventData.eventName}': Requirements not met.");
            return;
        }

        _itemManager.ConsumeRequirements(eventData.requirements); // 假设ItemManager有此方法

        if (eventData.type == EventData.EventType.Ongoing)
        {
            _activeEvents.Add(new ActiveEventState { eventDataID = eventID, turnsRemaining = eventData.durationInTurns });
            Debug.Log($"Ongoing event '{eventData.eventName}' started. Duration: {eventData.durationInTurns} turns.");
        }
        else // Instant
        {
            // 3. 调用提取出的通用方法
            ExecuteEvent(eventData);
        }
        GameEvents.TriggerGameStateChanged();
    }

    private void ProcessTurn()
    {
        for (int i = _activeEvents.Count - 1; i >= 0; i--)
        {
            _activeEvents[i].turnsRemaining--;
            if (_activeEvents[i].turnsRemaining <= 0)
            {
                if (_eventDatabase.TryGetValue(_activeEvents[i].eventDataID, out var finishedEventData))
                {
                    // 3. 调用提取出的通用方法
                    ExecuteEvent(finishedEventData);
                }
                _activeEvents.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 3. 新增：执行一个事件（判断是否需要对话）
    /// </summary>
    private void ExecuteEvent(EventData eventData)
    {
        if (eventData.inkStoryJson != null)
        {
            _dialogueManager.StartDialogue(eventData.inkStoryJson, eventData);
        }
        else
        {
            ResolveEventResults(eventData);
        }
    }

    public void ResolveEventResults(EventData eventData)
    {
        Debug.Log($"Resolving results for event '{eventData.eventName}'...");

        // 处理固定结果
        if (eventData.fixedResults != null)
        {
            // ... (处理 gainItems, loseItems, humanStateChange 等) ...
        }

        // 处理自定义结果资产
        if (eventData.customResults != null)
        {
            foreach (Result result in eventData.customResults)
            {
                result?.Execute(this); // 使用 ?. 安全调用
            }
        }
        GameEvents.TriggerGameStateChanged();
    }
}