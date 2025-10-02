using UnityEngine;
using UnityEditor;
using System.IO;
using System.Data;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 专门负责从 Tags.xlsx 文件导入并生成 TagData 资产的编辑器工具。
/// </summary>
public class TagImporter : BaseDataImporter
{
    private const string ExcelPath = "Assets/Editor/Sheets/Tags.xlsx";
    private const string OutputPath = "Assets/Resources/Data/Tags";

    [MenuItem("游戏工具/导入数据/★ 导入标签 (Tags)")]
    public static void RunImport()
    {
        // 创建实例并调用父类的统一入口方法
        new TagImporter().Import();
    }
    
    /// <summary>
    /// 实现父类要求的核心处理逻辑。
    /// </summary>
    protected override void Process()
    {
        Debug.Log("--- 开始导入标签数据 ---");
        
        // 使用父类的 CacheTags 方法来初始化缓存
        CacheTags();

        DataTable table = ReadExcelSheet(ExcelPath, "Tags");
        if (table == null) return;
        
        // --- 第一次遍历：创建或更新所有资产 ---
        // 使用父类中定义的规则 (DataStartRow)
        for (int i = DataStartRow - 2; i < table.Rows.Count; i++)
        {
            DataRow row = table.Rows[i];
            if (IsRowEmpty(row)) break;

            int id = ParseInt(row["ID"]);
            string tagName = ParseString(row["Tag"]);
            
            if (id == 0 || string.IsNullOrWhiteSpace(tagName)) continue;

            string assetPath = $"{OutputPath}/Tag_{id}_{SanitizeFileName(tagName)}.asset";
            
            var asset = GetOrCreateAsset<TagData>(assetPath);
            string oldJson = JsonUtility.ToJson(asset);
            
            asset.id = id;
            asset.tagName = tagName;
            asset.description = ParseString(row["note"]);
            asset.parent = null; // 先重置父级，防止旧引用残留

            if (oldJson != JsonUtility.ToJson(asset))
            {
                EditorUtility.SetDirty(asset);
            }

            // 更新缓存
            if (!TagCache.ContainsKey(id))
            {
                TagCache.Add(id, asset);
            }
        }
        
        // --- 第二次遍历：建立父子引用关系 ---
        foreach (var asset in TagCache.Values)
        {
            // 规则：ID为两位数或更多，则表示有父级
            if (asset.id >= 10)
            {
                int parentId = asset.id / 10; // 例如：21 -> 2
                if (TagCache.TryGetValue(parentId, out TagData parentAsset))
                {
                    if (asset.parent != parentAsset)
                    {
                        asset.parent = parentAsset;
                        EditorUtility.SetDirty(asset);
                    }
                }
                else
                {
                    Debug.LogWarning($"标签 '{asset.tagName}' (ID: {asset.id}) 的父级标签 (ID: {parentId}) 不存在。");
                }
            }
        }
    }
    /// <summary>
    /// 一个静态方法，供其他导入器在运行前加载和缓存所有已存在的TagData资产。
    /// </summary>
    public static void CacheTags()
    {
        if (TagCache != null && TagCache.Count > 0) return; // 如果已经缓存，则跳过

        Debug.Log("正在缓存所有TagData资产...");
        TagCache = new Dictionary<int, TagData>();
        
        // 从项目中查找所有已存在的TagData资产
        var allGuids = AssetDatabase.FindAssets("t:TagData");
        foreach(var guid in allGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var tagAsset = AssetDatabase.LoadAssetAtPath<TagData>(path);
            if(tagAsset != null && !TagCache.ContainsKey(tagAsset.id))
            {
                TagCache.Add(tagAsset.id, tagAsset);
            }
        }
        Debug.Log($"已缓存 {TagCache.Count} 个标签。");
    }
}

