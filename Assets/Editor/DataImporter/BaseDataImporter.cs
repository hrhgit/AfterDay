// 引入Unity和C#的必要库
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Data;
using ExcelDataReader;
using System.Collections.Generic;
using System.Reflection;

// 【注意】: 此脚本依赖于一个静态的 ImporterCache.cs 文件。
// ImporterCache.cs 负责提供全局唯一的 UniversalCache<int, GameAsset>
// 用以在所有导入器之间共享最终生成的 ScriptableObject 资产。

/// <summary>
/// 存储已解析的Excel列表头信息。
/// </summary>
public class ColumnInfo
{
    // 新增：列的索引，用于更安全地从DataRow中按位置读取数据
    public int Index;
    // 字段英文名 (来自第1行)
    public string FieldName;
    // 关键字 (来自第3行, 会被转为小写以方便处理)
    public string Keyword;
    // 关键字数据 (来自第4行)
    public string KeywordData;
}

/// <summary>
/// 所有Excel导入器的通用父类。
/// 实现了基于“表头预处理”思想的、高度可扩展的导入逻辑。
/// </summary>
public abstract class BaseDataImporter
{
    // 成员变量，用于在单个导入器实例内部缓存引用表的ID->Name映射。
    // Key是引用表路径(如 "Tags.xlsx"), Value是该表的 "ID -> Name" 映射字典。
    private Dictionary<string, Dictionary<int, string>> _refCache;

    #region Configurable Rules
    // Excel表头定义
    protected virtual int HeaderRow => 1;
    protected virtual int DataStartRow => 5; // 数据从第5行开始
    protected virtual string IdColumnName => "ID";
    #endregion

    /// <summary>
    /// 公开的导入入口方法。
    /// </summary>
    public void Import()
    {
        Process();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"导入流程 '{this.GetType().Name}' 完成！");
    }

    /// <summary>
    /// 子类需要实现的具体处理逻辑。
    /// </summary>
    protected abstract void Process();

    #region Core Processing Logic

    /// <summary>
    /// 统一的Excel工作表处理流程。
    /// </summary>
    protected void ProcessSheet<T>(string filePath, string sheetName, string outputPath, string namePrefix) where T : GameAsset
    {
        DataTable table = ReadExcelSheet(filePath, sheetName);
        if (table == null) return;

        // 1. 【预处理阶段】解析表头，并根据 ref 关键字预缓存依赖项
        List<ColumnInfo> headerInfo = ParseHeader(table);

        // 2. 【数据处理阶段】循环处理数据行
        for (int i = DataStartRow - 2; i < table.Rows.Count; i++)
        {
            DataRow row = table.Rows[i];
            if (IsRowEmpty(row)) continue;

            int id = ParseInt(row[IdColumnName]);
            if (id == 0) continue; // ID为0或无效的行直接跳过

            string name = row.Table.Columns.Contains("pawnName") ? ParseString(row["pawnName"]) : ParseString(row["Name"]);
            
            string assetPath = $"{outputPath}/{namePrefix}_{id}_{SanitizeFileName(name)}.asset";
            var asset = GetOrCreateAsset<T>(assetPath);
            string oldJson = JsonUtility.ToJson(asset);

            // 3. 使用反射，根据预处理好的表头信息，填充对象数据
            PopulateObject(asset, row, headerInfo);

            // 4. 将填充好的资产注册到全局缓存，供其他导入器引用
            // (注意：PopulateObject内部已经设置了asset的UniqueID)
            ImporterCache.RegisterAsset(asset);

            if (oldJson != JsonUtility.ToJson(asset))
            {
                EditorUtility.SetDirty(asset);
            }
        }
        Debug.Log($"成功处理工作表: {sheetName}");
    }

    /// <summary>
    /// 【预处理器】解析表头，并对 "ref" 关键字执行预缓存操作。
    /// </summary>
    private List<ColumnInfo> ParseHeader(DataTable table)
    {
        // 初始化当前导入流程的引用缓存
        _refCache = new Dictionary<string, Dictionary<int, string>>();

        var infoList = new List<ColumnInfo>();
        if (table.Rows.Count < 4)
        {
            Debug.LogError("表头格式不正确，至少需要4行！");
            return infoList;
        }

        var row1_FieldName = table.Rows[0].ItemArray;
        var row3_Keyword = table.Rows[2].ItemArray;
        var row4_KeywordData = table.Rows[3].ItemArray;

        for (int i = 0; i < table.Columns.Count; i++)
        {
            var colInfo = new ColumnInfo
            {
                Index = i,
                FieldName = row1_FieldName[i].ToString().Trim(),
                Keyword = row3_Keyword[i].ToString().Trim().ToLower(), // 关键字转为小写，方便switch case
                KeywordData = row4_KeywordData[i].ToString().Trim()
            };

            // 【核心预处理逻辑】
            // 如果关键字是 ref 并且关键字数据（引用的表名）不为空
            if (colInfo.Keyword == "ref" && !string.IsNullOrWhiteSpace(colInfo.KeywordData))
            {
                // 立即执行预缓存，读取引用表，建立ID->Name的映射
                PreCacheReferenceTable(colInfo.KeywordData);
            }

            infoList.Add(colInfo);
        }
        return infoList;
    }

    /// <summary>
    /// 使用反射，根据表头信息自动填充一个对象。
    /// </summary>
    private void PopulateObject(GameAsset asset, DataRow row, List<ColumnInfo> headerInfo)
    {
        Type assetType = asset.GetType();

        foreach (var colInfo in headerInfo)
        {
            if (string.IsNullOrWhiteSpace(colInfo.FieldName)) continue;
            
            FieldInfo field = assetType.GetField(colInfo.FieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (field == null) continue;

            // 使用索引读取数据，比使用列名更安全
            object cellValue = row[colInfo.Index];

            switch (colInfo.Keyword)
            {
                case "path":
                    string spriteName = ParseString(cellValue);
                    field.SetValue(asset, LoadSprite(colInfo.KeywordData, spriteName));
                    break;

                case "ref":
                    HandleRefField(asset, field, cellValue, colInfo);
                    break;

                default: // 处理普通数据
                    if (cellValue != DBNull.Value && cellValue.ToString() != "*")
                    {
                        try
                        {
                            object convertedValue = Convert.ChangeType(cellValue, field.FieldType);
                            field.SetValue(asset, convertedValue);
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"转换字段 '{colInfo.FieldName}' 的值 '{cellValue}' 到 '{field.FieldType}' 时失败: {e.Message}");
                        }
                    }
                    break;
            }
        }
        asset.UniqueID = ParseInt(row[IdColumnName]); // 确保ID被设置
    }
    
    #endregion

    #region Helper Methods (通用工具箱)

    /// <summary>
    /// 预缓存一个引用表，读取其ID和Name列，存入_refCache。
    /// </summary>
    private void PreCacheReferenceTable(string refTablePath)
    {
        if (_refCache.ContainsKey(refTablePath)) return; // 避免重复缓存

        Debug.Log($"[预处理] 开始预缓存引用表: {refTablePath}");

        string[] parts = refTablePath.Split('/');
        string excelName = parts[0];
        string sheetName = (parts.Length > 1) ? parts[1] : null; // 默认取第一个sheet

        string fullPath = $"Assets/Editor/Sheets/{excelName}";

        // 使用静默模式读取，即使失败也不会在控制台打印Error，避免干扰
        DataTable table = ReadExcelSheet(fullPath, sheetName, true);
        if (table == null) return;
        
        var idNameMap = new Dictionary<int, string>();
        string nameColumn = table.Columns.Contains("pawnName") ? "pawnName" : "Name";
        if (!table.Columns.Contains(IdColumnName) || !table.Columns.Contains(nameColumn))
        {
            Debug.LogWarning($"[预处理] 引用表 '{refTablePath}' 缺少 'ID' 或 '{nameColumn}' 列，无法建立映射。");
            return;
        }

        for (int i = DataStartRow - 2; i < table.Rows.Count; i++)
        {
            DataRow row = table.Rows[i];
            if (IsRowEmpty(row)) continue;

            int id = ParseInt(row[IdColumnName]);
            string name = ParseString(row[nameColumn]);

            if (id != 0 && !idNameMap.ContainsKey(id))
            {
                idNameMap[id] = name;
            }
        }

        _refCache[refTablePath] = idNameMap;
        Debug.Log($"[预处理] 成功缓存 {idNameMap.Count} 条来自 '{refTablePath}' 的 ID->Name 映射。");
    }
    
    /// <summary>
    /// 统一处理 ref 关键字的字段填充逻辑
    /// </summary>
    private void HandleRefField(object asset, FieldInfo field, object cellValue, ColumnInfo colInfo)
    {
        // 1. 检查预缓存数据是否存在
        if (!_refCache.TryGetValue(colInfo.KeywordData, out var idNameMap))
        {
            Debug.LogWarning($"未能找到表 '{colInfo.KeywordData}' 的预缓存数据，跳过字段 '{colInfo.FieldName}'");
            return;
        }

        string[] idStrings = ParseString(cellValue).Split(';');

        // 2. 判断字段是 List<T> 还是单个 T
        if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
        {
            // --- 列表引用 List<T> ---
            var list = (System.Collections.IList)field.GetValue(asset);
            list.Clear();
            Type itemType = field.FieldType.GetGenericArguments()[0];

            foreach (var idStr in idStrings)
            {
                if (int.TryParse(idStr.Trim(), out int id) && id != 0)
                {
                    // a. 使用预缓存验证ID合法性
                    if (!idNameMap.ContainsKey(id))
                    {
                        Debug.LogWarning($"引用错误! 在表 '{colInfo.KeywordData}' 中找不到ID为 '{id}' 的记录。 (字段: {colInfo.FieldName})");
                        continue;
                    }

                    // b. 从全局缓存获取最终的资产对象
                    if (ImporterCache.UniversalCache.TryGetValue(id, out GameAsset refAsset) && itemType.IsAssignableFrom(refAsset.GetType()))
                    {
                        list.Add(refAsset);
                    }
                }
            }
        }
        else
        {
            // --- 单个引用 T ---
            if (idStrings.Length > 0 && int.TryParse(idStrings[0].Trim(), out int id) && id != 0)
            {
                if (!idNameMap.ContainsKey(id))
                {
                    Debug.LogWarning($"引用错误! 在表 '{colInfo.KeywordData}' 中找不到ID为 '{id}' 的记录。 (字段: {colInfo.FieldName})");
                    return;
                }

                if (ImporterCache.UniversalCache.TryGetValue(id, out GameAsset refAsset) && field.FieldType.IsAssignableFrom(refAsset.GetType()))
                {
                    field.SetValue(asset, refAsset);
                }
            }
        }
    }

    /// <summary>
    /// 检查一个DataRow是否完全为空。
    /// </summary>
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

    /// <summary>
    /// 读取指定的Excel文件和工作表，返回一个DataTable。
    /// </summary>
    /// <param name="isSilent">静默模式，失败时只返回null，不打印LogError</param>
    protected DataTable ReadExcelSheet(string filePath, string sheetName, bool isSilent = false)
    {
        if (!File.Exists(filePath))
        {
            if (!isSilent) Debug.LogError($"Excel文件未找到: {filePath}");
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

                    if (string.IsNullOrEmpty(sheetName))
                    {
                        table = tables[0]; // 如果没指定sheet名，默认取第一个
                    }
                    else if (tables.Contains(sheetName))
                    {
                        table = tables[sheetName];
                    }

                    if (table == null)
                    {
                        if (!isSilent) Debug.LogError($"在 {filePath} 中未找到工作表 '{sheetName ?? "默认第一个"}'.");
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
            if (!isSilent) Debug.LogError($"读取Excel文件 {filePath} 时出错: {e.Message}");
            return null;
        }
    }
    
    protected static Sprite LoadSprite(string basePath, string iconName)
    {
        if (string.IsNullOrWhiteSpace(iconName)) return null;
        
        string normalizedBasePath = basePath.Replace('\\', '/').TrimStart('/');
        string fullPath = $"Assets/Resources/{normalizedBasePath}/{iconName}.png";
        
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);

        if (sprite == null)
        {
            Debug.LogWarning($"加载失败! 未能在路径 '{fullPath}' 找到Sprite资源。");
        }
        return sprite;
    }
    
    protected T GetOrCreateAsset<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            // 确保目录存在
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

    // --- Primitive Type Parsers ---
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