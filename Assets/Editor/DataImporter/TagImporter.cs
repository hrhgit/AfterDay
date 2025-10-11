using UnityEditor;
using System.Data;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 标签数据导入器。
/// 采用“两遍循环”的方式来正确建立标签的父子层级关系，
/// 并将最终的TagData资产注册到全局的 ImporterCache 中。
/// </summary>
public class TagImporter : BaseDataImporter
{
    private const string TagsExcelPath = "Assets/Editor/Sheets/Tags.xlsx";
    private const string TagsOutputPath = "Assets/Resources/Data/Tags";
    private const string TagPrefix = "Tag";

    /// <summary>
    /// Unity菜单入口
    /// </summary>
    [MenuItem("游戏工具/从Excel导入标签数据")]
    public static void RunImport()
    {
        new TagImporter().Import();
    }

    /// <summary>
    /// 实现父类要求的主处理方法。
    /// </summary>
    protected override void Process()
    {
        Debug.Log("--- 开始导入标签数据 ---");

        DataTable table = ReadExcelSheet(TagsExcelPath, "Tags");
        if (table == null) return;

        // 使用一个临时的本地字典来辅助完成“两遍循环”的逻辑
        var localTagCache = new Dictionary<int, TagData>();

        // =================================================================
        // 第一遍循环 (Pass 1): 创建所有 TagData 资产
        // =================================================================
        // 在这一遍，我们只创建资产并填充基础数据，但不处理父子关系。
        for (int i = 3; i < table.Rows.Count; i++) // 假设数据从第5行开始
        {
            DataRow row = table.Rows[i];
            if (IsRowEmpty(row)) continue;

            int id = ParseInt(row["ID"]);
            if (id == 0) continue;

            string name = ParseString(row["name"]);
            string assetPath = $"{TagsOutputPath}/{TagPrefix}_{id}_{SanitizeFileName(name)}.asset";
            
            var asset = GetOrCreateAsset<TagData>(assetPath);
            
            // 填充基础字段
            asset.UniqueID = id;
            asset.tagName = name;
            asset.description = ParseString(row["description"]);
            asset.parent = null; // 关键：在第一遍循环中，暂时不设置父节点

            // 将创建的资产存入本地缓存，并注册到全局缓存
            localTagCache[id] = asset;
            
            EditorUtility.SetDirty(asset);
        }

        // =================================================================
        // 第二遍循环 (Pass 2): 建立父子关系
        // =================================================================
        // 在这一遍，我们遍历刚刚创建的所有资产，并为它们设置正确的父节点。
        foreach (var asset in localTagCache.Values)
        {
            int parentId = asset.UniqueID / 10; // 根据ID规则计算父ID (例如 21 -> 2)
            
            // 只有当父ID有效，并且不是自身时才查找
            if (parentId > 0 && parentId != asset.UniqueID)
            {
                // 从本地缓存中查找父节点资产
                if (localTagCache.TryGetValue(parentId, out TagData parentAsset))
                {
                    asset.parent = parentAsset;
                    EditorUtility.SetDirty(asset);
                }
            }
        }
        
        Debug.Log($"成功处理 'Tags' 工作表，创建/更新了 {localTagCache.Count} 个标签。");
    }

    
}