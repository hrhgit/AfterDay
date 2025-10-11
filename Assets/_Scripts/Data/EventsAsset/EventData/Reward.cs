using System;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public class RewardDistributionRule
{
    // 出现窗口（0-based，含端点）
    public int earliestIndex = 0;
    public int latestIndex   = 0;

    // 区域偏好（Beta 的 Mean/Kappa 表示；kappa==0 表示不启用）
    public float mean  = 0f;   // 0=前，1=后，中段≈0.5
    public float kappa = 0f;   // 0=不生效

    // 离散峰点（自动 K = max(1, floor(copies/peakMaxPerPoint))，由分配器计算）
    public float peakBoost       = 0f; // 权重乘 (1 + peakBoost) 仅对峰点生效；0=不启用
    public int   peakMaxPerPoint = 0;  // 单个峰点容量；0=不启用
    
    // 最小间距（同一奖励两次出现的最小槽间距；0=不限制）
    public int minDistanceBetweenSame = 0;
}

[Serializable]
public class RewardPlanItem
{
    public CardData card;
    public int quantity= 0; // 总发放的数量
    public RewardDistributionRule rule = new RewardDistributionRule();
}