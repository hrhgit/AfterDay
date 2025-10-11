using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LocationRuntimeState
{
    public int locationDataID;
    public int remainingExplorations;

    // 老字段：仍保留，但新逻辑不再使用
    public List<RandomRewardDrop> availableRandomRewards;

    // ★ 新增：整局分配表（每个探索槽位一个 CardReward；可能为空：quantity=0）
    public CardReward[] perSlotReward;

    // ★ 新增：初始化时生成一次的随机种子（可选）
    public int seedUsed = 0;

    public LocationRuntimeState(LocationData blueprint)
    {
        locationDataID = blueprint.UniqueID;
        remainingExplorations = blueprint.totalExplorations;
        availableRandomRewards = new List<RandomRewardDrop>(blueprint.randomRewardPool);

        // ★★ 核心：生成“每格一个 CardReward”的分配表
        seedUsed = (int)DateTime.Now.Ticks; // 你也可来自玩家/关卡seed
        UnityEngine.Random.InitState(seedUsed);
        perSlotReward = RewardDistributor.BuildSchedule(
            totalSlots: blueprint.totalExplorations,
            plans: blueprint.allRewards
        );
    }
}

// ========== 分配器（嵌在同文件，方便你直接用） ==========
internal static class RewardDistributor
{
    // 主入口：把每个计划项的“总量”分到每个槽位（每格1个CardReward，冲突自动重排）
    public static CardReward[] BuildSchedule(int totalSlots, List<RewardPlanItem> plans)
    {
        var slots = new CardReward[totalSlots];
        for (int i = 0; i < totalSlots; i++) slots[i] = new CardReward { card = null, quantity = 0 };

        if (plans == null || totalSlots <= 0) return slots;

        // 针对每个物品计划，逐单位地“放豆子”，直到它的quantity分完
        foreach (var plan in plans)
        {
            if (plan?.card == null || plan.quantity <= 0) continue;
            var rule = plan.rule ?? new RewardDistributionRule();

            // 1) 窗口解析
            int E = Mathf.Clamp(rule.earliestIndex, 0, Math.Max(0, totalSlots - 1));
            int L = (rule.latestIndex <= 0) ? totalSlots - 1 : Mathf.Clamp(rule.latestIndex, E, totalSlots - 1);
            int windowCount = L - E + 1;
            if (windowCount <= 0) continue;

            // 2) 基线权重（Beta(mean,kappa)，只要相对量，常数因子可略）
            var weightsBase = new float[windowCount];
            for (int j = 0; j < windowCount; j++)
            {
                float p = (windowCount == 1) ? 0.5f : (float)j / (windowCount - 1); // 归一化到[0,1]
                weightsBase[j] = BetaShapeWeight(p, rule.mean, rule.kappa); // kappa==0 => 1
                if (weightsBase[j] <= 0f) weightsBase[j] = 1e-6f;
            }

            // 3) 峰点：自动K与容量（峰容量是“数量上限”）
            var peakCaps = new Dictionary<int, int>(); // key=槽位索引, val=剩余容量(数量)
            if (rule.peakBoost > 0f && rule.peakMaxPerPoint > 0)
            {
                int K = Mathf.Max(1, plan.quantity / rule.peakMaxPerPoint); // Floor 自动
                K = Mathf.Min(K, windowCount); // 不超过可用槽位
                var peaks = WeightedPickDistinct(E, L, weightsBase, K);
                foreach (int s in peaks)
                {
                    peakCaps[s] = rule.peakMaxPerPoint;
                }
            }

            // 4) 逐“单位数量”分配，直到该物品 quantity 用完
            var usedSlots = new List<int>(); // 用于 minDistanceBetweenSame 约束
            int remain = plan.quantity;
            int guard = 0; // 安全阈，防无限循环
            while (remain > 0 && guard++ < plan.quantity * 20)
            {
                // 4.1 计算当前可用权重（窗口内），应用峰点增益与距离约束
                var wNow = new float[windowCount];
                float sumW = 0f;
                for (int j = 0; j < windowCount; j++)
                {
                    int s = E + j;

                    // 最小间距：如果距离最近一次用到的槽 < minDistance，就禁用
                    bool blocked = false;
                    if (rule.minDistanceBetweenSame > 0)
                    {
                        foreach (var u in usedSlots)
                        {
                            if (Mathf.Abs(u - s) < rule.minDistanceBetweenSame)
                            {
                                blocked = true; break;
                            }
                        }
                    }

                    if (blocked) { wNow[j] = 0f; continue; }

                    // 峰点：有容量就乘 (1+peakBoost)，容量见底则禁用该峰
                    if (peakCaps.TryGetValue(s, out int cap))
                    {
                        if (cap <= 0) { wNow[j] = 0f; continue; }
                        wNow[j] = weightsBase[j] * (1f + rule.peakBoost);
                    }
                    else
                    {
                        wNow[j] = weightsBase[j];
                    }

                    // 槽内已有别的物品且不是同一种卡 → 不允许（单格一个CardReward）
                    if (slots[s].card != null && slots[s].card != plan.card) wNow[j] = 0f;

                    if (wNow[j] < 0f) wNow[j] = 0f;
                    sumW += wNow[j];
                }

                // 如果全被禁了（可能因为间距或全被占），放宽：只要“不是别人占的卡”，就用均匀权重
                if (sumW <= 0f)
                {
                    for (int j = 0; j < windowCount; j++)
                    {
                        int s = E + j;
                        bool occupiedByOther = (slots[s].card != null && slots[s].card != plan.card);
                        if (!occupiedByOther) wNow[j] = 1f; else wNow[j] = 0f;
                    }
                    sumW = Sum(wNow);
                    if (sumW <= 0f) break; // 实在放不下了，只能终止（避免死循环）
                }

                // 4.2 加权抽一个槽位
                int pick = WeightedIndex(wNow, sumW);
                int slotIndex = E + pick;

                // 4.3 落位：该槽是空或同卡 → 累加数量（单格一个CardReward）
                if (slots[slotIndex].card == null)
                {
                    slots[slotIndex].card = plan.card;
                    slots[slotIndex].quantity = 1;
                }
                else
                {
                    // 已是同一张卡
                    slots[slotIndex].quantity += 1;
                }

                usedSlots.Add(slotIndex);
                remain--;

                // 峰容量-1
                if (peakCaps.TryGetValue(slotIndex, out int capNow))
                {
                    peakCaps[slotIndex] = Mathf.Max(0, capNow - 1);
                }
            }
        }

        return slots;
    }

    // —— 相对 Beta 权重（只要形状；常数略）：
    
// meanSym ∈ [-1,1]；kappa ≥ 0
    private static float BetaShapeWeight(float p, float meanSym, float kappa)
    {
        // 关闭偏好：严格均匀
        if (kappa <= 0f) return 1f;

        // 把对称域 [-1,1] 映射到 Beta 的均值域 [0,1]
        float m = 0.5f * (meanSym + 1f);   // -1→0（偏前）、0→0.5（居中）、+1→1（偏后）

        // 采用“nu≥2”的浓度参数化，保证 a,b≥1（单峰、不会U形）
        // kappa=0 → nu=2 → a=b=1（均匀）；kappa增大→nu增大→集中度提升
        float nu = kappa + 2f;
        float a  = m * (nu - 2f) + 1f;
        float b  = (1f - m) * (nu - 2f) + 1f;

        // 形状项：p^(a-1) * (1-p)^(b-1)，边界做夹紧避免NaN
        float pp = Mathf.Clamp(p, 1e-4f, 1f - 1e-4f);
        return Mathf.Pow(pp, a - 1f) * Mathf.Pow(1f - pp, b - 1f);
    }


    private static int WeightedIndex(float[] w, float sum)
    {
        float r = UnityEngine.Random.value * sum;
        float acc = 0f;
        for (int i = 0; i < w.Length; i++)
        {
            acc += w[i];
            if (r <= acc) return i;
        }
        return w.Length - 1;
    }

    private static int[] WeightedPickDistinct(int E, int L, float[] wBase, int K)
    {
        // 简单的不放回抽样：每次按当前权重抽1个并置0
        int n = L - E + 1;
        K = Mathf.Clamp(K, 0, n);
        var picks = new List<int>(K);
        var w = (float[])wBase.Clone();
        for (int t = 0; t < K; t++)
        {
            float sum = Sum(w);
            if (sum <= 0f) break;
            int idx = WeightedIndex(w, sum);
            picks.Add(E + idx);
            w[idx] = 0f; // 不放回
        }
        return picks.ToArray();
    }

    private static float Sum(float[] arr)
    {
        float s = 0f; for (int i = 0; i < arr.Length; i++) s += arr[i]; return s;
    }
}
