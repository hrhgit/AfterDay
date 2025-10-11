using UnityEngine;


[CreateAssetMenu(fileName = "Event_Exploration_", menuName = "Game Data/Events/Exploration Event")]
public class ExplorationEventData : EventData 
{
    
    public override void Execute(EventManager manager, object context = null)
    {
        Debug.Log($"事件 '{name}' 被触发，请求 EventManager 执行探索...");
        
        // 将探索者信息 (context) 和事件本身传递给 EventManager
        manager.HandleExplorationEvent(this, context as RobotState);
    }
}