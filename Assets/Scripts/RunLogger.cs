using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 无尽模式的每轮流水。
///
/// 存在的理由：难度模拟器还没用真实数据校准，而校准需要的是每轮的
/// 「打中了几颗 vs 目标几颗」—— 金币总额倒推不出这个数（金球、超额、里程碑混在一起）。
/// 靠人手抄凑不出样本量，所以让游戏自己记。
///
/// 每轮结束就落盘，不等整局结束 —— 中途关掉游戏也不会丢数据。
/// 编辑器里写项目根目录的 RunLog.csv，构建版写 PlayerPrefs（结算页有 Copy Log 按钮）。
/// </summary>
public static class RunLogger
{
    const string Key = "RunLog";

    /// <summary>一行约 45 字节，2000 行 ≈ 90KB，够两百局。超了从最老的开始丢</summary>
    const int MaxStoredRows = 2000;

    public const string Header = "run,round,balls,target,triggered,gold,passed,coins";

    static string runId;

    /// <summary>一整局开始。runId 用来在 CSV 里把同一局的行归到一起</summary>
    public static void BeginRun()
    {
        runId = DateTime.Now.ToString("MMdd-HHmmss");
    }

    /// <param name="triggered">这轮实际打中几颗（含金球）</param>
    /// <param name="gold">这轮吃到几颗金球</param>
    /// <param name="coins">整局累计金币（不是这一轮的）</param>
    public static void RecordRound(int round, int balls, int target,
                                   int triggered, int gold, bool passed, int coins)
    {
        if (string.IsNullOrEmpty(runId)) BeginRun();

        string row = string.Join(",", runId, round, balls, target,
                                 triggered, gold, passed ? 1 : 0, coins);

        AppendToPrefs(row);
#if UNITY_EDITOR
        AppendToFile(row);
#endif
    }

    static void AppendToPrefs(string row)
    {
        var rows = new List<string>(StoredRows());
        rows.Add(row);
        if (rows.Count > MaxStoredRows)
            rows.RemoveRange(0, rows.Count - MaxStoredRows);

        PlayerPrefs.SetString(Key, string.Join("\n", rows));
        PlayerPrefs.Save();
    }

    static string[] StoredRows()
    {
        string raw = PlayerPrefs.GetString(Key, "");
        if (string.IsNullOrEmpty(raw)) return Array.Empty<string>();
        return raw.Split('\n');
    }

    /// <summary>带表头的完整 CSV，直接丢进 Excel 就能画图</summary>
    public static string Csv
    {
        get
        {
            var rows = StoredRows();
            return rows.Length == 0 ? Header : Header + "\n" + string.Join("\n", rows);
        }
    }

    public static int RowCount => StoredRows().Length;

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }

#if UNITY_EDITOR
    /// <summary>项目根目录（Assets 的上一层），和 BalanceReport.csv 放一起</summary>
    public static string FilePath =>
        System.IO.Path.Combine(Application.dataPath, "..", "RunLog.csv");

    static void AppendToFile(string row)
    {
        try
        {
            if (!System.IO.File.Exists(FilePath))
                System.IO.File.WriteAllText(FilePath, Header + "\n");
            System.IO.File.AppendAllText(FilePath, row + "\n");
        }
        catch (Exception e)
        {
            // 写不进去不该把游戏搞崩，记一条就算了
            Debug.LogWarning($"RunLogger 写文件失败：{e.Message}");
        }
    }
#endif
}
