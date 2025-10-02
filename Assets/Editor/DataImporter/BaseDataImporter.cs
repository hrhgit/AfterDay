using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Data;
using ExcelDataReader;
using System.Collections.Generic;
using System.Linq;
using System.Reflection; // 引入反射库

/// <summary>
/// 存储已解析的Excel列表头信息。
/// </summary>
public class ColumnInfo
{
    public string FieldName;    // 字段英文名 (来自第1行)
    public string Keyword;      // 关键字 (来自第3行)
    public string KeywordData;  // 关键字数据 (来自第4行)
}
/// <summary>
/// 所有Excel导入器的通用父类。
/// </summary>
public abstract class BaseDataImporter
{
    // 运行时缓存，用于跨表引用
    protected static Dictionary<int, TagData> TagCache;

    #region Configurable Rules
    protected virtual int HeaderRow => 1;
    protected virtual int DataStartRow => 5; // 数据现在从第5行开始
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

    #region Core Processing Logic

    /// <summary>
    /// 统一的Excel表处理流程。
    /// </summary>
    protected void ProcessSheet<T>(string filePath, string sheetName, string outputPath, string namePrefix) where T : CardData
    {
        DataTable table = ReadExcelSheet(filePath, sheetName);
        if (table == null) return;

        // 1. 解析4行表头
        List<ColumnInfo> headerInfo = ParseHeader(table);

        // 2. 循环处理数据行
        for (int i = DataStartRow - 2; i < table.Rows.Count; i++)
        {
            DataRow row = table.Rows[i];
            if (IsRowEmpty(row)) break;

            int id = ParseInt(row[IdColumnName]);
            string name = row.Table.Columns.Contains("pawnName") ? ParseString(row["pawnName"]) : ParseString(row["Name"]);
            
            string assetPath = $"{outputPath}/{namePrefix}_{id}_{SanitizeFileName(name)}.asset";
            var asset = GetOrCreateAsset<T>(assetPath);
            string oldJson = JsonUtility.ToJson(asset);

            // 3. 使用反射，根据表头信息自动填充数据
            PopulateObject(asset, row, headerInfo);

            if (oldJson != JsonUtility.ToJson(asset))
            {
                EditorUtility.SetDirty(asset);
            }
        }
        Debug.Log($"成功处理工作表: {sheetName}");
    }

    /// <summary>
    /// 解析4行表头，返回一个包含列信息的列表。
    /// </summary>
    private List<ColumnInfo> ParseHeader(DataTable table)
    {
        var infoList = new List<ColumnInfo>();
        if (table.Rows.Count < 4) return infoList;

        var row1_FieldName = table.Rows[0].ItemArray;
        var row3_Keyword = table.Rows[2].ItemArray;
        var row4_KeywordData = table.Rows[3].ItemArray;

        for (int i = 0; i < table.Columns.Count; i++)
        {
            infoList.Add(new ColumnInfo
            {
                FieldName = row1_FieldName[i].ToString().Trim(),
                Keyword = row3_Keyword[i].ToString().Trim(),
                KeywordData = row4_KeywordData[i].ToString().Trim()
            });
        }
        return infoList;
    }

    /// <summary>
    /// 使用反射，根据表头信息自动填充一个CardData对象。
    /// </summary>
    private void PopulateObject(CardData asset, DataRow row, List<ColumnInfo> headerInfo)
    {
        Type assetType = asset.GetType();

        foreach (var colInfo in headerInfo)
        {
            if (string.IsNullOrWhiteSpace(colInfo.FieldName)) continue;
            
            // 使用反射查找与列名匹配的字段
            FieldInfo field = assetType.GetField(colInfo.FieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (field == null) continue;

            object cellValue = row[colInfo.FieldName];

            // --- 处理特殊关键字 ---
            if (colInfo.Keyword == "path")
            {
                field.SetValue(asset, LoadSprite(colInfo.KeywordData, ParseString(cellValue)));
            }
            else if (colInfo.Keyword == "ref" && colInfo.FieldName == "Tags")
            {
                var tagsList = (List<TagData>)field.GetValue(asset);
                tagsList.Clear();
                string[] tagIds = ParseString(cellValue).Split(';');
                foreach (var tagIdStr in tagIds)
                {
                    if (int.TryParse(tagIdStr, out int tagId) && TagCache.ContainsKey(tagId))
                    {
                        tagsList.Add(TagCache[tagId]);
                    }
                }
            }
            else // --- 处理普通数据 ---
            {
                if (cellValue != DBNull.Value && cellValue.ToString() != "*")
                {
                    try
                    {
                        // 尝试将单元格的值转换为字段需要的类型
                        object convertedValue = Convert.ChangeType(cellValue, field.FieldType);
                        field.SetValue(asset, convertedValue);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"转换字段 '{colInfo.FieldName}' 的值 '{cellValue}' 时失败: {e.Message}");
                    }
                }
            }
        }
        asset.UniqueID = ParseInt(row["ID"]); // 确保ID被设置
    }
    
    #endregion

    #region Helper Methods (通用工具箱)
   
    /// <summary>
    /// 检查一个DataRow是否完全为空。
    /// </summary>
    protected bool IsRowEmpty(DataRow row)
    {
        if (row == null) return true;
        foreach (var value in row.ItemArray)
        {
            if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
            {
                return false; // 只要找到一个非空单元格，行就不为空
            }
        }
        return true; // 所有单元格都为空
    }
    /// <summary>
    /// 读取指定的Excel文件和工作表，返回一个DataTable。
    /// (已更新：会自动清理表头列名的前后空格)
    /// </summary>
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
                    
                    if (!result.Tables.Contains(sheetName))
                    {
                        Debug.LogError($"在 {filePath} 中未找到名为 '{sheetName}' 的工作表。");
                        return null;
                    }

                    DataTable table = result.Tables[sheetName];
                    
                    // 遍历DataTable中的每一列
                    foreach (DataColumn column in table.Columns)
                    {
                        // 使用 Trim() 方法移除列名前后的所有空白字符
                        column.ColumnName = column.ColumnName.Trim();
                    }
                    return table;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"读取Excel文件 {filePath} (工作表: {sheetName}) 时出错: {e.Message}");
            return null;
        }
    }

    
    
    protected static Sprite LoadSprite(string basePath, string iconName)
    {
        if (string.IsNullOrWhiteSpace(iconName)) return null;

        // --- 核心修改：路径规范化 ---
        // 1. 将所有反斜杠 \ 替换为正斜杠 /
        string normalizedBasePath = basePath.Replace('\\', '/');
        
        // 2. 移除路径开头可能存在的斜杠，确保路径是相对的
        normalizedBasePath = normalizedBasePath.TrimStart('/');
        
        string fullPath = $"Assets/Resources/{normalizedBasePath}/{iconName}.png";

        // 打印出最终尝试加载的路径，便于调试
        // Debug.Log($"正在尝试从路径加载Sprite: {fullPath}");
        
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);

        if (sprite == null)
        {
            Debug.LogWarning($"加载失败! 未能在路径 '{fullPath}' 找到Sprite资源。请检查路径和图片导入设置。");
        }
        
        return sprite;
    }
    
    protected void PopulateTags(CardData asset, string tagsString)
    {
        asset.tags.Clear(); // 清空旧标签
        if (string.IsNullOrEmpty(tagsString) || TagCache == null) return;

        // 按分号分割ID字符串
        string[] tagIds = tagsString.Split(';');
        foreach (var tagIdStr in tagIds)
        {
            if (int.TryParse(tagIdStr.Trim(), out int tagId) && TagCache.TryGetValue(tagId, out TagData tagAsset))
            {
                asset.tags.Add(tagAsset);
            }
            else if (!string.IsNullOrWhiteSpace(tagIdStr))
            {
                Debug.LogWarning($"在处理卡牌 '{asset.name}' (ID: {asset.UniqueID}) 时，未找到ID为 '{tagIdStr.Trim()}' 的标签。");
            }
        }
    }
    
    protected T GetOrCreateAsset<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
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

    protected string ParseString(object cellValue, string defaultValue = "")
    {
        if (cellValue == null) return defaultValue;
        string valueStr = cellValue.ToString().Trim();
        return (valueStr == "*" || string.IsNullOrEmpty(valueStr)) ? defaultValue : valueStr;
    }
    
    protected int ParseInt(object cellValue, int defaultValue = 0)
    {
        string valueStr = ParseString(cellValue?.ToString(), defaultValue.ToString());
        if (int.TryParse(valueStr, out int result)) return result;
        return defaultValue;
    }
    
    protected bool ParseBool(object cellValue, bool defaultValue = false)
    {
        string valueStr = ParseString(cellValue?.ToString(), defaultValue.ToString());
        if (bool.TryParse(valueStr, out bool result)) return result;
        return defaultValue;
    }
    
    protected T ParseEnum<T>(object cellValue, T defaultValue) where T : struct
    {
        string valueStr = ParseString(cellValue?.ToString(), defaultValue.ToString());
        if (Enum.TryParse<T>(valueStr, true, out T result)) return result;
        return defaultValue;
    }
    #endregion
}
