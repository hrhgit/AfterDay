using UnityEngine;

[CreateAssetMenu(fileName = "NewTag", menuName = "Game Data/Tag")]
public class TagData : ScriptableObject
{
    public int id;
    public string tagName;
    public string description;
    public TagData parent; 
}