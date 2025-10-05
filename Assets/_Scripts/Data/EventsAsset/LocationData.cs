using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Location_", menuName = "Game Data/Location")]
public class LocationData : GameAsset // 假设继承自GameAsset以获得UniqueID
{
    public string locationName;
    [TextArea] public string description;
    public Sprite locationImage;

    [Header("地点事件")]
    [Tooltip("此地点固有的、始终可用的事件，通常是'探索'。")]
    public EventData inherentEvent;

    [Tooltip("此地点可能发生的、需要条件解锁的隐藏事件列表。")]
    public List<EventData> potentialEvents;
}