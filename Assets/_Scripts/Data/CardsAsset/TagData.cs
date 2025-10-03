using UnityEngine;

[CreateAssetMenu(fileName = "NewTag", menuName = "Game Data/Tag")]
public class TagData : GameAsset
{
    public string tagName;
    public string description;
    public TagData parent; 
}