using UnityEngine;

public static class ProgressManager
{
    private const string Key = "UnlockedLevel";
    private const string BestRoundKey = "EndlessBestRound";
    // 存档 key 保持旧名 "Pollen"：货币改名叫金币只是显示层的事，
    // 换 key 会让已有存档里的余额凭空消失
    private const string CoinsKey = "Pollen";
    private const string ThemeOwnedKey = "ThemeOwned_";
    private const string ThemeEquippedKey = "ThemeEquipped";

    private const int StoryLevelCount = 10;

    public static int UnlockedLevel
    {
        get => PlayerPrefs.GetInt(Key, 1);
        set
        {
            PlayerPrefs.SetInt(Key, value);
            PlayerPrefs.Save();
        }
    }

    public static void CompleteLevel(int globalLevel)
    {
        if (globalLevel >= UnlockedLevel)
            UnlockedLevel = globalLevel + 1;
    }

    /// <summary>通关十关之后才开放无尽模式和商店</summary>
    public static bool EndlessUnlocked => UnlockedLevel > StoryLevelCount;

    /// <summary>历史最好成绩：通过的最高轮次（不是死在第几轮）</summary>
    public static int BestRound
    {
        get => PlayerPrefs.GetInt(BestRoundKey, 0);
        private set
        {
            PlayerPrefs.SetInt(BestRoundKey, value);
            PlayerPrefs.Save();
        }
    }

    public static void ReportClearedRound(int round)
    {
        if (round > BestRound)
            BestRound = round;
    }

    public static int Coins
    {
        get => PlayerPrefs.GetInt(CoinsKey, 0);
        private set
        {
            PlayerPrefs.SetInt(CoinsKey, value);
            PlayerPrefs.Save();
        }
    }

    public static void AddCoins(int amount)
    {
        if (amount > 0) Coins += amount;
    }

    public static bool TrySpendCoins(int amount)
    {
        if (Coins < amount) return false;
        Coins -= amount;
        return true;
    }

    /// <summary>主题只作用于无尽模式，正式关卡锁死作者配色。0 号是免费初始主题</summary>
    public static bool IsThemeOwned(int index)
    {
        return index == 0 || PlayerPrefs.GetInt(ThemeOwnedKey + index, 0) == 1;
    }

    public static void UnlockTheme(int index)
    {
        PlayerPrefs.SetInt(ThemeOwnedKey + index, 1);
        PlayerPrefs.Save();
    }

    public static int EquippedTheme
    {
        get
        {
            int t = PlayerPrefs.GetInt(ThemeEquippedKey, 0);
            return IsThemeOwned(t) ? t : 0;
        }
        set
        {
            if (!IsThemeOwned(value)) return;
            PlayerPrefs.SetInt(ThemeEquippedKey, value);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// "每次 Play 自动重置全部进度"的开关，**默认开启**，存在 EditorPrefs 里。
    ///
    /// 整段被 #if UNITY_EDITOR 包着，**不会编译进任何构建版本** ——
    /// itch.io 上的玩家进度照常保存，只有编辑器里每次 Play 从零开始。
    ///
    /// 要测无尽模式或商店时记得先关掉（Tools → Bloom → Auto Reset On Play），
    /// 否则金币和解锁状态每次 Play 都会被清空。
    /// </summary>
    public const string AutoResetPrefKey = "Bloom.AutoResetOnPlay";
    public const bool AutoResetDefault = true;

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoResetOnPlay()
    {
        if (!UnityEditor.EditorPrefs.GetBool(AutoResetPrefKey, AutoResetDefault)) return;

        ResetLevelProgress();
        ResetEndlessProgress();
        Debug.Log("[Bloom] 已自动重置全部进度（Tools → Bloom → Auto Reset On Play 可关掉）");
    }
#endif

    public static void ResetLevelProgress()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }

    public static void ResetEndlessProgress()
    {
        PlayerPrefs.DeleteKey(BestRoundKey);
        PlayerPrefs.DeleteKey(CoinsKey);
        PlayerPrefs.DeleteKey(ThemeEquippedKey);
        for (int i = 0; i < BallPalette.ThemeCount; i++)
            PlayerPrefs.DeleteKey(ThemeOwnedKey + i);
        PlayerPrefs.Save();
    }

    public static void UnlockAllLevels()
    {
        UnlockedLevel = StoryLevelCount + 1;
    }
}
