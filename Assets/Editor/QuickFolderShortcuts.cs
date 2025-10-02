using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics; // 需要引入这个库来启动外部进程

/// <summary>
/// 一个Unity编辑器工具，用于在操作系统的文件资源管理器中快速打开常用文件夹。
/// </summary>
public static class OpenFolderInExplorer
{
    // --- 在这里定义您想要快速访问的文件夹路径 ---
    // 路径必须从 "Assets/" 开始，并使用正斜杠 "/"

    private const string SheetsPath = "Assets/Editor/Sheets";
    private const string CardArtPath = "Assets/Resources/Art/Cards"; 
    private const string PYPath = "Assets/Editor/py"; 


    [MenuItem("打开文件夹//1. 表格文件夹 (Sheets)", false, 1)]
    private static void OpenSheetsFolder()
    {
        OpenPathInFileBrowser(SheetsPath);
    }

    [MenuItem("打开文件夹//2. 卡牌图片文件夹 (Art)", false, 2)]
    private static void OpenCardsArtFolder()
    {
        OpenPathInFileBrowser(CardArtPath);
    }
    [MenuItem("打开文件夹//3. py表格编辑器 (PY)", false, 3)]
    private static void OpenPYFolder()
    {
        OpenPathInFileBrowser(PYPath);
    }

    

    
    
    /// <summary>
    /// 在操作系统的文件资源管理器中打开指定的项目相对路径。
    /// </summary>
    private static void OpenPathInFileBrowser(string projectRelativePath)
    {
        // 将项目相对路径（如 "Assets/Editor/Sheets"）转换为系统的绝对路径
        string fullPath = Path.GetFullPath(projectRelativePath);

        // --- 核心修改：检查文件夹是否存在 ---
        if (Directory.Exists(fullPath))
        {
            // 如果存在，则打开它
            Process.Start(fullPath);
        }
        else
        {
            // 如果不存在，则弹出错误对话框并打印错误日志
            string errorMessage = $"文件夹不存在: \n{fullPath}";
            EditorUtility.DisplayDialog("错误", errorMessage, "确定");
            UnityEngine.Debug.LogError(errorMessage);
        }
    }
}
