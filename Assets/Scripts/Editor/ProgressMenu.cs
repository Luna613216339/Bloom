using UnityEditor;
using UnityEngine;

/// <summary>
/// 测试用的进度开关。以前是每次进 Play 自动清空关卡进度，但那样没法直接开
/// Endless 场景测试（解锁状态会被一起清掉），所以改成在这里手动触发。
/// </summary>
public static class ProgressMenu
{
    [MenuItem("Tools/Bloom/Unlock All Levels")]
    static void UnlockAll()
    {
        ProgressManager.UnlockAllLevels();
        Debug.Log("[Bloom] 十关全部解锁，无尽模式和商店可用");
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
}
