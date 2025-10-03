using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Data;

/// <summary>
/// 继承自BaseDataImporter，专门负责从Excel文件导入并生成复杂的需求规则资产树。
/// </summary>
public class RuleImporter : BaseDataImporter
{
    private const string ExcelPath = "Assets/Editor/Sheets/Requirements.xlsx";
    private const string OutputPath = "Assets/Resources/Data/Rules/Requirements";
    
    // 缓存所有可引用的 GameAsset，用于解析ID规则
    private static Dictionary<int, GameAsset> _assetDatabaseCache;

    [MenuItem("游戏工具/从Excel导入需求规则")]
    public static void RunImport()
    {
        // 创建实例并调用父类的统一入口方法
        new RuleImporter().Import();
    }
    
    //----------------------------------------------------------------------
    // 核心流程
    //----------------------------------------------------------------------

    /// <summary>
    /// 实现父类要求的核心处理逻辑。
    /// </summary>
    protected override void Process()
    {
        Debug.Log("--- 开始导入需求规则 ---");
        TagImporter.CacheTags();
        Directory.CreateDirectory(OutputPath);
        
        // 这是RuleImporter特有的准备工作：预加载所有卡牌/物品资产
        CacheAllGameAssets();
        
        // 注意：这里我们假设工作表名为 "Requirements"
        DataTable table = ReadExcelSheet(ExcelPath, "Requirements");
        if (table == null) return;
        
        // 使用父类中定义的规则 (DataStartRow, IdColumnName) 进行循环
        for (int i = DataStartRow - 2; i < table.Rows.Count; i++)
        {
            DataRow row = table.Rows[i];
            if (row == null || row.IsNull(IdColumnName) || string.IsNullOrWhiteSpace(row[IdColumnName].ToString()))
            {
                break; // 遇到空ID行，终止读取
            }

            string ruleID = row["ID"].ToString().Trim();
            string name = row["Name"].ToString().Trim();
            string categoryStr = row["catagory"].ToString().Trim(); // 读取新的类别列
            string requirementStr = row["Requirement"].ToString().Trim();
            
            if (string.IsNullOrWhiteSpace(ruleID)) continue;

            // 步骤 1: 根据 Requirement 列递归解析并创建主规则资产
            ValidationRule finalRule = ParseAndCreateRuleAsset(requirementStr, ruleID, name);
            
            // 步骤 2: 如果成功生成了主规则，就为它填充类别限制
            if(finalRule != null)
            {
                string oldJson = JsonUtility.ToJson(finalRule); // 记录填充类别前的状态

                finalRule.requiredTags.Clear();
                if (!string.IsNullOrWhiteSpace(categoryStr) && categoryStr != "*")
                {
                    // 解析并添加类别
                    finalRule.requiredTags.Add(ParseEnum<Tags>(categoryStr, Tags.Card));
                }
                
                // 只有当类别信息发生变化时，才标记资产
                if (oldJson != JsonUtility.ToJson(finalRule))
                {
                    EditorUtility.SetDirty(finalRule);
                }
            }
        }
    }
    
    
    /// <summary>
    /// 递归解析器入口，负责处理 & || ()
    /// </summary>
    private ValidationRule ParseAndCreateRuleAsset(string req, string id, string name)
    {
        string finalAssetName = $"sub_{SanitizeFileName(id)}_{SanitizeFileName(name)}";
        string path = $"{OutputPath}/{finalAssetName}.asset";
        
        var orParts = SplitRespectingParentheses(req, "||");
        if (orParts.Length > 1)
        {
            var orRule = GetOrCreateAsset<OrRule>(path);
            orRule.nestedRules.Clear();
            foreach (var part in orParts)
            {
                var subRule = ParseAndCreateRuleAsset(part, $"{id}_OR_{orRule.nestedRules.Count}", $"{name}_OR");
                orRule.nestedRules.Add(subRule);
            }
            EditorUtility.SetDirty(orRule);
            return orRule;
        }

        var andParts = SplitRespectingParentheses(req, "&");
        if (andParts.Length > 1)
        {
            var andRule = GetOrCreateAsset<AndRule>(path);
            andRule.nestedRules.Clear();
            foreach (var part in andParts)
            {
                var subRule = ParseAndCreateRuleAsset(part, $"{id}_AND_{andRule.nestedRules.Count}", $"{name}_AND");
                andRule.nestedRules.Add(subRule);
            }
            EditorUtility.SetDirty(andRule);
            return andRule;
        }
        
        req = req.Trim();
        if (req.StartsWith("(") && req.EndsWith(")"))
        {
            return ParseAndCreateRuleAsset(req.Substring(1, req.Length - 2), id, name);
        }

        return ParseSingleCondition(req, path);
    }
    
    /// <summary>
    /// 解析单一条件 
    /// </summary>
    private ValidationRule ParseSingleCondition(string condition, string path)
    {
        var match = Regex.Match(condition.Trim(), @"(.+?)\s*(==|!=|>=|<=|>|<)\s*(.+)");
        if (!match.Success) { Debug.LogError($"无法解析单一条件: {condition}"); return null; }

        string front = match.Groups[1].Value.Trim();
        string symbol = match.Groups[2].Value.Trim();
        string back = match.Groups[3].Value.Trim();
        
        var existingRule = AssetDatabase.LoadAssetAtPath<ValidationRule>(path);

        // 检测ID: ID == 12345
        if (front.Equals("ID", StringComparison.OrdinalIgnoreCase))
        {
            var rule = GetOrCreateAsset<IdRule>(path);
            string oldJson = JsonUtility.ToJson(rule);
            
            rule.requiredCardIDs.Clear();
            if (int.TryParse(back, out int id))
            {
                if(!_assetDatabaseCache.ContainsKey(id)) Debug.LogWarning($"资产缓存中未找到ID为 '{id}' 的资产");
                rule.requiredCardIDs.Add(id);
            }

            if (existingRule == null || oldJson != JsonUtility.ToJson(rule)) EditorUtility.SetDirty(rule);
            return rule;
        }
        // 检测属性: calculation > 5
        else
        {
            var rule = GetOrCreateAsset<AttributeRule>(path);
            string oldJson = JsonUtility.ToJson(rule);
            
            rule.attributeName = front;
            rule.targetValue = float.Parse(back);
            rule.comparison = symbol switch {
                ">" => AttributeRule.ComparisonType.GreaterThan,
                "<" => AttributeRule.ComparisonType.LessThan,
                "==" => AttributeRule.ComparisonType.EqualTo,
                "!=" => AttributeRule.ComparisonType.NotEqualTo,
                ">=" => AttributeRule.ComparisonType.GreaterThanOrEqual, 
                "<=" => AttributeRule.ComparisonType.LessThanOrEqual,    
                _ => rule.comparison
            };
            
            if (existingRule == null || oldJson != JsonUtility.ToJson(rule)) EditorUtility.SetDirty(rule);
            return rule;
        }
    }
    
    #region RuleImporter Specific Helpers

    /// <summary>
    /// 预加载项目中所有GameAsset到缓存字典中，以数字ID为键
    /// </summary>
    private void CacheAllGameAssets()
    {
        _assetDatabaseCache = new Dictionary<int, GameAsset>();
        var allGuids = AssetDatabase.FindAssets("t:GameAsset");
        foreach (var guid in allGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<GameAsset>(path);
            if (asset != null && !_assetDatabaseCache.ContainsKey(asset.UniqueID))
            {
                _assetDatabaseCache.Add(asset.UniqueID, asset);
            }
        }
    }

    /// <summary>
    /// 一个能正确处理括号的字符串分割方法
    /// </summary>
    private string[] SplitRespectingParentheses(string input, string delimiter)
    {
        List<string> result = new List<string>();
        int partStart = 0;
        int parenthesesDepth = 0;
        for (int i = 0; i <= input.Length - delimiter.Length; i++)
        {
            if (input[i] == '(') parenthesesDepth++;
            else if (input[i] == ')') parenthesesDepth--;

            if (parenthesesDepth == 0 && input.Substring(i, delimiter.Length) == delimiter)
            {
                result.Add(input.Substring(partStart, i - partStart).Trim());
                partStart = i + delimiter.Length;
                i += delimiter.Length - 1;
            }
        }
        result.Add(input.Substring(partStart).Trim());
        return result.Where(s => !string.IsNullOrEmpty(s)).ToArray();
    }

    #endregion
}

