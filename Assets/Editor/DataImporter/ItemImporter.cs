using UnityEngine;
using UnityEditor;
using System.Data;
using System.Collections.Generic;
using System.IO;
public class ItemImporter : BaseDataImporter
{
    private const string ItemsExcelPath = "Assets/Editor/Sheets/Items.xlsx";
    private const string ItemsOutputPath = "Assets/Resources/Data/Items";

    [MenuItem("游戏工具/从Excel导入物品数据")]
    public static void RunImport() { new ItemImporter().Import(); }

    protected override void Process()
    {
        TagImporter.CacheTags();
        Directory.CreateDirectory(ItemsOutputPath);
        ProcessSheet(ItemsExcelPath, "Items", ProcessItemRow);
    }
    private void ProcessSheet(string filePath, string sheetName, System.Action<DataRow, string> processRowAction)
    {
        DataTable table = ReadExcelSheet(filePath, sheetName);
        if (table == null) return;
        string baseIconPath = table.Rows[1][3].ToString().Trim();
        
        // 使用父类定义的 DataStartRow 规则
        for (int i = this.DataStartRow - 2; i < table.Rows.Count; i++)
        {
            DataRow row = table.Rows[i];
            if (row == null || row.IsNull(this.IdColumnName) || string.IsNullOrWhiteSpace(row[this.IdColumnName].ToString()))
            {
                break;
            }
            processRowAction(row, baseIconPath);
        }
    }
    private void ProcessItemRow(DataRow row, string baseIconPath)
    {
        int id = ParseInt(row["ID"]);
        string name = ParseString(row["Name"]);
        string assetPath = $"{ItemsOutputPath}/IT_{id}_{SanitizeFileName(name)}.asset";
        
        
        var asset = GetOrCreateAsset<ItemData>(assetPath);
        string oldJson = JsonUtility.ToJson(asset);

        asset.UniqueID = id;
        asset.name = name;
        asset.description = ParseString(row["description"]);
        asset.icon = LoadSprite(baseIconPath, ParseString(row["icon"]));
        string tagsString = ParseString(row["Tag"]);
        PopulateTags(asset, tagsString);
        asset.isConsumable = ParseBool(row["isConsumable"]);
        asset.isStackable = ParseBool(row["isStackable"]);
        asset.maxStackSize = ParseInt(row["maxStackSize"], 99);
        asset.spoilageTurns = ParseInt(row["spoilageTurns"]);

        if (oldJson != JsonUtility.ToJson(asset)) EditorUtility.SetDirty(asset);
    }
}