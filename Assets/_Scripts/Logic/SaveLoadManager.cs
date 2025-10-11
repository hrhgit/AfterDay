using UnityEngine;
using System.IO;

/// <summary>
/// 游戏存档和读档的总控管理器。
/// (已更新以适配新的数据结构)
/// </summary>
public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }

    private string _savePath;
    
    // 依赖的其他管理器
    private ItemManager _itemManager;
    private CharacterManager _characterManager;
    // ... 其他需要存档的管理器

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // 可选：让存档管理器在切换场景时不被销毁
        // DontDestroyOnLoad(gameObject); 
    }

    private void Start()
    {
        _savePath = Path.Combine(Application.persistentDataPath, "savegame.json");
        // 获取依赖
        _itemManager = ItemManager.Instance;
        _characterManager = CharacterManager.Instance;
    }

    /// <summary>
    /// 检查是否存在存档文件。
    /// </summary>
    public bool HasSaveFile()
    {
        return File.Exists(_savePath);
    }

    /// <summary>
    /// 保存游戏。
    /// </summary>
    public void SaveGame()
    {
        Debug.Log("Saving game...");
        
        // 1. 创建一个新的顶层存档状态对象
        GameState state = new GameState();
        
        // 2. 从所有管理器收集各自的状态数据
        // 【已修改】确保字段名和类型匹配
        state.inventoryState = _itemManager.GetState(); // 返回 List<ItemStack>
        state.charactersState = _characterManager.GetState(); // 返回 CharactersState

        // 3. 将整个 GameState 对象序列化为JSON字符串
        string json = JsonUtility.ToJson(state, true);
        
        // 4. 将JSON字符串写入文件
        File.WriteAllText(_savePath, json);
        
        Debug.Log("Game Saved to: " + _savePath);
    }
    
    /// <summary>
    /// 加载游戏。
    /// </summary>
    public void LoadGame()
    {
        if (!HasSaveFile())
        {
            Debug.LogWarning("Load failed: No save file found.");
            return;
        }

        Debug.Log("Loading game...");
        
        // 1. 从文件读取JSON字符串
        string json = File.ReadAllText(_savePath);
        
        // 2. 将JSON反序列化为 GameState 对象
        GameState state = JsonUtility.FromJson<GameState>(json);

        // 3. 将状态数据分发给所有管理器
        // 【已修改】确保传入的参数类型与 SetState 方法匹配
        if (state.inventoryState != null)
        {
            _itemManager.SetState(state.inventoryState); // 需要 List<ItemStack>
        }
        
        if (state.charactersState != null)
        {
            _characterManager.SetState(state.charactersState); // 需要 CharactersState
        }
        
        Debug.Log("Game Loaded.");
        
        // 加载完成后，通常需要触发一次全局状态更新，来刷新UI
        // GameEvents.TriggerGameStateChanged();
    }
}