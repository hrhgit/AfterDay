using UnityEngine;

[CreateAssetMenu(fileName = "NewRobotPawn", menuName = "Game Data/Robot Pawn")]
public class RobotPawnData : PawnData // 继承自新的、精简的PawnData
{
    [Header("机器人核心属性")]
    [Range(0, 10)]
    public int movement = 1;
    
    [Range(0, 10)]
    public int calculation = 1;
    
    [Range(0, 10)]
    public int search = 1;
    
    [Range(0, 10)]
    public int art = 1;
    
    // 你可以在这里添加机器人特有的其他属性，比如“能源上限”、“装甲类型”等
}