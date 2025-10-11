using System;
using UnityEditor;
using System.Data;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq; // 需要引用Linq来使用 .ToDictionary()

/// <summary>
/// 具体的事件导入器。
/// 模仿 ItemImporter 的结构，利用父类 BaseDataImporter 提供的表头预处理功能。
/// </summary>
public class EventImporter : BaseDataImporter
{
    // --- 配置常量 ---
    private const string EventsExcelPath = "Assets/Editor/Sheets/Events.xlsx";
    private const string EventsOutputPath = "Assets/Resources/Data/Events";
    private const string EventPrefix = "EV";

    /// <summary>
    /// Unity菜单入口
    /// </summary>
    [MenuItem("游戏工具/从Excel导入事件数据")]
    public static void RunImport()
    {
        new EventImporter().Import();
    }
    
    /// <summary>
    /// 主处理方法，负责准备所有需要的缓存。
    /// </summary>
    protected override void Process()
    {
        Debug.Log("--- 开始导入事件数据 ---");
        ProcessEventSheet("Events");
    }

    /// <summary>
    /// 处理单个事件工作表的核心逻辑。
    /// </summary>
    /// <param name="sheetName">要处理的工作表名称</param>
    /// <param name="ruleCache">预加载好的ValidationRule资产缓存</param>
    private void ProcessEventSheet(string sheetName)
    {
        DataTable table = ReadExcelSheet(EventsExcelPath, sheetName);
        if (table == null) return;
        //缓存
        var ruleCache = LoadAllAssets<ValidationRule>();
        var cardCache = LoadAllAssets<CardData>();
        
        // 1. 调用父类的预处理方法，获取所有加工后的列信息
        List<ColumnInfo> header = ParseHeader(table);
        var headerMap = header.ToDictionary(info => info.FieldName, info => info, System.StringComparer.OrdinalIgnoreCase);
        // 2. 循环处理数据行
        for (int i = 3; i < table.Rows.Count; i++) // 数据从Excel第5行开始
        {
            DataRow row = table.Rows[i];
            if (IsRowEmpty(row)) continue;

            int id = GetValue<int>(row, headerMap, "ID");
            Debug.Log(id);
            if (id == 0) continue;
            
            // 1. 【核心修改】从Excel读取事件类型的字符串
            string eventTypeString = GetValue<string>(row, headerMap, "eventType");
            if (string.IsNullOrEmpty(eventTypeString))
            {
                Debug.LogWarning($"第 {i+2} 行的 eventType 为空，已跳过。");
                continue;
            }

            // 2. 【核心修改】根据字符串动态创建或加载资产
            string name = GetValue<string>(row, headerMap, "name");
            string assetPath = $"{EventsOutputPath}/{EventPrefix}_{id}_{SanitizeFileName(name)}.asset";
            
            // 调用新的动态创建方法，替换 GetOrCreateAsset<T>()
            EventData asset = GetOrCreateEventAsset(assetPath, eventTypeString);
            
            string oldJson = JsonUtility.ToJson(asset);

            // --- 3. 开始动态填充所有字段 ---
            
            // 填充 GameAsset (基类) 字段
            asset.UniqueID = id;

            // 填充 EventData (父类) 字段
            asset.name = name;
            asset.title = GetValue<string>(row, headerMap, "title");
            asset.description = GetValue<string>(row, headerMap, "description");
            
            // 使用通用的 FindAssetsInCache 方法来填充引用列表
            asset.mandatoryValidations = FindAssetsInCache<ValidationRule>(GetValue<string>(row, headerMap, "mandatoryValidations"), ruleCache);
            asset.optionalValidations = FindAssetsInCache<ValidationRule>(GetValue<string>(row, headerMap, "optionalValidations"), ruleCache);

            // 填充固定奖励 
            asset.fixedReward = ParseCardRewards(GetValue<string>(row, headerMap, "fixedReward"),cardCache);

            if (oldJson != JsonUtility.ToJson(asset))
            {
                EditorUtility.SetDirty(asset);
            }
            
            Debug.Log($"成功生成/更新事件: ID={id}, Name='{name}'");
        }
        Debug.Log($"成功处理 '{sheetName}' 工作表。");
    }
    /// <summary>
    /// 【新增辅助方法】根据字符串类型动态创建或加载 EventData 资产。
    /// </summary>
    /// <summary>
    /// 【已修正】根据字符串类型动态创建或加载 EventData 资产。
    /// 修正了 Type.GetType 无法跨程序集查找类型的问题。
    /// </summary>
    private EventData GetOrCreateEventAsset(string path, string typeName)
    {
        var asset = AssetDatabase.LoadAssetAtPath<EventData>(path);

        // 【核心修改】使用新的、更可靠的方式来查找类型
        Type assetType = FindTypeByName(typeName);

        if (assetType == null || !typeof(EventData).IsAssignableFrom(assetType))
        {
            Debug.LogError($"类型名 '{typeName}' 无效或不继承自 EventData。请检查Excel中的拼写，以及项目中是否存在该C#脚本。");
            return null;
        }

        if (asset != null)
        {
            // 如果资产已存在，检查类型是否匹配
            if (asset.GetType() != assetType)
            {
                Debug.LogError($"资产类型冲突！路径 '{path}' 已存在一个 '{asset.GetType().Name}' 类型的资产，但Excel中指定的新类型是 '{typeName}'。请手动删除旧资产后重试。已跳过此行。");
                return null;
            }
        }
        else // 如果 asset == null，说明需要创建新资产
        {
            asset = (EventData)ScriptableObject.CreateInstance(assetType);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"成功创建新资产: [{typeName}] 于路径 '{path}'");
        }

        return asset;
    }

    /// <summary>
    /// 【新增的辅助方法】遍历所有已加载的程序集来根据名称查找一个Type。
    /// </summary>
    /// <param name="typeName">要查找的类型的完整名称。</param>
    /// <returns>找到的Type对象，如果找不到则返回null。</returns>
    private Type FindTypeByName(string typeName)
    {
        // 遍历当前应用程序域中的所有程序集
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            // 在每个程序集中查找该类型
            var type = assembly.GetType(typeName);
            if (type != null)
            {
                // 找到了就立刻返回
                return type;
            }
        }
        // 遍历完所有程序集都没找到，返回null
        return null;
    }
}