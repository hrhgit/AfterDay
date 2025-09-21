using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    private Dictionary<string, PawnData> _pawnDatabase = new Dictionary<string, PawnData>();
    private Dictionary<string, ItemData> _itemDatabase = new Dictionary<string, ItemData>();
    private Dictionary<string, EventData> _eventDatabase = new Dictionary<string, EventData>();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadAllData();
    }

    private void LoadAllData()
    {
        var allAssets = Resources.LoadAll<GameAsset>("Data");
        foreach (var asset in allAssets)
        {
            if (asset is PawnData pawn) _pawnDatabase[pawn.UniqueID] = pawn;
            else if (asset is ItemData item) _itemDatabase[item.UniqueID] = item;
            else if (asset is EventData evt) _eventDatabase[evt.UniqueID] = evt;
        }
        Debug.Log($"Data loaded: {_pawnDatabase.Count} Pawns, {_itemDatabase.Count} Items, {_eventDatabase.Count} Events.");
    }

    // --- 公共查询方法 ---
    public PawnData GetPawnData(string id)
    {
        if (_pawnDatabase.TryGetValue(id, out PawnData data)) return data;
        Debug.LogError($"PawnData with ID '{id}' not found!");
        return null;
    }

    public ItemData GetItemData(string id)
    {
        if (_itemDatabase.TryGetValue(id, out ItemData data)) return data;
        Debug.LogError($"ItemData with ID '{id}' not found!");
        return null;
    }

    /// <summary>
    /// 根据ID获取单个事件的数据蓝图。
    /// </summary>
    public EventData GetEventData(string id)
    {
        if (_eventDatabase.TryGetValue(id, out EventData data)) return data;
        Debug.LogError($"EventData with ID '{id}' not found!");
        return null;
    }

    /// <summary>
    /// 获取整个事件数据库的只读引用。
    /// </summary>
    public IReadOnlyDictionary<string, EventData> GetEventDatabase()
    {
        return _eventDatabase;
    }
}