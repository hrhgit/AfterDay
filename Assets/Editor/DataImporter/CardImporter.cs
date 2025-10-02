using UnityEngine;
using UnityEditor;
using System.Data;
using System.Collections.Generic;
using System.IO;

public class CardImporter : BaseDataImporter
{
    private const string CardsExcelPath = "Assets/Editor/Sheets/Cards.xlsx";
    private const string PawnsOutputPath = "Assets/Resources/Data/Pawns";
    
    [MenuItem("游戏工具/从Excel导入卡牌数据")]
    public static void RunImport()
    {
        // 创建实例并调用父类的入口方法
        new CardImporter().Import();
    }
    
    // 实现父类要求的核心处理逻辑
    protected override void Process()
    {
        TagImporter.CacheTags();
        Directory.CreateDirectory(PawnsOutputPath);
        
        // 分别处理不同的工作表
        ProcessSheet(CardsExcelPath, "Robot", ProcessRobotRow);
        ProcessSheet(CardsExcelPath, "Human", ProcessHumanRow);
    }
    
    // 从父类继承来的 ProcessSheet 方法的简化版
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
    private void ProcessRobotRow(DataRow row, string baseIconPath)
    {
        int id = ParseInt(row["ID"]);
        string name = ParseString(row["pawnName"]);
        string assetPath = $"{PawnsOutputPath}/RB_{id}_{SanitizeFileName(name)}.asset";
        
        var asset = GetOrCreateAsset<RobotPawnData>(assetPath);
        string oldJson = JsonUtility.ToJson(asset);

        asset.UniqueID = id;
        asset.name = name;
        asset.description = ParseString(row["description"]);
        asset.icon = LoadSprite(baseIconPath, ParseString(row["icon"]));
        string tagsString = ParseString(row["Tag"]);
        PopulateTags(asset, tagsString);
        asset.movement = ParseInt(row["movement"]);
        asset.calculation = ParseInt(row["calculation"]);
        asset.search = ParseInt(row["search"]);
        asset.art = ParseInt(row["art"]);
        
        if (oldJson != JsonUtility.ToJson(asset)) EditorUtility.SetDirty(asset);
    }
    
    private void ProcessHumanRow(DataRow row, string baseIconPath)
    {
        int id = ParseInt(row["ID"]);
        string name = ParseString(row["pawnName"]);
        string assetPath = $"{PawnsOutputPath}/HM_{id}_{SanitizeFileName(name)}.asset";
        
        var asset = GetOrCreateAsset<HumanPawnData>(assetPath);
        string oldJson = JsonUtility.ToJson(asset);

        asset.UniqueID = id;
        asset.name = name;
        asset.description = ParseString(row["description"]);
        string tagsString = ParseString(row["Tag"]);
        PopulateTags(asset, tagsString);
        asset.initialHealth = ParseInt(row["Health"]);
        asset.initialMorale = ParseInt(row["Morale"]);
        
        if (oldJson != JsonUtility.ToJson(asset)) EditorUtility.SetDirty(asset);
    }
    
}
