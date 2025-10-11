using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Data;
using ExcelDataReader;
using System.Collections.Generic;

#region Pre-Processing Data Structures
// --- 与表头预处理相关的数据结构 ---

public class ColumnInfo
{
    public int Index;
    public string FieldName;
    public string Keyword;
    public string KeywordData;
}

#endregion

public abstract class BaseDataImporter
{

    #region Configurable Rules
    protected virtual int DataStartRow => 5;
    protected virtual string IdColumnName => "ID";
    #endregion

    public void Import()
    {
        Process();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"导入流程 '{this.GetType().Name}' 完成！");
    }

    protected abstract void Process();


    #region Header Pre-Processing
    
    /// <summary>
    /// 基础表头解析器。
    /// (此方法保持不变)
    /// </summary>
    protected List<ColumnInfo> ParseHeader(DataTable table)
    {
        var infoList = new List<ColumnInfo>();
        if (table.Rows.Count < 3)
        {
            Debug.LogError("[BaseDataImporter] 表头格式不正确，数据行至少需要3行 (对应Excel的2-4行)！");
            return infoList;
        }

        // ... (内部逻辑保持不变，依然是读取第1, 3, 4行)
        var row3_Keyword = table.Rows[1].ItemArray;
        var row4_KeywordData = table.Rows[2].ItemArray;
        
        for (int i = 0; i < table.Columns.Count; i++)
        {
            DataColumn column = table.Columns[i];
            string fieldName = column.ColumnName;

            if (string.IsNullOrEmpty(fieldName)) continue;

            infoList.Add(new ColumnInfo
            {
                Index = i,
                FieldName = fieldName,
                Keyword = row3_Keyword[i]?.ToString().Trim().ToLower(),
                KeywordData = row4_KeywordData[i]?.ToString().Trim()
            });
        }
        return infoList;
    }

    #endregion
    
    
    
    #region Helper Methods (通用工具箱 - 保持不变)

    #region 一些方法
    
    protected bool IsRowEmpty(DataRow row)
    {
        if (row == null) return true;
        foreach (var value in row.ItemArray)
        {
            if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                return false;
        }
        return true;
    }

    protected DataTable ReadExcelSheet(string filePath, string sheetName)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"Excel文件未找到: {filePath}");
            return null;
        }
        try
        {
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
                    });
                    
                    DataTableCollection tables = result.Tables;
                    DataTable table = null;

                    if (string.IsNullOrEmpty(sheetName)) table = tables[0];
                    else if (tables.Contains(sheetName)) table = tables[sheetName];

                    if (table == null)
                    {
                        Debug.LogError($"在 {filePath} 中未找到工作表 '{sheetName ?? "默认第一个"}'.");
                        return null;
                    }

                    foreach (DataColumn column in table.Columns)
                    {
                        column.ColumnName = column.ColumnName.Trim();
                    }
                    return table;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"读取Excel文件 {filePath} 时出错: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// 【已重构 - 采用您的更优方案】
    /// 加载项目中所有指定类型的 ScriptableObject 资产。
    /// 它通过 AssetDatabase 全局搜索，不再局限于 Resources 文件夹。
    /// </summary>
    /// <typeparam name="T">要加载的资产类型 (必须继承自 GameAsset)。</typeparam>
    /// <returns>一个以 UniqueID 为键，资产对象为值的字典。</returns>
    protected Dictionary<int, GameAsset> LoadAllAssets<T>() where T : GameAsset
    {
        var assetCache = new Dictionary<int, GameAsset>();
        
        // 1. 使用 AssetDatabase.FindAssets 按类型搜索项目中所有匹配资产的GUID
        // "t:{typeof(T).Name}" 会生成例如 "t:TagData" 或 "t:CardData" 的搜索指令
        var allGuids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        
        // 2. 遍历所有找到的GUID
        foreach (var guid in allGuids)
        {
            // 3. 根据GUID获取资产的路径
            var path = AssetDatabase.GUIDToAssetPath(guid);
            
            // 4. 根据路径加载资产
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            
            // 5. 将加载成功的资产添加到字典中
            if (asset != null && !assetCache.ContainsKey(asset.UniqueID))
            {
                assetCache.Add(asset.UniqueID, asset);
            }
        }
        
        Debug.Log($"[LoadAllAssets] 通过全局搜索，加载了 {assetCache.Count} 个 '{typeof(T).Name}' 类型的资产。");
        return assetCache;
    }

    
    protected static Sprite LoadSprite(string basePath, string iconName)
    {
        if (string.IsNullOrWhiteSpace(iconName)) return null;
        
        string normalizedBasePath = basePath.Replace('\\', '/').TrimStart('/');
        string fullPath = $"Assets/Resources/{normalizedBasePath}/{iconName}.png";
        
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);
        if (sprite == null) Debug.LogWarning($"加载失败! 未能在路径 '{fullPath}' 找到Sprite资源。");
        return sprite;
    }
    
    protected T GetOrCreateAsset<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }

    protected string SanitizeFileName(string name)
    {
        string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        foreach (char c in invalidChars)
        {
            name = name.Replace(c.ToString(), "");
        }
        return name.Replace(" ", "_");
    }
    #endregion
    
    #region 填充方法
    
    /// <summary>
    /// 【已重构】根据一个 "ID:Quantity" 格式的字符串，从一个指定的缓存字典中查找资产并返回 CardReward 列表。
    /// </summary>
    /// <param name="rewardString">从Excel读取的原始字符串，例如 "20001:3;101:1"。</param>
    /// <param name="cache">用于查找资产的缓存字典 (Key: UniqueID, Value: GameAsset)。</param>
    /// <returns>一个包含所有已解析奖励的 List<CardReward>。</returns>
    protected List<CardReward> ParseCardRewards(string rewardString, Dictionary<int, GameAsset> cache)
    {
        var rewards = new List<CardReward>();
        if (string.IsNullOrEmpty(rewardString) || cache == null) return rewards;

        // 按分号分割每个奖励条目
        string[] rewardEntries = rewardString.Split(';');
        foreach (var entry in rewardEntries)
        {
            if (string.IsNullOrWhiteSpace(entry)) continue;

            // 按冒号分割ID和数量
            string[] parts = entry.Split(':');
    
            if (int.TryParse(parts[0].Trim(), out int id))
            {
                // 默认数量为1
                int quantity = 1;
                if (parts.Length > 1)
                {
                    int.TryParse(parts[1].Trim(), out quantity);
                }
                quantity = Mathf.Max(1, quantity); // 确保数量至少为1

                // 从传入的 cache 参数中查找对应的 CardData 资产
                if (cache.TryGetValue(id, out GameAsset foundAsset) && foundAsset is CardData card)
                {
                    rewards.Add(new CardReward { card = card, quantity = quantity });
                }
                else
                {
                    Debug.LogWarning($"[ParseCardRewards] 解析奖励时，在传入的缓存中找不到ID为 '{id}' 的CardData资产。");
                }
            }
        }
        return rewards;
    }
    
    protected List<RandomRewardDrop> ParseRandomRewardDrops(string rewardString, Dictionary<int, GameAsset> cache)
    {
        var randomRewards = new List<RandomRewardDrop>();
        if (string.IsNullOrEmpty(rewardString)) return randomRewards;

        string[] rewardEntries = rewardString.Split(';');
        foreach (var entry in rewardEntries)
        {
            if (string.IsNullOrWhiteSpace(entry)) continue;

            string[] parts = entry.Split(':');
            if (parts.Length < 3) continue;

            if (int.TryParse(parts[0].Trim(), out int id) &&
                int.TryParse(parts[1].Trim(), out int quantity) &&
                float.TryParse(parts[2].Trim(), out float chance))
            {
                if (cache.TryGetValue(id, out GameAsset foundAsset) && foundAsset is CardData card)
                {
                    var cardReward = new CardReward { card = card, quantity = quantity };
                    randomRewards.Add(new RandomRewardDrop { cardReward = cardReward, dropChance = chance });
                }
            }
        }
        return randomRewards;
    }
    
    /// <summary>
    /// 【已修正】根据字段名，从DataRow中安全地获取并解析值。
    /// </summary>
    protected T GetValue<T>(DataRow row, Dictionary<string, ColumnInfo> headerMap, string fieldName)
    {
        // 【核心修改】字典的值类型现在是 ColumnInfo
        if (headerMap.TryGetValue(fieldName, out ColumnInfo columnInfo))
        {
            // 从 ColumnInfo 中获取列索引
            object cellValue = row[columnInfo.Index];
        
            // 类型转换逻辑保持不变
            if (typeof(T) == typeof(int)) return (T)(object)ParseInt(cellValue);
            if (typeof(T) == typeof(string)) return (T)(object)ParseString(cellValue);
            if (typeof(T) == typeof(bool)) return (T)(object)ParseBool(cellValue);
            if (typeof(T) == typeof(float)) return (T)(object)ParseFloat(cellValue);
        }
        return default(T); // 如果找不到列，返回默认值
    }
    /// <summary>
    /// 【新增的通用工具】根据一个用分号分隔的ID字符串，从一个缓存字典中查找并返回所有匹配的资产列表。
    /// </summary>
    /// <typeparam name="T">要查找的资产的具体类型 (例如 TagData, ValidationRule)。</typeparam>
    /// <param name="idString">从Excel单元格读取的、用分号分隔的ID字符串 (例如 "1;11;21")。</param>
    /// <param name="cache">用于查找的缓存字典 (Key: UniqueID, Value: GameAsset)。</param>
    /// <returns>一个包含所有已找到且类型匹配的资产的 List<T>。</returns>
    protected List<T> FindAssetsInCache<T>(string idString, Dictionary<int, GameAsset> cache) where T : GameAsset
    {
        var foundAssets = new List<T>();
        if (string.IsNullOrEmpty(idString) || cache == null || cache.Count == 0)
        {
            return foundAssets;
        }

        string[] ids = idString.Split(';');
        foreach (var idStr in ids)
        {
            if (int.TryParse(idStr.Trim(), out int id))
            {
                // 1. 尝试从缓存中查找ID
                if (cache.TryGetValue(id, out GameAsset foundAsset))
                {
                    // 2. 检查找到的资产是否是我们期望的类型 T
                    if (foundAsset is T typedAsset)
                    {
                        foundAssets.Add(typedAsset);
                    }
                    else
                    {
                        Debug.LogWarning($"[FindAssetsInCache] 找到了ID为 '{id}' 的资产，但其类型 '{foundAsset.GetType().Name}' 不是期望的 '{typeof(T).Name}'。");
                    }
                }
                else
                {
                    Debug.LogWarning($"[FindAssetsInCache] 在缓存中找不到ID为 '{id}' 的资产。");
                }
            }
        }

        return foundAssets;
    }
    
    /// <summary>
    /// 专门用于获取Sprite类型的值。
    /// </summary>
    /// <summary>
    /// 【已修正】专门用于获取Sprite类型的值。
    /// </summary>
    protected Sprite GetSpriteValue(DataRow row, Dictionary<string, ColumnInfo> headerMap, string fieldName)
    {
        if (headerMap.TryGetValue(fieldName, out ColumnInfo columnInfo))
        {
            if (columnInfo.Keyword == "path")
            {
                string basePath = columnInfo.KeywordData;
                string spriteName = ParseString(row[columnInfo.Index]);
                return LoadSprite(basePath, spriteName);
            }
        }
        return null;
    }
    
    protected string ParseString(object cellValue, string defaultValue = "")
    {
        if (cellValue == null || cellValue == DBNull.Value) return defaultValue;
        string valueStr = cellValue.ToString().Trim();
        return (valueStr == "*" || string.IsNullOrEmpty(valueStr)) ? defaultValue : valueStr;
    }
    
    protected int ParseInt(object cellValue, int defaultValue = 0)
    {
        string valueStr = ParseString(cellValue, defaultValue.ToString());
        if (int.TryParse(valueStr, out int result)) return result;
        return defaultValue;
    }
    protected float ParseFloat(object cellValue, float defaultValue = 0f)
    {
        string valueStr = ParseString(cellValue, defaultValue.ToString());
        if (float.TryParse(valueStr, out float result)) return result;
        return defaultValue;
    }
    
    protected bool ParseBool(object cellValue, bool defaultValue = false)
    {
        string valueStr = ParseString(cellValue, defaultValue.ToString()).Trim();

        if (bool.TryParse(valueStr, out bool result))
            return result;

        // 手动处理数字或特殊情况
        if (valueStr == "1") return true;
        if (valueStr == "0") return false;

        return defaultValue;
    } 
    
    protected T ParseEnum<T>(object cellValue, T defaultValue) where T : struct
    {
        string valueStr = ParseString(cellValue, defaultValue.ToString());
        if (Enum.TryParse<T>(valueStr, true, out T result)) return result;
        return defaultValue;
    }
    #endregion
    #endregion
}