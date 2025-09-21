using UnityEngine;

// 所有ScriptableObject数据资产的基类
public abstract class GameAsset : ScriptableObject
{
    [Tooltip("用于存档和引用的唯一ID，在项目中必须唯一！")]
    public string UniqueID;

    // OnValidate() 是一个Unity编辑器函数
    // 当在Inspector中修改脚本数据时，它会被调用
    private void OnValidate()
    {
        // 如果UniqueID是空的，并且这个资产已经存在于项目中 (有文件名)
        if (string.IsNullOrWhiteSpace(UniqueID) && !string.IsNullOrWhiteSpace(this.name))
        {
            // 将文件名（通常是小写加下划线）作为初始ID
            UniqueID = this.name.ToLower().Replace(" ", "_");
        }
    }
}