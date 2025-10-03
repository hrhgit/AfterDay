using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 在编辑器导入流程中，提供一个全局的、静态的数据缓存。
/// 用于处理跨表引用（ref）。
/// 它的生命周期仅限于当次Unity编辑器会话。
/// </summary>
public static class ImporterCache
{
    /// <summary>
    /// 全局唯一的中央缓存，存储所有已导入的数据资产。
    /// Key: UniqueID
    /// Value: 继承自GameAsset的ScriptableObject资产
    /// </summary>
    public static Dictionary<int, GameAsset> UniversalCache = new Dictionary<int, GameAsset>();

    /// <summary>
    /// 在Unity菜单中添加一个清理缓存的按钮。
    /// 在进行全量导入前，建议先执行此操作。
    /// </summary>
    [MenuItem("游戏工具/导入数据/★ 清理全局缓存")]
    public static void ClearCache()
    {
        UniversalCache.Clear();
        Debug.Log("Importer Universal Cache has been cleared.");
    }

    /// <summary>
    /// 向全局缓存中注册一个资产。
    /// </summary>
    /// <param name="asset">要注册的资产</param>
    public static void RegisterAsset(GameAsset asset)
    {
        if (asset == null || asset.UniqueID == 0)
        {
            return;
        }

        // 检查ID是否已存在，这通常意味着Excel表中有重复ID，是需要修复的严重问题。
        if (UniversalCache.ContainsKey(asset.UniqueID))
        {
            // 更新已有的引用，以防是重复导入
            UniversalCache[asset.UniqueID] = asset; 
            // 如果您希望对重复ID进行严格报错，可以使用下面这行
            // Debug.LogError($"ID冲突！UniqueID '{asset.UniqueID}' 已被注册。请检查您的Excel表。");
        }
        else
        {
            UniversalCache.Add(asset.UniqueID, asset);
        }
    }
}
