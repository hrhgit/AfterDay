using UnityEditor;
using System.Data;
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // 需要引用Linq来使用 .ToDictionary()

/// <summary>
/// 具体的物品导入器。
/// 它完全利用父类 BaseDataImporter 提供的表头预处理功能，动态填充数据。
/// </summary>
public class ItemImporter : BaseDataImporter
{
    // --- 配置常量 ---
    private const string ItemsExcelPath = "Assets/Editor/Sheets/Items.xlsx";
    private const string ItemsOutputPath = "Assets/Resources/Data/Items";
    private const string ItemPrefix = "Item";

    /// <summary>
    /// Unity菜单入口
    /// </summary>
    [MenuItem("游戏工具/从Excel导入物品数据")]
    public static void RunImport()
    {
        new ItemImporter().Import();
    }

    
    protected override void Process()
    {
        Debug.Log("--- 开始导入物品数据 ---");
        ProcessItemSheet("Items");
    }

    /// <summary>
    /// 处理单个物品工作表的核心逻辑。
    /// </summary>
    /// <param name="sheetName">要处理的工作表名称</param>
    private void ProcessItemSheet(string sheetName)
    {
        DataTable table = ReadExcelSheet(ItemsExcelPath, sheetName);
        if (table == null) return;

        // 1. 【核心】调用父类的预处理方法，获取所有加工后的列信息
        List<ProcessedColumn> header = ProcessHeader(table);
        var headerMap = header.ToDictionary(info => info.OriginalInfo.FieldName, info => info);

        // 2. 循环处理数据行
        for (int i = 3; i < table.Rows.Count; i++) // 假设数据从第5行开始
        {
            DataRow row = table.Rows[i];
            if (IsRowEmpty(row)) continue;

            int id = GetValue<int>(row, headerMap, "ID");
            if (id == 0) continue;

            // 注意：我们假设物品的名称列在Excel中为 "Name"
            string name = GetValue<string>(row, headerMap, "Name");
            string assetPath = $"{ItemsOutputPath}/{ItemPrefix}_{id}_{SanitizeFileName(name)}.asset";

            // 我们假设物品也使用 CardData 作为统一数据容器
            var asset = GetOrCreateAsset<ItemData>(assetPath);
            string oldJson = JsonUtility.ToJson(asset);

            // --- 3. 开始动态填充所有字段 ---
            
            // 填充 GameAsset (基类) 字段
            asset.UniqueID = id;
            asset.description = GetValue<string>(row, headerMap, "description");
            asset.icon = GetSpriteValue(row, headerMap, "icon");

            // 填充 CardData (父类) 字段
            // 注意：我们将Excel中的 "Name" 列映射到 CardData 的 "pawnName" 字段
            asset.name = name;
            PopulateTags(asset, GetValue<string>(row, headerMap, "Tag"));

            // 填充物品特有的字段 (我们假设这些字段存在于 CardData 中)
            // 根据 Items.xlsx - Items.csv 文件中的列进行填充
            asset.isConsumable = GetValue<bool>(row, headerMap, "isConsumable");
            asset.isStackable = GetValue<bool>(row, headerMap, "isStackable");
            asset.maxStackSize = GetValue<int>(row, headerMap, "maxStackSize");
            asset.spoilageTurns = GetValue<int>(row, headerMap, "spoilageTurns");

            if (oldJson != JsonUtility.ToJson(asset))
            {
                EditorUtility.SetDirty(asset);
            }
        }
        Debug.Log($"成功处理 '{sheetName}' 工作表。");
    }
    
}