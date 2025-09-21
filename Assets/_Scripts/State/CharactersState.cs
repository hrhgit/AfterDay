using System.Collections.Generic;

// 用于存档所有角色（人类和机器人）状态的数据容器
[System.Serializable]
public class CharactersState
{
    public List<HumanState> humanCharacters;
    public List<RobotState> robotFleet;

    public CharactersState()
    {
        humanCharacters = new List<HumanState>();
        robotFleet = new List<RobotState>();
    }
}