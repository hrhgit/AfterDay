using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#region Auxiliary Classes
// --- 用于支持 LocationManager 的辅助类 ---

/// <summary>
/// 封装一次探索行动的结果，告知调用者获得了什么。
/// </summary>
public class ExplorationResult
{
    public List<CardReward> foundItems = new List<CardReward>();
    public bool isFinalExploration = false;
    public int explorationsLeft;
}

#endregion


/// <summary>
/// 地点管理器，负责处理所有与地点探索相关的运行时逻辑。
/// </summary>
public class LocationManager : MonoBehaviour
{
    #region Singleton
    public static LocationManager Instance { get; private set; }
    #endregion

    // 存储所有地点运行时状态的核心字典 (Key: LocationData的UniqueID)
    private Dictionary<int, LocationRuntimeState> _locationStates = new Dictionary<int, LocationRuntimeState>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 【核心方法】由 EventManager 调用，执行一次完整的探索。
    /// </summary>
    /// <param name="locationData">要探索哪个地点的数据蓝图</param>
    /// <param name="explorer">谁在探索（可选，用于未来扩展）</param>
    /// <returns>包含本次探索所有结果的对象</returns>
    public ExplorationResult PerformExploration(LocationData locationData, RobotState explorer = null)
    {
        var result = new ExplorationResult();
        if (locationData == null)
        {
            Debug.LogError("[LocationManager] PerformExploration 失败: 传入的 locationData 为空。");
            return result;
        }

        LocationRuntimeState state = GetOrCreateLocationState(locationData);

        if (state.remainingExplorations <= 0)
        {
            Debug.Log($"地点 '{locationData.name}' 已没有剩余探索次数。");
            result.explorationsLeft = 0;
            return result;
        }

        // 消耗一次
        // 1) 计算本次要前进的“步数”
        int steps = explorer.search;
        // 不能超过剩余
        steps = Mathf.Clamp(steps, 1, state.remainingExplorations);

        // 2) 计算本次覆盖的槽位区间 [start, end)
        int exploredSoFar = locationData.totalExplorations - state.remainingExplorations;
        int start = exploredSoFar;                    // 0-based
        int end   = Mathf.Min(start + steps, locationData.totalExplorations);

        // 3) 聚合这段区间内的奖励（合并相同卡）
        var merge = new Dictionary<CardData, int>();
        for (int s = start; s < end; s++)
        {
            if (state.perSlotReward != null && s >= 0 && s < state.perSlotReward.Length)
            {
                var r = state.perSlotReward[s];
                if (r != null && r.card != null && r.quantity > 0)
                {
                    if (merge.ContainsKey(r.card)) merge[r.card] += r.quantity;
                    else merge[r.card] = r.quantity;
                }
            }
        }

        foreach (var kv in merge)
            result.foundItems.Add(new CardReward { card = kv.Key, quantity = kv.Value });

        // 4) 消耗步数 & 标记是否结束
        state.remainingExplorations -= (end - start);
        result.explorationsLeft = state.remainingExplorations;
        result.isFinalExploration = (state.remainingExplorations == 0);

        Debug.Log($"探索 '{locationData.name}'：本次步数={steps}，区间[{start}..{end-1}]，获得 {result.foundItems.Count} 种物品；剩余 {state.remainingExplorations} 步。");
        return result;
    }

    #region State Management & Save/Load

    /// <summary>
    /// 获取或创建一个地点的运行时状态。
    /// </summary>
    private LocationRuntimeState GetOrCreateLocationState(LocationData locationBlueprint)
    {
        if (!_locationStates.TryGetValue(locationBlueprint.UniqueID, out LocationRuntimeState state))
        {
            state = new LocationRuntimeState(locationBlueprint);
            _locationStates[locationBlueprint.UniqueID] = state;
            Debug.Log($"[LocationManager] 首次访问地点 '{locationBlueprint.name}'，已初始化其运行时状态。");
        }
        return state;
    }

    /// <summary>
    /// 获取所有地点状态，用于游戏存档。
    /// </summary>
    public Dictionary<int, LocationRuntimeState> GetSaveData()
    {
        return _locationStates;
    }

    /// <summary>
    /// 从存档数据中加载所有地点状态。
    /// </summary>
    public void LoadSaveData(Dictionary<int, LocationRuntimeState> loadedStates)
    {
        _locationStates = loadedStates ?? new Dictionary<int, LocationRuntimeState>();
        Debug.Log($"[LocationManager] 已从存档加载 {_locationStates.Count} 个地点的状态。");
    }

    #endregion
}