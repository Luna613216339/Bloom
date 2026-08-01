using System.Collections.Generic;
using UnityEngine;

public enum Lang { EN = 0, ZH = 1 }

/// <summary>
/// 界面文案的唯一来源。用法：<c>Loc.T("shop.equip")</c>，带参数的用 <c>Loc.F("game.coins", n)</c>。
///
/// 语言存 PlayerPrefs，切换立刻生效（OnGUI 每帧重新取字符串，不需要重建样式）。
///
/// ⚠️ 中文需要字体：Unity 内置的 OnGUI 字体不含汉字，缺字体时中文会渲染成空白。
///    见 Assets/Resources/Fonts/README.md。英文不受影响。
/// </summary>
public static class Loc
{
    const string Key = "Lang";

    static Lang? cached;

    public static Lang Current
    {
        get
        {
            if (cached == null) cached = (Lang)PlayerPrefs.GetInt(Key, (int)Lang.EN);
            return cached.Value;
        }
        set
        {
            cached = value;
            PlayerPrefs.SetInt(Key, (int)value);
            PlayerPrefs.Save();
        }
    }

    public static bool IsZh => Current == Lang.ZH;

    /// <summary>语言每切一次 +1。各 UI 用它判断缓存的 GUIStyle 该不该重建</summary>
    public static int Version { get; private set; }

    public static void Toggle()
    {
        Current = IsZh ? Lang.EN : Lang.ZH;
        Version++;
    }

    /// <summary>语言按钮上显示的是"要切过去的那个"，不是当前的 —— 按钮说的是它会做什么</summary>
    public static string SwitchLabel => IsZh ? "English" : "中文";

    public static string T(string key)
    {
        if (!Table.TryGetValue(key, out var pair)) return key;   // 缺词条就把 key 显出来，好排查
        return pair[(int)Current];
    }

    public static string F(string key, params object[] args) => string.Format(T(key), args);

    // ---- 字体 ----

    static Font font;
    static bool fontProbed;

    /// <summary>中文字体。放 Assets/Resources/Fonts/UIFont.ttf 即生效，没有就返回 null</summary>
    public static Font CjkFont
    {
        get
        {
            if (!fontProbed)
            {
                fontProbed = true;
                font = Resources.Load<Font>("Fonts/UIFont");
            }
            return font;
        }
    }

    public static bool CjkReady => CjkFont != null;

    /// <summary>
    /// 当前该用的字体。英文返回 null —— GUIStyle.font = null 表示"用默认字体"，
    /// 英文界面因此保持内置字体原样。
    /// </summary>
    public static Font UIFont => IsZh ? CjkFont : null;

    /// <summary>
    /// 给一个 GUIStyle 套上当前字体。
    ///
    /// ⚠️ 千万别改成 <c>GUI.skin.font = ...</c>。在编辑器里 <c>GUI.skin</c> 返回的是内置 skin，
    /// 而那个对象是<b>和编辑器自身的 IMGUI 共享的</b>。写它会连编辑器的字体图集一起改掉，
    /// 表现是所有 GUI 文字变成 "Gizmos" 之类的乱码 + 颜色错乱，而且退出 Play 也不恢复。
    /// </summary>
    public static GUIStyle Fit(GUIStyle s)
    {
        s.font = UIFont;
        return s;
    }

    // ---- 词条表。[0] = English，[1] = 中文 ----

    static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
    {
        // 主菜单
        { "menu.author",     new[]{ "Author: Jie Li",  "作者：李洁" } },
        { "menu.endless",    new[]{ "Endless",         "无尽模式" } },
        { "menu.shop",       new[]{ "Shop",            "商店" } },
        { "menu.stats",      new[]{ "Best: Round {0}", "最高记录：第 {0} 关" } },
        { "menu.nofont",     new[]{ "Chinese needs a font — see Resources/Fonts/README",
                                    "Chinese needs a font — see Resources/Fonts/README" } },

        // 游戏内
        { "game.level",      new[]{ "Level {0}",       "第 {0} 关" } },
        { "game.round",      new[]{ "Endless · Round {0}", "无尽 · 第 {0} 关" } },
        { "game.congrats",   new[]{ "Congratulations!", "全部通关！" } },
        { "game.passed",     new[]{ "Passed!  {0} / {1}",   "过关！  {0} / {1}" } },
        { "game.tryagain",   new[]{ "Try Again  {0} / {1}", "再试一次  {0} / {1}" } },
        { "game.replay",     new[]{ "Replay",          "重玩" } },
        { "game.menu",       new[]{ "Menu",            "主菜单" } },
        { "game.next",       new[]{ "Next",            "下一关" } },

        // 无尽模式结算
        { "run.over",        new[]{ "Game Over",       "游戏结束" } },
        { "run.cleared",     new[]{ "This run {0}     Best {1}", "本局 {0} 关     历史最高 {1} 关" } },
        { "run.coins",       new[]{ "Coins +{0}",      "金币 +{0}" } },
        { "run.new",         new[]{ "New Run",         "重新开始" } },

        // 商店
        { "shop.title",      new[]{ "Skin Shop",       "皮肤商店" } },
        { "shop.back",       new[]{ "Back",            "返回" } },
        { "shop.equipped",   new[]{ "Equipped",        "使用中" } },
        { "shop.equip",      new[]{ "Equip",           "使用" } },
        { "shop.unlock",     new[]{ "Unlock",          "解锁" } },
        { "shop.short",      new[]{ "◆ {0} more",      "还差 {0}" } },
        { "shop.current",    new[]{ "Current Theme",   "当前主题" } },
        { "shop.note",       new[]{ "Skins apply to Endless only.", "皮肤效果只作用于无尽模式" } },
        { "shop.placeholder",new[]{ "preview placeholder", "预览图待补" } },
    };

#if UNITY_EDITOR
    /// <summary>子集化字体时要用的字符集：把所有中文词条和主题名里出现过的字去重</summary>
    public static string EditorAllChineseChars()
    {
        var set = new SortedSet<char>();
        void Eat(string s)
        {
            if (string.IsNullOrEmpty(s)) return;
            foreach (var ch in s)
                if (ch > 0x2000) set.Add(ch);   // 只收非 ASCII（汉字、全角标点、◆✓ 这类符号）
        }
        foreach (var pair in Table.Values) Eat(pair[(int)Lang.ZH]);
        foreach (var pair in Table.Values) Eat(pair[(int)Lang.EN]);
        foreach (var n in BallPalette.ThemeNamesZh) Eat(n);
        foreach (var n in BallPalette.ThemeNames) Eat(n);
        return new string(new List<char>(set).ToArray());
    }
#endif
}
