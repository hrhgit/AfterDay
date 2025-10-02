using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 游戏所有静态数据的中央存储库。
/// 在游戏启动时加载所有 GameAsset，并提供按ID快速查询的功能。
/// </summary>
public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    // --- 核心修改：使用一个统一的数据库来存储所有资产 ---
    private Dictionary<int, GameAsset> _assetDatabase = new Dictionary<int, GameAsset>();

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
        // 从 "Resources/Data" 文件夹下的所有子文件夹加载所有 GameAsset
        var allAssets = Resources.LoadAll<GameAsset>("Data");

        foreach (var asset in allAssets)
        {
            if (asset == null || asset.UniqueID == 0) continue;

            if (!_assetDatabase.ContainsKey(asset.UniqueID))
            {
                _assetDatabase.Add(asset.UniqueID, asset);
            }
            else
            {
                Debug.LogWarning($"重复的ID: {asset.UniqueID}。已存在资产: {_assetDatabase[asset.UniqueID].name}, 新资产: {asset.name}");
            }
        }
        Debug.Log($"Data loaded: {_assetDatabase.Count} assets indexed.");
    }

    // --- 公共查询方法 ---

    /// <summary>
    /// 【新增】通用的、按ID获取任何类型GameAsset的方法。
    /// </summary>
    public GameAsset GetAsset(int id)
    {
        if (_assetDatabase.TryGetValue(id, out GameAsset data))
        {
            return data;
        }
        Debug.LogError($"资产数据库中未找到ID为 '{id}' 的资产！");
        return null;
    }

    /// <summary>
    /// 【已修改】获取棋子数据的便捷方法（内部调用GetAsset）。
    /// </summary>
    public CardData GetCardData(int id)
    {
        GameAsset asset = GetAsset(id);
        // as 关键字：如果转换成功，则返回PawnData；如果asset不是PawnData类型，则安全地返回null
        return asset as CardData; 
    }

    /// <summary>
    /// 【已修改】获取物品数据的便捷方法。
    /// </summary>
    public ItemData GetItemData(int id)
    {
        GameAsset asset = GetAsset(id);
        return asset as ItemData;
    }
    
    /// <summary>
    /// 【已修改】获取事件数据的便捷方法。
    /// </summary>
    public EventData GetEventData(int id)
    {
        GameAsset asset = GetAsset(id);
        return asset as EventData;
    }
}
