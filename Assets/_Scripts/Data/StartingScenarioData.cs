using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 定义一个完整的游戏开局剧本。
/// 包含了初始角色、初始机器人和初始物品。
/// </summary>
[CreateAssetMenu(fileName = "NewStartingScenario", menuName = "Game Data/Starting Scenario")]
public class StartingScenarioData : GameAsset // 继承GameAsset以保持一致性
{
    [Header("剧本信息")]
    public string scenarioName;
    [TextArea] public string description;

    [Header("初始角色")]
    [Tooltip("这个剧本的主角蓝图")]
    public HumanPawnData playerCharacterBlueprint;

    [Header("初始同伴")]
    [Tooltip("开局时就拥有的机器人")]
    public List<RobotPawnData> startingRobots;

    [Header("初始资源")]
    [Tooltip("开局时就拥有的物品和资源")]
    public List<CardReward> startingItems; // 复用我们之前为事件结果定义的CardReward
}