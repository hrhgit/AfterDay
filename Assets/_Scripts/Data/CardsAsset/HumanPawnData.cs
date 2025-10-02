using UnityEngine;

[CreateAssetMenu(fileName = "EmmaPawn", menuName = "Game Data/Human Pawn")]
public class HumanPawnData : CardData // 继承自CardData
{
    // 这个类现在非常干净，只包含人类的基础设定值。
    // 实际的健康、饥饿、情绪值是在运行时由 EmmaState 管理。
    
    [Header("波夏的基础设定")]
    [Tooltip("游戏开始时的初始健康值 (上限)")]
    public int initialHealth = 100; // 艾玛的健康上限和初始值

    [Tooltip("游戏开始时的初始情绪值 (上限)")]
    public int initialMorale = 75; // 艾玛的情绪上限和初始值

    [Tooltip("每回合自动增加的饥饿值。达到一定阈值会影响健康和情绪。")]
    public float hungerGainPerTurn = 10.0f; // 每回合饥饿值增加量 (例如，0-100表示饥饿程度)

    [Tooltip("每消耗1单位食物，降低多少饥饿值。")]
    public float hungerReducedPerFood = 20.0f; // 每消耗1单位食物，降低的饥饿量

    [Tooltip("饥饿值达到此阈值时，开始对健康和情绪产生负面影响。")]
    public float criticalHungerThreshold = 70.0f; 
}