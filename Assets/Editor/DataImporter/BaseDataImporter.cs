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

public enum ColumnProcessingType { Normal, Path, Ref }

public class ProcessedColumn
{
    public ColumnInfo OriginalInfo;
    public ColumnProcessingType Type;
    public object ProcessedData;
}
#endregion

public abstract class BaseDataImporter
{
    private Dictionary<string, Dictionary<int, string>> _preCachedRefTables;
    #region Configurable Rules
    protected virtual int DataStartRow => 5;
    protected virtual string IdColumnName => "ID";
    #endregion

    
    public void Import()
    {
        _preCachedRefTables = new Dictionary<string, Dictionary<int, string>>();

        Process();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"导入流程 '{this.GetType().Name}' 完成！");
    }

    protected abstract void Process();


    #region Header Pre-Processing
    // --- 表头预处理核心功能区 ---

    /// <summary>
    /// 【入口】表头“深加工”处理器。
    /// 它不仅解析表头，还会根据关键字执行预处理工作（例如为ref规则预缓存数据）。
    /// </summary>
    protected List<ProcessedColumn> ProcessHeader(DataTable table)
    {
        var processedList = new List<ProcessedColumn>();
        List<ColumnInfo> originalHeader = ParseHeader(table);

        foreach (var colInfo in originalHeader)
        {
            var processedColumn = new ProcessedColumn { OriginalInfo = colInfo };
            switch (colInfo.Keyword)
            {
                case "ref":
                    processedColumn.Type = ColumnProcessingType.Ref;
                    processedColumn.ProcessedData = PreCacheReferenceTable(colInfo.KeywordData);
                    break;
                case "path":
                    processedColumn.Type = ColumnProcessingType.Path;
                    processedColumn.ProcessedData = colInfo.KeywordData;
                    break;
                default:
                    processedColumn.Type = ColumnProcessingType.Normal;
                    processedColumn.ProcessedData = null;
                    break;
            }
            processedList.Add(processedColumn);
        }
        return processedList;
    }

    /// <summary>
    /// 【步骤1】基础表头解析器。
    /// 读取DataTable的头几行，返回一个标准化的 ColumnInfo 列表。
    /// </summary>
    private List<ColumnInfo> ParseHeader(DataTable table)
    {
        var infoList = new List<ColumnInfo>();
        if (table.Rows.Count < 4)
        {
            Debug.LogError("[BaseDataImporter] 表头格式不正确，至少需要4行来进行预处理！");
            return infoList;
        }
        
        var row3_Keyword = table.Rows[1].ItemArray;
        var row4_KeywordData = table.Rows[2].ItemArray;
        
        for (int i = 0; i < table.Columns.Count; i++)
        {
            DataColumn column = table.Columns[i];
            string fieldName = column.ColumnName; // 直接从列对象获取名称

            if (string.IsNullOrEmpty(fieldName)) continue;

            infoList.Add(new ColumnInfo
            {
                Index = i,
                FieldName = fieldName, // 来自Excel第1行
                Keyword = row3_Keyword[i]?.ToString().Trim().ToLower(), // 来自Excel第3行
                KeywordData = row4_KeywordData[i]?.ToString().Trim() // 来自Excel第4行
            });
        }
        return infoList;
    }

    /// <summary>
    /// 【步骤2】引用预缓存器。
    /// 读取指定的Excel工作表，提取其ID和Name/Tag列，并以字典形式返回。
    /// </summary>
    private Dictionary<int, string> PreCacheReferenceTable(string refTablePath)
    {
        if (_preCachedRefTables.ContainsKey(refTablePath))
        {
            return _preCachedRefTables[refTablePath];
        }

        Debug.Log($"[预处理] 开始预缓存引用表: {refTablePath}");

        string[] parts = refTablePath.Split('/');
        string excelName = parts[0];
        string sheetName = (parts.Length > 1) ? parts[1] : null;
        string fullPath = $"Assets/Editor/Sheets/{excelName}.xlsx";

        DataTable table = ReadExcelSheet(fullPath, sheetName);
        if (table == null)
        {
            _preCachedRefTables[refTablePath] = new Dictionary<int, string>();
            return _preCachedRefTables[refTablePath];
        }
        
        string nameColumn = "Name";
        if (table.Columns.Contains("name")) nameColumn = "name";
        else if (table.Columns.Contains("Tag")) nameColumn = "Tag";
        
        string idColumn = "ID";
        if (!table.Columns.Contains(idColumn) || !table.Columns.Contains(nameColumn))
        {
            Debug.LogWarning($"[预处理] 引用表 '{refTablePath}' 缺少 '{idColumn}' 或 '{nameColumn}' 列，无法建立映射。");
            _preCachedRefTables[refTablePath] = new Dictionary<int, string>();
            return _preCachedRefTables[refTablePath];
        }
        
        var idNameMap = new Dictionary<int, string>();
        int dataStartRow = 5;
        for (int i = dataStartRow - 2; i < table.Rows.Count; i++)
        {
            DataRow row = table.Rows[i];
            if (IsRowEmpty(row)) continue;

            int id = ParseInt(row[idColumn]);
            string name = ParseString(row[nameColumn]);

            if (id != 0 && !idNameMap.ContainsKey(id))
            {
                idNameMap[id] = name;
            }
        }

        _preCachedRefTables[refTablePath] = idNameMap;
        Debug.Log($"[预处理] 成功缓存 {idNameMap.Count} 条来自 '{refTablePath}' 的 ID->Name 映射。");
        return idNameMap;
    }
    #endregion
    
    
    
    #region Helper Methods (通用工具箱 - 保持不变)
    
    
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
    protected Dictionary<int, T> LoadAllAssets<T>() where T : GameAsset
    {
        var assetCache = new Dictionary<int, T>();
        
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

    
    /// <summary>
    /// 根据字段名，从DataRow中安全地获取并解析值。
    /// </summary>
    protected T GetValue<T>(DataRow row, Dictionary<string, ProcessedColumn> headerMap, string fieldName)
    {
        
        if (headerMap.TryGetValue(fieldName, out ProcessedColumn columnInfo))
        {
            
            object cellValue = row[columnInfo.OriginalInfo.Index];
            
            if (typeof(T) == typeof(int)) return (T)(object)ParseInt(cellValue);
            if (typeof(T) == typeof(string)) return (T)(object)ParseString(cellValue);
            if (typeof(T) == typeof(bool)) return (T)(object)ParseBool(cellValue);
            if (typeof(T) == typeof(float)) return (T)(object)ParseFloat(cellValue); 
        }
        return default(T); // 如果找不到列，返回默认值
    }

    protected void PopulateTags(CardData asset, string tagsString, Dictionary<int, TagData> tagCache)
    {
        asset.tags.Clear();
        if (string.IsNullOrEmpty(tagsString)) return;

        string[] tagIds = tagsString.Split(';');
        foreach (var tagIdStr in tagIds)
        {
            
            if (int.TryParse(tagIdStr.Trim(), out int tagId))
            {
                // 从传入的 tagCache 字典中查找
                if (tagCache.TryGetValue(tagId, out TagData tagAsset))
                {
                    asset.tags.Add(tagAsset);
                }
                else
                {
                    Debug.LogWarning($"处理卡牌 '{asset.name}' 时，未在已加载的Tag资产中找到ID为 '{tagId}' 的标签。");
                }
            }
        }
    }
    
    /// <summary>
    /// 专门用于获取Sprite类型的值。
    /// </summary>
    protected Sprite GetSpriteValue(DataRow row, Dictionary<string, ProcessedColumn> headerMap, string fieldName)
    {
        if (headerMap.TryGetValue(fieldName, out ProcessedColumn columnInfo))
        {
            if (columnInfo.Type == ColumnProcessingType.Path)
            {
                // 这就是您要的逻辑：从预处理结果中获取 base path
                string basePath = (string)columnInfo.ProcessedData;
                string spriteName = ParseString(row[columnInfo.OriginalInfo.Index]);
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
        string valueStr = ParseString(cellValue, defaultValue.ToString());
        if (bool.TryParse(valueStr, out bool result)) return result;
        return defaultValue;
    }
    
    protected T ParseEnum<T>(object cellValue, T defaultValue) where T : struct
    {
        string valueStr = ParseString(cellValue, defaultValue.ToString());
        if (Enum.TryParse<T>(valueStr, true, out T result)) return result;
        return defaultValue;
    }
    #endregion
}