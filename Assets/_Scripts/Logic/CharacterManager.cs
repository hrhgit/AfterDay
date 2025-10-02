using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 管理游戏中所有“活物”（棋子）的运行时状态。
/// 负责追踪、创建、移除和修改所有人类和机器人的状态。
/// </summary>
public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance { get; private set; }

    // 依赖的其他管理器
    private DataManager _dataManager;

    // 运行时状态
    private List<HumanState> _humanCharacters = new List<HumanState>();
    private List<RobotState> _robotFleet = new List<RobotState>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    private void Start()
    {
        // 在Start中获取依赖，确保其他管理器的Instance已经准备好
        _dataManager = DataManager.Instance;
    }

    #region Initialization & Setup

    /// <summary>
    /// 初始化新游戏的角色状态。
    /// </summary>
    public void SetupNewGame(HumanPawnData playerBlueprint, List<RobotPawnData> startingRobots)
    {
        _humanCharacters.Clear();
        _robotFleet.Clear();
        
        // 创建主角的初始状态
        _humanCharacters.Add(new HumanState(playerBlueprint));

        // 根据剧本添加初始机器人
        if (startingRobots != null)
        {
            foreach (var robotBlueprint in startingRobots)
            {
                AddPawn(robotBlueprint);
            }
        }
        
        Debug.Log("CharacterManager: New game setup complete.");
        // 注意：这里的GameEvents.TriggerGameStateChanged()会被AddPawn多次调用，可以优化
    }

    #endregion

    #region Public Queries

    /// <summary>
    /// 获取所有机器人状态的列表副本。返回副本是为了安全，防止外部直接修改列表。
    /// </summary>
    public List<RobotState> GetAllRobotStates() => new List<RobotState>(_robotFleet);
    
    /// <summary>
    /// 获取主角的状态。
    /// </summary>
    public HumanState GetHumanState() => _humanCharacters.Count > 0 ? _humanCharacters[0] : null;

    #endregion

    #region Public API - State Modification

    /// <summary>
    /// 添加一个新的棋子（当前只实现机器人）。
    /// </summary>
    public void AddPawn(CardData pawnBlueprint)
    {
        if (pawnBlueprint is RobotPawnData robotData)
        {
            _robotFleet.Add(new RobotState(robotData));
            Debug.Log($"New robot '{robotData.name}' added to fleet.");
            GameEvents.TriggerGameStateChanged();
        }
        // else if (pawnBlueprint is HumanPawnData humanData) { ... } // 未来可扩展以添加新的人类
    }

    /// <summary>
    /// 根据InstanceID移除一个棋子（人类或机器人）。
    /// </summary>
    public void RemovePawn(int instanceID)
    {
        // 尝试从机器人舰队中移除
        int removedCount = _robotFleet.RemoveAll(r => r.instanceID == instanceID);

        if (removedCount > 0)
        {
            Debug.Log($"Robot with instance ID '{instanceID}' was removed from the fleet.");
            GameEvents.TriggerGameStateChanged();
            return;
        }

        // 尝试从人类角色中移除
        removedCount = _humanCharacters.RemoveAll(h => h.instanceID == instanceID);
        if (removedCount > 0)
        {
            Debug.Log($"Human with instance ID '{instanceID}' was removed.");
            // 注意：如果移除的是艾玛，可能需要触发游戏结束逻辑
            GameEvents.TriggerGameStateChanged();
            return;
        }

        Debug.LogWarning($"RemovePawn failed: No pawn with instance ID '{instanceID}' was found.");
    }

    /// <summary>
    /// 修改指定人类的状态。
    /// </summary>
    public void ModifyHumanState(int humanInstanceID, int healthChange, int moraleChange, float hungerChange)
    {
        HumanState target = _humanCharacters.Find(h => h.instanceID == humanInstanceID);
        if (target == null)
        {
            Debug.LogWarning($"ModifyHumanState failed: Human with ID '{humanInstanceID}' not found.");
            return;
        }

        HumanPawnData blueprint = _dataManager.GetCardData(target.pawnDataID) as HumanPawnData;
        if (blueprint == null) return;

        target.currentHealth += healthChange;
        target.currentMorale += moraleChange;
        target.currentHunger += hungerChange;

        target.currentHealth = Mathf.Clamp(target.currentHealth, 0, blueprint.initialHealth);
        target.currentMorale = Mathf.Clamp(target.currentMorale, 0, blueprint.initialMorale);
        target.currentHunger = Mathf.Clamp(target.currentHunger, 0, 100f);

        Debug.Log($"Modified state for human '{target.pawnDataID}'. New Health: {target.currentHealth}");
        GameEvents.TriggerGameStateChanged();
    }

    /// <summary>
    /// 修改指定机器人的核心属性。
    /// </summary>
    public void ModifyRobotState(
        int robotInstanceID, 
        int movementChange = 0, 
        int calculationChange = 0, 
        int searchChange = 0, 
        int artChange = 0, 
        RobotState.RobotCondition? newCondition = null)
    {
        RobotState target = _robotFleet.Find(r => r.instanceID == robotInstanceID);
        if (target == null)
        {
            Debug.LogWarning($"ModifyRobotState failed: Robot with ID '{robotInstanceID}' not found.");
            return;
        }

        target.movement += movementChange;
        target.calculation += calculationChange;
        target.search += searchChange;
        target.art += artChange;

        target.movement = Mathf.Clamp(target.movement, 0, 10);
        target.calculation = Mathf.Clamp(target.calculation, 0, 10);
        target.search = Mathf.Clamp(target.search, 0, 10);
        target.art = Mathf.Clamp(target.art, 0, 10);

        if (newCondition.HasValue)
        {
            target.condition = newCondition.Value;
        }

        Debug.Log($"Modified attributes for robot '{target.pawnDataID}'. New Movement: {target.movement}");
        GameEvents.TriggerGameStateChanged();
    }

    /// <summary>
    /// 直接设置指定机器人的核心属性到特定值。
    /// 未提供的属性将保持不变。
    /// </summary>
    public void SetRobotAttributes(
        int robotInstanceID, 
        int? movement = null, 
        int? calculation = null, 
        int? search = null, 
        int? art = null)
    {
        RobotState target = _robotFleet.Find(r => r.instanceID == robotInstanceID);
        if (target == null)
        {
            Debug.LogWarning($"SetRobotAttributes failed: Robot with ID '{robotInstanceID}' not found.");
            return;
        }

        // 使用 HasValue 检查参数是否被传入了值
        // 如果传入了，就更新属性；否则，保持原样
        if (movement.HasValue) target.movement = movement.Value;
        if (calculation.HasValue) target.calculation = calculation.Value;
        if (search.HasValue) target.search = search.Value;
        if (art.HasValue) target.art = art.Value;

        // 确保所有属性值仍在 [0, 10] 的合法范围内
        target.movement = Mathf.Clamp(target.movement, 0, 10);
        target.calculation = Mathf.Clamp(target.calculation, 0, 10);
        target.search = Mathf.Clamp(target.search, 0, 10);
        target.art = Mathf.Clamp(target.art, 0, 10);

        Debug.Log($"Set attributes for robot '{target.pawnDataID}'. New Movement: {target.movement}");
        GameEvents.TriggerGameStateChanged();
    }
    #endregion

    #region Save & Load

    /// <summary>
    /// 获取当前所有角色的状态以供存档。
    /// </summary>
    public CharactersState GetState()
    {
        return new CharactersState
        {
            humanCharacters = new List<HumanState>(_humanCharacters),
            robotFleet = new List<RobotState>(_robotFleet)
        };
    }

    /// <summary>
    /// 从存档数据中恢复所有角色的状态。
    /// </summary>
    public void SetState(CharactersState state)
    {
        if (state == null) return;
        
        _humanCharacters = state.humanCharacters ?? new List<HumanState>();
        _robotFleet = state.robotFleet ?? new List<RobotState>();
        
        Debug.Log("CharacterManager state loaded.");
        GameEvents.TriggerGameStateChanged();
    }

    #endregion
}