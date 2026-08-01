using UnityEditor;
using UnityEngine;

/// <summary>
/// 测试用的进度开关，全部只影响编辑器，构建版本不受任何影响。
///
/// Auto Reset On Play 默认开启，编辑器里每次 Play 都从零开始。
/// 但它会连金币和关卡解锁一起清掉，所以测无尽模式和商店之前要先关掉它，
/// 再用下面的 Unlock All Levels / Add 1000 Coins 把测试环境搭起来。
/// </summary>
public static class ProgressMenu
{
    const string AutoResetMenu = "Tools/Bloom/Auto Reset On Play";

    /// <summary>
    /// 默认开启：每次进 Play 都清空全部进度，编辑器里永远是"第一次打开游戏"的状态。
    /// 只影响编辑器，构建版本里玩家的进度照常保存。
    /// 要测无尽模式或商店时先关掉，否则金币和解锁状态每次 Play 都会没。
    /// </summary>
    [MenuItem(AutoResetMenu)]
    static void ToggleAutoReset()
    {
        bool on = !EditorPrefs.GetBool(ProgressManager.AutoResetPrefKey, ProgressManager.AutoResetDefault);
        EditorPrefs.SetBool(ProgressManager.AutoResetPrefKey, on);
        Menu.SetChecked(AutoResetMenu, on);
        Debug.Log(on
            ? "[Bloom] 自动重置已开启：每次 Play 都会从零开始"
            : "[Bloom] 自动重置已关闭：进度会保留");
    }

    [MenuItem(AutoResetMenu, true)]
    static bool ToggleAutoResetValidate()
    {
        Menu.SetChecked(AutoResetMenu, EditorPrefs.GetBool(ProgressManager.AutoResetPrefKey, ProgressManager.AutoResetDefault));
        return true;
    }

    [MenuItem("Tools/Bloom/Unlock All Levels")]
    static void UnlockAll()
    {
        ProgressManager.UnlockAllLevels();
        Debug.Log("[Bloom] 十关全部解锁（无尽模式和商店本来就不需要解锁）");
    }

    [MenuItem("Tools/Bloom/Reset Level Progress")]
    static void ResetLevels()
    {
        ProgressManager.ResetLevelProgress();
        Debug.Log("[Bloom] 关卡进度已清空，回到只有第 1 关的状态");
    }

    [MenuItem("Tools/Bloom/Reset Endless Progress (coins, themes, best)")]
    static void ResetEndless()
    {
        ProgressManager.ResetEndlessProgress();
        Debug.Log("[Bloom] 金币、主题、最高轮次已清空");
    }

    [MenuItem("Tools/Bloom/Reset Everything")]
    static void ResetAll()
    {
        ProgressManager.ResetLevelProgress();
        ProgressManager.ResetEndlessProgress();
        Debug.Log("[Bloom] 全部进度已清空");
    }

    [MenuItem("Tools/Bloom/Add 1000 Coins")]
    static void AddCoins()
    {
        ProgressManager.AddCoins(1000);
        Debug.Log($"[Bloom] 金币 +1000，当前 {ProgressManager.Coins}");
    }

    // ---- 无尽模式流水。注意 Auto Reset On Play 不碰它，数据会跨 Play 一直攒 ----

    [MenuItem("Tools/Bloom/Run Log/Reveal RunLog.csv")]
    static void RevealRunLog()
    {
        if (!System.IO.File.Exists(RunLogger.FilePath))
        {
            Debug.Log("[Bloom] 还没有 RunLog.csv —— 在编辑器里打一局无尽模式就会生成");
            return;
        }
        EditorUtility.RevealInFinder(RunLogger.FilePath);
    }

    [MenuItem("Tools/Bloom/Run Log/Copy To Clipboard")]
    static void CopyRunLog()
    {
        EditorGUIUtility.systemCopyBuffer = RunLogger.Csv;
        Debug.Log($"[Bloom] 已复制 {RunLogger.RowCount} 轮流水到剪贴板");
    }

    /// <summary>
    /// 导出所有非 ASCII 字符，给字体子集化用。完整中文字体 10MB+，
    /// 对 WebGL 是灾难；只打包用到的这两百来个字，能压到几十 KB。
    /// </summary>
    [MenuItem("Tools/Bloom/Export Font Charset")]
    static void ExportCharset()
    {
        string chars = Loc.EditorAllChineseChars();
        string path = System.IO.Path.Combine(Application.dataPath, "..", "FontCharset.txt");
        System.IO.File.WriteAllText(path, chars, System.Text.Encoding.UTF8);
        Debug.Log($"[Bloom] 已导出 {chars.Length} 个字符到 FontCharset.txt\n{chars}");
        EditorUtility.RevealInFinder(path);
    }

    [MenuItem("Tools/Bloom/Run Log/Clear")]
    static void ClearRunLog()
    {
        if (!EditorUtility.DisplayDialog("清空无尽模式流水",
                $"当前存了 {RunLogger.RowCount} 轮的数据，清了就回不来了。\n" +
                "（项目根目录的 RunLog.csv 不受影响，要删得自己去删）",
                "清空", "算了"))
            return;

        RunLogger.Clear();
        Debug.Log("[Bloom] 流水已清空");
    }
}
