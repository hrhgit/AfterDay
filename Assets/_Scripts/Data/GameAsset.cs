using UnityEngine;

// 所有ScriptableObject数据资产的基类
public abstract class GameAsset : ScriptableObject
{
    [Tooltip("用于存档和引用的唯一ID，在项目中必须唯一！")]
    public int UniqueID;
    
}