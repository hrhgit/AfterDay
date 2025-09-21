using UnityEngine;

/// <summary>
/// 游戏总管理器，负责协调所有其他管理器，并控制游戏的核心流程和状态。
/// 这是一个单例，并会在场景切换时保持存在。
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Singleton & Dependencies

    public static GameManager Instance { get; private set; }

    [Header("游戏配置")]
    [Tooltip("用于开始新游戏的默认开局剧本。请从项目文件夹中拖入对应的 StartingScenarioData 资产。")]
    [SerializeField] private StartingScenarioData defaultStartingScenario;

    // 依赖的其他管理器
    private SaveLoadManager _saveLoadManager;
    private CharacterManager _characterManager;
    private ItemManager _itemManager;

    #endregion

    #region Game State

    public enum GameStatus { InMenu, Playing, Paused, Dialogue }
    public GameStatus CurrentStatus { get; private set; }

    #endregion

    private void Awake()
    {
        // 设置健壮的单例模式
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 确保GameManager在场景切换时依然存在
    }

    private void Start()
    {
        // 在Start中获取所有其他核心管理器的引用，确保它们都已初始化
        _saveLoadManager = SaveLoadManager.Instance;
        _characterManager = CharacterManager.Instance;
        _itemManager = ItemManager.Instance;

        // 初始状态通常是主菜单
        SetStatus(GameStatus.InMenu);

        // 注意：游戏不会自动开始。
        // 开始新游戏或加载存档的操作，现在应该由UI（例如主菜单按钮）来触发。
    }
    
    #region Game Flow Control

    /// <summary>
    /// 使用指定的剧本开始一个全新的游戏。
    /// </summary>
    public void StartNewGame(StartingScenarioData scenario)
    {
        if (scenario == null)
        {
            Debug.LogError("GameManager: Cannot start new game, the provided Starting Scenario is null!");
            return;
        }
        
        if (scenario.playerCharacterBlueprint == null)
        {
            Debug.LogError($"GameManager: The scenario '{scenario.scenarioName}' is missing a Player Character Blueprint!");
            return;
        }

        Debug.Log($"GameManager: Starting new game with scenario '{scenario.scenarioName}'...");

        // 1. 指挥CharacterManager初始化主角和初始机器人
        _characterManager.SetupNewGame(scenario.playerCharacterBlueprint, scenario.startingRobots);

        // 2. 指挥ItemManager初始化初始物品
        _itemManager.SetupNewGame(scenario.startingItems);

        // 3. 指挥其他所有管理器进行新游戏设置 ...
        // 例如：EventManager.SetupNewGame(); TurnManager.ResetTurns();

        SetStatus(GameStatus.Playing);
    }
    
    /// <summary>
    /// 使用在Inspector中设置的默认剧本开始一个全新的游戏。
    /// 通常由主菜单的“新游戏”按钮调用。
    /// </summary>
    public void StartNewGameWithDefault()
    {
        StartNewGame(defaultStartingScenario);
    }

    /// <summary>
    /// 加载存档游戏。通常由主菜单的“继续游戏”按钮调用。
    /// </summary>
    public void LoadGame()
    {
        if (!_saveLoadManager.HasSaveFile())
        {
            Debug.LogWarning("GameManager: No save file found. Cannot load game.");
            // 可选：在这里禁用“继续游戏”按钮
            return;
        }

        Debug.Log("GameManager: Loading game...");
        
        // 指挥SaveLoadManager执行加载操作
        _saveLoadManager.LoadGame();
        
        SetStatus(GameStatus.Playing);
    }

    /// <summary>
    // 保存当前游戏。由UI按钮或自动保存在特定时机调用。
    /// </summary>
    public void SaveGame()
    {
        if (CurrentStatus != GameStatus.Playing)
        {
            Debug.LogWarning("GameManager: Can only save while playing.");
            return;
        }
        _saveLoadManager.SaveGame();
    }
    
    #endregion

    #region State Management

    /// <summary>
    /// 设置并管理游戏的当前状态。
    /// </summary>
    public void SetStatus(GameStatus newStatus)
    {
        CurrentStatus = newStatus;
        Debug.Log($"Game status changed to: {newStatus}");
        
        // 在这里可以根据不同状态执行逻辑，例如：
        switch (newStatus)
        {
            case GameStatus.Paused:
                Time.timeScale = 0; // 暂停游戏时间
                break;
            case GameStatus.Playing:
                Time.timeScale = 1; // 恢复游戏时间
                break;
            // 其他状态...
        }
    }

    #endregion
}