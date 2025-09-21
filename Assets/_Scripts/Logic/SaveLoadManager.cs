using UnityEngine;
using System.IO;

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }

    private string _savePath;
    
    // 依赖的其他管理器
    private ItemManager _itemManager;
    private CharacterManager _characterManager;
    // ... 其他需要存档的管理器

    private void Awake() => Instance = this;

    private void Start()
    {
        _savePath = Path.Combine(Application.persistentDataPath, "savegame.json");
        
        // 获取依赖
        _itemManager = ItemManager.Instance;
        _characterManager = CharacterManager.Instance;
    }

    public bool HasSaveFile()
    {
        return File.Exists(_savePath);
    }

    public void SaveGame()
    {
        Debug.Log("Saving game...");
        GameState state = new GameState();
        
        // 从所有管理器收集状态
        state.itemsState = _itemManager.GetState();
        state.charactersState = _characterManager.GetState();
        // ...

        string json = JsonUtility.ToJson(state, true);
        File.WriteAllText(_savePath, json);
        Debug.Log("Game Saved to: " + _savePath);
    }
    
    public void LoadGame()
    {
        if (!HasSaveFile()) return;

        Debug.Log("Loading game...");
        string json = File.ReadAllText(_savePath);
        GameState state = JsonUtility.FromJson<GameState>(json);

        // 将状态分发给所有管理器
        _itemManager.SetState(state.itemsState);
        _characterManager.SetState(state.charactersState);
        // ...
        
        Debug.Log("Game Loaded.");
    }
}