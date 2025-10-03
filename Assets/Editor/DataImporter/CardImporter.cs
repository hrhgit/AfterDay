using UnityEditor;
using System.Data;
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // 需要引用Linq来使用 .ToDictionary()

/// <summary>
/// 最终版的卡牌导入器。
/// 它完全利用父类 BaseDataImporter 提供的表头预处理功能，
/// 动态地、健壮地填充所有字段。
/// </summary>
public class CardImporter : BaseDataImporter
{
    private const string CardsExcelPath = "Assets/Editor/Sheets/Cards.xlsx";
    private const string PawnsOutputPath = "Assets/Resources/Data/Pawns";

    [MenuItem("游戏工具/从Excel导入卡牌数据")]
    public static void RunImport()
    {
        new CardImporter().Import();
    }

    /// <summary>
    /// 实现父类要求的主处理方法。
    /// </summary>
    protected override void Process()
    {
        Debug.Log("--- 开始导入卡牌数据 ---");

        // 分别处理 Robot 和 Human 工作表
        ProcessPawnSheet<RobotPawnData>("Robot");
        ProcessPawnSheet<HumanPawnData>("Human");
    }

    /// <summary>
    /// 一个通用的、处理单个Pawn工作表的辅助方法，避免代码重复。
    /// </summary>
    /// <typeparam name="T">要创建的具体Pawn类型 (RobotPawnData 或 HumanPawnData)</typeparam>
    /// <param name="sheetName">要处理的工作表名称</param>
    private void ProcessPawnSheet<T>(string sheetName) where T : CardData
    {
        DataTable table = ReadExcelSheet(CardsExcelPath, sheetName);
        if (table == null) return;

        // 1. 【核心】调用父类的预处理方法，获取包含所有加工后信息的列表
        List<ProcessedColumn> header = ProcessHeader(table);

        // 2. 为了方便、高效地查询，将列表转换为字典
        var headerMap = header.ToDictionary(info => info.OriginalInfo.FieldName, info => info);

        // 3. 循环处理数据行
        for (int i = 3; i < table.Rows.Count; i++)
        {
            DataRow row = table.Rows[i];
            if (IsRowEmpty(row)) continue;

            int id = GetValue<int>(row, headerMap, "ID");
            if (id == 0) continue;

            string name = GetValue<string>(row, headerMap, "pawnName");
            string assetPath = $"{PawnsOutputPath}/Pawn_{id}_{SanitizeFileName(name)}.asset";

            var asset = GetOrCreateAsset<T>(assetPath);
            string oldJson = JsonUtility.ToJson(asset);

            // --- 4. 开始动态填充所有字段 ---

            // 填充 GameAsset (基类) 字段
            asset.UniqueID = id;
            asset.description = GetValue<string>(row, headerMap, "description");

            // 【关键实现】从预处理好的 headerMap 中动态获取 iconBasePath
            asset.icon = GetSpriteValue(row, headerMap, "icon");

            // 填充 CardData (父类) 字段
            asset.name = name;
            PopulateTags(asset, GetValue<string>(row, headerMap, "Tags"));

            // 填充具体子类的特有字段
            if (asset is RobotPawnData robotData)
            {
                robotData.movement = GetValue<int>(row, headerMap, "movement");
                robotData.calculation = GetValue<int>(row, headerMap, "calculation");
                robotData.search = GetValue<int>(row, headerMap, "search");
                robotData.art = GetValue<int>(row, headerMap, "art");
            }
            else if (asset is HumanPawnData humanData)
            {
                // 根据您最新的描述填充Human特有字段
                humanData.initialHealth = GetValue<int>(row, headerMap, "Health");
                humanData.initialMorale = GetValue<int>(row, headerMap, "Morale");
                humanData.hungerGainPerTurn = GetValue<float>(row, headerMap, "Hunger");
            }

            if (oldJson != JsonUtility.ToJson(asset))
            {
                EditorUtility.SetDirty(asset);
            }
        }

        Debug.Log($"成功处理 '{sheetName}' 工作表。");
    }
}
    
    