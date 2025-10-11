using UnityEditor;
using System.Data;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 合并版地点导入器：在一个脚本里依次读取同一 Excel 中的
/// 1) RewardDistribution 表 → 构建 RewardPlanItem 模板映射
/// 2) Locations 表 → 生成/更新 LocationData，并用 allRewards 的 5位ID 引用上面的模板
/// </summary>
public class LocationImporter : BaseDataImporter
{
    // Excel 路径与 Sheet 名（两张表在同一个文件里）
    private const string ExcelPath          = "Assets/Editor/Sheets/Locations.xlsx";
    private const string Sheet_RewardDist   = "RewardDistribution";
    private const string Sheet_Locations    = "Locations";

    // 输出与命名
    private const string LocationsOutputPath = "Assets/Resources/Data/Locations";
    private const string LocationPrefix      = "LOC";

    [MenuItem("游戏工具/从Excel导入地点+奖励分配")]
    public static void RunImport()
    {
        new LocationImporter().Import();
    }

    protected override void Process()
    {
        Debug.Log("--- 开始导入：RewardDistribution + Locations ---");

        // 资产缓存
        var cardCache   = LoadAllAssets<CardData>();
        var eventCache  = LoadAllAssets<EventData>();
        

        // 1) 先读 RewardDistribution（ID -> RewardPlanItem 模板）
        var rewardMap = LoadRewardDistributionMap(cardCache);

        // 2) 再读 Locations，用 allRewards 的5位ID 引用上面的模板
        ProcessLocationsSheet(rewardMap, cardCache, eventCache);

        Debug.Log("--- 导入完成 ---");
    }

    // ================= RewardDistribution =================

    /// <summary>
    /// 读取 RewardDistribution 表，构建“5位ID → RewardPlanItem 模板”映射
    /// </summary>
    private Dictionary<int, RewardPlanItem> LoadRewardDistributionMap(
        Dictionary<int, GameAsset> cardCache
    )
    {
        var map = new Dictionary<int, RewardPlanItem>();

        DataTable table = ReadExcelSheet(ExcelPath, Sheet_RewardDist);
        if (table == null) return map;

        var header = ParseHeader(table);
        var H = header.ToDictionary(h => h.FieldName, h => h, System.StringComparer.OrdinalIgnoreCase);

        // 数据行从 DataStartRow 开始；与 CardImporter 一致，这里用索引 (DataStartRow-2) == 3
        for (int r = DataStartRow - 2; r < table.Rows.Count; r++)
        {
            DataRow row = table.Rows[r];
            if (IsRowEmpty(row)) continue;

            // 兼容列名 "RewardID" 或 "ID"
            int rewardId = GetValue<int>(row, H, "RewardID");
            if (rewardId == 0) rewardId = GetValue<int>(row, H, "ID");
            if (rewardId == 0) { Debug.LogWarning($"[RewardDistribution] 第{r+1}行缺少 RewardID/ID"); continue; }

            int cardId   = GetValue<int>(row, H, "CardID");
            int quantity = GetValue<int>(row, H, "Quantity");
            if (quantity <= 0) { Debug.LogWarning($"[RewardDistribution] 第{r+1}行 Quantity<=0，跳过"); continue; }

            if (!cardCache.TryGetValue(cardId, out var cardSO) || !(cardSO is CardData card))
            {
                Debug.LogWarning($"[RewardDistribution] 第{r+1}行 未找到 CardID={cardId}，跳过"); continue;
            }

            // 规则字段（Mean ∈ [-1,1]；Kappa ≥ 0；LatestIndex<=0 的语义在运行时分配器里兜底为 total-1）
            int   earliest   = GetValue<int>(row, H, "EarliestIndex");
            int   latest     = GetValue<int>(row, H, "LatestIndex");
            float mean       = Mathf.Clamp(GetValue<float>(row, H, "Mean"), -1f, 1f);
            float kappa      = Mathf.Max(0f, GetValue<float>(row, H, "Kappa"));
            float peakBoost  = Mathf.Max(0f, GetValue<float>(row, H, "PeakBoost"));
            int   peakMax    = Mathf.Max(0,  GetValue<int>(row, H, "PeakMaxPerPoint"));
            int   minDist    = Mathf.Max(0,  GetValue<int>(row, H, "MinDistance"));

            var rule = new RewardDistributionRule
            {
                earliestIndex = Mathf.Max(0, earliest),
                latestIndex   = Mathf.Max(0, latest),
                mean          = mean,
                kappa         = kappa,
                peakBoost       = peakBoost,
                peakMaxPerPoint = peakMax,
                minDistanceBetweenSame = minDist
            };

            var plan = new RewardPlanItem { card = card, quantity = quantity, rule = rule };

            if (map.ContainsKey(rewardId))
                Debug.LogWarning($"[RewardDistribution] 重复 RewardID={rewardId}，将以较后者为准。");

            map[rewardId] = plan;
        }

        Debug.Log($"[RewardDistribution] 加载完成：{map.Count} 条。");
        return map;
    }

    // ================= Locations =================

    /// <summary>
    /// 读取 Locations 表，生成/更新 LocationData，并把 allRewards 的 5位ID 解析为 RewardPlanItem 列表
    /// </summary>
    private void ProcessLocationsSheet(
        Dictionary<int, RewardPlanItem> rewardMap,
        Dictionary<int, GameAsset> cardCache,
        Dictionary<int, GameAsset> eventCache
    )
    {
        DataTable table = ReadExcelSheet(ExcelPath, Sheet_Locations);
        if (table == null) return;

        var header = ParseHeader(table);
        var H = header.ToDictionary(info => info.FieldName, info => info, System.StringComparer.OrdinalIgnoreCase);

        for (int i = DataStartRow - 2; i < table.Rows.Count; i++)
        {
            DataRow row = table.Rows[i];
            if (IsRowEmpty(row)) continue;

            int id = GetValue<int>(row, H, "ID");
            if (id == 0) continue;

            string locName  = GetValue<string>(row, H, "name");
            string assetPath = $"{LocationsOutputPath}/{LocationPrefix}_{id}_{SanitizeFileName(locName)}.asset";

            var asset = GetOrCreateAsset<LocationData>(assetPath);
            string oldJson = JsonUtility.ToJson(asset);

            // --- 基础字段 ---
            asset.UniqueID     = id;
            asset.name         = locName;
            asset.description  = GetValue<string>(row, H, "description");
            asset.locationImage = GetSpriteValue(row, H, "locationImage");

            asset.inherentEvent    = FindAssetsInCache<EventData>(GetValue<string>(row, H, "inherentEvent"),   eventCache).FirstOrDefault();
            asset.potentialEvents  = FindAssetsInCache<EventData>(GetValue<string>(row, H, "potentialEvents"), eventCache);

            asset.totalExplorations = GetValue<int>(row, H, "totalExplorations");

            // --- allRewards：读取为 5位ID 列表 → 解析为 RewardPlanItem 列表（深拷贝） ---
            string allRewardsRaw = GetValue<string>(row, H, "allRewards"); // e.g. "30001;30002,30003"
            asset.allRewards = ParseRewardPlanRefs(allRewardsRaw, rewardMap);

            // --- 随机奖励池（若你原来有解析方法，可在此沿用；否则留空/按需实现） ---
            string randomPoolRaw = GetValue<string>(row, H, "randomRewardPool");
            asset.randomRewardPool = ParseRandomRewardDrops(randomPoolRaw, cardCache);

            if (oldJson != JsonUtility.ToJson(asset))
                EditorUtility.SetDirty(asset);

            Debug.Log($"[Locations] 生成/更新：ID={id}, Name='{locName}', allRewards.Count={asset.allRewards?.Count ?? 0}");
        }

        Debug.Log($"成功处理 '{Sheet_Locations}' 工作表。");
    }

    // ================= 工具：解析 allRewards 的 5位ID 引用 =================

    private List<RewardPlanItem> ParseRewardPlanRefs(string raw, Dictionary<int, RewardPlanItem> map)
    {
        var list = new List<RewardPlanItem>();
        if (string.IsNullOrWhiteSpace(raw) || map == null || map.Count == 0) return list;

        var tokens = raw.Split(new[] { ',', ';', '|', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var tk in tokens)
        {
            if (!int.TryParse(tk.Trim(), out int rid))
            {
                Debug.LogWarning($"[Locations] allRewards 含非数字 '{tk}'，已忽略。");
                continue;
            }
            if (!map.TryGetValue(rid, out var tpl))
            {
                Debug.LogWarning($"[Locations] 未找到 RewardDistribution ID={rid}，已忽略。");
                continue;
            }
            list.Add(ClonePlan(tpl)); // 深拷贝，避免多地点共享引用
        }
        return list;
    }

    private RewardPlanItem ClonePlan(RewardPlanItem src)
    {
        if (src == null) return null;
        return new RewardPlanItem
        {
            card = src.card,
            quantity = src.quantity,
            rule = new RewardDistributionRule
            {
                earliestIndex = src.rule.earliestIndex,
                latestIndex   = src.rule.latestIndex,
                mean          = src.rule.mean,
                kappa         = src.rule.kappa,
                peakBoost       = src.rule.peakBoost,
                peakMaxPerPoint = src.rule.peakMaxPerPoint,
                minDistanceBetweenSame = src.rule.minDistanceBetweenSame
            }
        };
    }
}
