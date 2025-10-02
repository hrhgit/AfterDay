using UnityEngine;
using UnityEditor;
using System.IO;
using System.Data;
using System.Linq;
using System.Text;
using ExcelDataReader;

/// <summary>
/// 从 Tags.xlsx 文件自动生成 CardTag.cs 枚举脚本。
/// </summary>
public static class TagEnumGenerator
{
    private const string ExcelPath = "Assets/Editor/Sheets/Tags.xlsx";
    private const string EnumOutputPath = "Assets/_Scripts/Data/CardsAsset/Tags.cs"; // 确保路径正确

    [MenuItem("游戏工具/★ 第一步：从Tags表生成枚举")]
    public static void GenerateEnum()
    {
        Debug.Log("开始从 Tags.xlsx 生成 CardTag 枚举...");

        if (!File.Exists(ExcelPath))
        {
            Debug.LogError($"Excel文件未找到: {ExcelPath}");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// 【自动生成】定义游戏中所有实体可能拥有的所有标签。");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public enum Tags");
        sb.AppendLine("{");

        using (var stream = File.Open(ExcelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var result = reader.AsDataSet(new ExcelDataSetConfiguration() { ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true } });
                DataTable table = result.Tables["Tags"];

                foreach (DataRow row in table.AsEnumerable().Skip(3)) // 从第5行开始
                {
                    if (row.IsNull("ID")) break;

                    string tagName = row["Tag"].ToString().Trim();
                    int tagId = int.Parse(row["ID"].ToString());
                    string note = row["note"].ToString();

                    if (!string.IsNullOrWhiteSpace(tagName))
                    {
                        if(!string.IsNullOrWhiteSpace(note))
                        {
                            sb.AppendLine($"    // {note}");
                        }
                        sb.AppendLine($"    {tagName} = {tagId},");
                    }
                }
            }
        }

        sb.AppendLine("}");
        
        Directory.CreateDirectory(Path.GetDirectoryName(EnumOutputPath));
        File.WriteAllText(EnumOutputPath, sb.ToString());

        AssetDatabase.Refresh();
        Debug.Log($"CardTag.cs 文件已成功生成到: {EnumOutputPath}");
    }
}
