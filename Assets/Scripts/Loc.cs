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

    /// <summary>
    /// 汉字之间插一个空格，把字距撑开。中文两三个字挤在按钮正中间显得很局促，
    /// 拉开之后才像个标签而不是一坨。
    ///
    /// 只在相邻的两个汉字之间插 —— 所以英文原样返回，标点前后也不动。
    /// 用普通空格而不是更细的 U+2009：那个字要单独进字体子集，
    /// 而普通空格本来就在 ASCII 里，不会有缺字风险。
    /// </summary>
    public static string Spaced(string t)
    {
        if (string.IsNullOrEmpty(t)) return t;
        var sb = new System.Text.StringBuilder(t.Length * 2);
        for (int i = 0; i < t.Length; i++)
        {
            if (i > 0 && IsHan(t[i - 1]) && IsHan(t[i])) sb.Append(' ');
            sb.Append(t[i]);
        }
        return sb.ToString();
    }

    static bool IsHan(char c) => c >= 0x4E00 && c <= 0x9FFF;

    /// <summary>
    /// "N 关" / "N level(s)"。单独拎出来是因为英文有单复数：
    /// 写死 "levels" 会出现 "1 levels"，而这一行里有两个各自独立的数字，
    /// 没法在一条格式串里判断。中文没这个问题，量词恒定。
    /// </summary>
    public static string Levels(int n)
    {
        if (IsZh) return $"{n} 关";
        return n == 1 ? "1 level" : $"{n} levels";
    }

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

    // ---- 测试菜单的字体 ----
    // 测试阶段的主菜单要显示每关一行中文说明，这些字不在子集字体里。
    // 试过 Font.CreateDynamicFontFromOSFont 借系统字体：IMGUI 里字形全错（每行都画成别的字符串），
    // 不可用。改用 Unity 内置的动态字体（font = null）：它在编辑器和 macOS 包里遇到缺字会自动
    // 退到系统字体（开发笔记坑 24 就是这么发现的），中文照常显示；WebGL 里没有这条退路，
    // 但测试菜单本来也不打网页版。正式关卡名要进游戏时仍然要重做子集字体（先问用户下载 Noto Sans SC）。

    /// <summary>测试菜单的样式：中英文都用内置动态字体，靠系统字体回退显示汉字</summary>
    public static GUIStyle FitTestMenu(GUIStyle s)
    {
        Fit(s);
        s.font = null;
        return s;
    }

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
    /// <summary>
    /// 语言按钮专用：**永远**用中文字体。
    ///
    /// 这个按钮的文案是"要切过去的那个语言"，所以英文界面下它显示的是「中文」——
    /// 两个汉字。而 UIFont 在英文下返回 null（内置字体），内置字体没有汉字，
    /// 构建版里就是一片空白。
    ///
    /// 编辑器里看不出来：Unity 在编辑器里会退回系统字体，而 WebGL 里没有系统字体可退。
    /// </summary>
    public static GUIStyle FitAlwaysCjk(GUIStyle s)
    {
        Fit(s);
        if (CjkFont != null) s.font = CjkFont;
        return s;
    }

    public static GUIStyle Fit(GUIStyle s)
    {
        s.font = UIFont;
        FillTransparentStates(s);
        return s;
    }

    /// <summary>
    /// 把 normal 的文字色**无条件**复制到其余七个状态。
    ///
    /// GUIStyle 的 normal / hover / active / focused 各自独立，只设 normal 的话
    /// IMGUI 走到别的状态时会拿到 skin 自带的颜色 —— 在这个项目里表现为
    /// 鼠标一悬停文字就消失。
    ///
    /// ⚠️ 曾经写成"只补 alpha==0 的状态"，结果没生效：内置 skin 那些状态
    /// 并不是透明的，条件根本不成立。判断状态"有没有被设过"这件事做不到，
    /// 所以只能无条件覆盖。
    ///
    /// 按钮除外 —— 按钮的悬停/按下变色是内置 skin 有意做的，覆盖了就没有反馈了。
    /// 靠 name 区分：GUIStyle 的拷贝构造会带上原样式的名字，
    /// 从 GUI.skin.button 复制出来的 name 就是 "button"。
    /// </summary>
    public static void SetTextColor(GUIStyle s, Color c)
    {
        s.normal.textColor = c;
        FillTransparentStates(s);
    }

    static void FillTransparentStates(GUIStyle s)
    {
        if (s.name == "button") return;

        var c = s.normal.textColor;
        s.hover.textColor = c;
        s.active.textColor = c;
        s.focused.textColor = c;
        s.onNormal.textColor = c;
        s.onHover.textColor = c;
        s.onActive.textColor = c;
        s.onFocused.textColor = c;
    }

    // ---- 词条表。[0] = English，[1] = 中文 ----

    static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
    {
        // 主菜单
        { "menu.author",     new[]{ "Author: Jie Li",  "作者：李洁" } },
        { "menu.endless",    new[]{ "Endless",         "无尽模式" } },
        { "menu.shop",       new[]{ "Shop",            "商店" } },
        { "menu.stats",      new[]{ "Best: Level {0}", "最高记录：第 {0} 关" } },
        { "menu.nofont",     new[]{ "Chinese needs a font — see Resources/Fonts/README",
                                    "Chinese needs a font — see Resources/Fonts/README" } },

        // 游戏内
        { "game.level",      new[]{ "Level {0}",       "第 {0} 关" } },
        { "game.levelNamed", new[]{ "Level {0} · {1}", "第 {0} 关 · {1}" } },
        { "game.round",      new[]{ "Endless · Level {0}", "无尽 · 第 {0} 关" } },
        { "game.congrats",   new[]{ "Congratulations!", "全部通关！" } },
        { "game.passed",     new[]{ "Passed!  {0} / {1}",   "过关！  {0} / {1}" } },
        { "game.tryagain",   new[]{ "Try Again  {0} / {1}", "再试一次  {0} / {1}" } },
        { "game.replay",     new[]{ "Replay",          "重玩" } },
        { "game.menu",       new[]{ "Menu",            "主菜单" } },
        { "game.next",       new[]{ "Next",            "下一关" } },
        // 续命：没过关时花钱再点一下。中文只能用子集字体里已有的字（见 FontCharset.txt）
        { "game.more",       new[]{ "One More Click",  "再打一下" } },

        // 无尽模式结算
        { "run.over",        new[]{ "Game Over",       "游戏结束" } },
        { "run.cleared",     new[]{ "This run {0}     Best {1}", "本局 {0}     历史最高 {1}" } },
        { "run.new",         new[]{ "New Run",         "重新开始" } },

        // 商店
        { "shop.title",      new[]{ "Skin Shop",       "皮肤商店" } },
        { "shop.back",       new[]{ "Back",            "返回" } },
        { "shop.equipped",   new[]{ "Equipped",        "使用中" } },
        { "shop.equip",      new[]{ "Equip",           "使用" } },
        { "shop.unlock",     new[]{ "Unlock",          "解锁" } },
        { "shop.current",    new[]{ "Current Theme",   "当前主题" } },
        { "shop.note",       new[]{ "Skins apply to Endless only.", "皮肤效果只作用于无尽模式" } },
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
        foreach (var d in LevelCatalog.All) { Eat(d.NameZh); Eat(d.NameEn); }
        return new string(new List<char>(set).ToArray());
    }
#endif
}
