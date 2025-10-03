using UnityEditor;

// CardImporter.cs (简化后)
public class CardImporter : BaseDataImporter
{
    // 1. 定义常量 (路径和前缀)
    private const string CardsExcelPath = "Assets/Editor/Sheets/Cards.xlsx";
    private const string PawnsOutputPath = "Assets/Resources/Data/Pawns";
    

    // 2. 保留菜单入口
    [MenuItem("游戏工具/从Excel导入卡牌数据")]
    public static void RunImport()
    {
        new CardImporter().Import();
    }

    // 3. 实现 Process 方法，现在只需“发号施令”
    protected override void Process()
    {
        // 调用父类强大的、通用的 ProcessSheet 方法来处理 Robot 工作表
        // 它会自动处理表头、循环、创建资产、填充数据、注册缓存和保存
        ProcessSheet<CardData>(CardsExcelPath, "Robot", PawnsOutputPath, "HM");

        // 用同样的方法处理 Human 工作表
        ProcessSheet<CardData>(CardsExcelPath, "Human", PawnsOutputPath,"RB");
        
        
    }

    // 所有私有的 ProcessSheet, ProcessRobotRow, ProcessHumanRow 方法都已被删除！
}