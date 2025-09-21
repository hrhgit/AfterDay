using UnityEngine;

// 这是所有棋子的“共同祖先”，只包含最基础的通用信息。
public abstract class PawnData : GameAsset 
{
    [Header("基本信息")]
    public string pawnName;
    [TextArea] public string description;
    public Sprite icon;
}