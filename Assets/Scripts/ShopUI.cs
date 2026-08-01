using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 皮肤商店。买过的主题在这里直接点 Equip 切换，没有背包那一层。
/// 主题只作用于无尽模式，正式关卡永远是作者配色。
///
/// 目前每张卡的预览图是用主题色现画的球，占位用。等美术出图之后换成真图。
/// </summary>
public class ShopUI : MonoBehaviour
{
    // ---- 布局旋钮。全部在 Inspector 可调，Play 模式下拖着改能实时看到效果 ----
    // 单位不是像素：整个 OnGUI 按屏幕高度归一化到 600，所以这些是"600 高画布上的坐标"

    [Header("边距")]
    [Tooltip("左边距。标题、卡片列表、Back 按钮共用这条左边线")]
    [SerializeField] float padLeft = 62f;
    [Tooltip("右边距。钱包、Current Theme 面板")]
    [SerializeField] float padRight = 30f;
    [Tooltip("卡片列表顶端，标题下方留多少")]
    [SerializeField] float gridTop = 78f;
    [Tooltip("卡片列表底端到屏幕底的留白（Back 按钮占的地方）")]
    [SerializeField] float gridBottom = 70f;

    [Header("卡片")]
    [SerializeField] int columns = 2;
    [SerializeField] float cardW = 300f;
    [SerializeField] float cardGap = 28f;

    [Header("右侧面板")]
    [SerializeField] float panelW = 260f;

    // 预览图是 16:9 —— 和游戏画面本身同比例，所以一张预览可以直接就是
    // 装备该主题跑一局无尽模式的截图。高度由宽度算出来，改宽度不用手动补高度
    float PreviewH => (cardW - 2f) * 9f / 16f;
    float PanelPreviewH => (panelW - 2f) * 9f / 16f;
    // ---- 卡片下半部分的纵向排布，全部相对预览图底边 ----
    const float SwatchSize = 18f;
    const float SwatchTop = 9f;     // 预览图底边 → 色板条
    const float RowTop = 46f;       // → 名字和按钮那一行（色板条和它之间有第二条分隔线）
    const float RowH = 34f;
    const float CardChromeH = 92f;  // 预览图下面这一整块的高度

    const float BtnW = 96f;         // 按钮宽
    const float BtnRight = 12f;     // 按钮右边距
    const float NameLeft = 12f;     // 名字左边距
    const float NameBtnGap = 10f;   // 名字和按钮之间至少留这么多

    /// <summary>主题名可用的宽度：整张卡减掉两侧边距、按钮、以及中间的间隙</summary>
    float NameWidth => cardW - NameLeft - BtnW - BtnRight - NameBtnGap;

    // 名字字号：在不挤到按钮的前提下取最大值。所有卡用同一个字号 ——
    // 每张卡各自算的话，短名字大、长名字小，网格看着就散了。
    // 只有语言或卡片宽度变了才需要重算。
    private int nameFontSize;
    private int fontFitVersion = -1;
    private float fontFitWidth = -1f;
    float CardH => PreviewH + CardChromeH;

    private GUIStyle titleStyle;
    private GUIStyle nameStyle;
    private GUIStyle chipStyle;
    private GUIStyle smallStyle;
    private GUIStyle buttonStyle;
    private int styleVersion = -1;

    private Vector2 scroll;

    static readonly Color Ink = new Color(0.15f, 0.15f, 0.15f);
    static readonly Color Muted = new Color(0.5f, 0.5f, 0.5f);
    static readonly Color Line = new Color(0.8f, 0.8f, 0.8f);

    void Awake()
    {
        var cam = Camera.main;
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.white;
    }

    void InitStyles()
    {
        if (styleVersion == Loc.Version) return;
        styleVersion = Loc.Version;

        titleStyle = Loc.Fit(new GUIStyle(GUI.skin.label)
        {
            fontSize = 34,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Ink }
        });
        nameStyle = Loc.Fit(new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = Ink }
        });
        chipStyle = Loc.Fit(new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        });
        smallStyle = Loc.Fit(new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            normal = { textColor = Muted }
        });
        buttonStyle = Loc.Fit(new GUIStyle(GUI.skin.button) { fontSize = 17 });

        // 上面的对象初始化器只设了 normal，其余状态还是内置 skin 的透明色。
        // 统一补一遍，免得哪个标签一碰鼠标就消失
        SetTextColor(titleStyle, Ink);
        SetTextColor(nameStyle, Ink);
        SetTextColor(smallStyle, Muted);
    }

    /// <summary>
    /// 找出"所有主题名都塞得下"的最大字号。逐档往下试，第一个全部通过的就是答案。
    /// 上限 32 是给短名字（糖霜、极光）封顶，再大就比按钮还抢眼了。
    /// </summary>
    void FitNameFont()
    {
        if (fontFitVersion == Loc.Version && Mathf.Approximately(fontFitWidth, NameWidth)) return;
        fontFitVersion = Loc.Version;
        fontFitWidth = NameWidth;

        for (nameFontSize = 32; nameFontSize > 11; nameFontSize--)
        {
            nameStyle.fontSize = nameFontSize;
            bool allFit = true;
            for (int i = 0; i < BallPalette.ThemeCount && allFit; i++)
                allFit = nameStyle.CalcSize(new GUIContent(BallPalette.ThemeName(i))).x <= NameWidth;
            if (allFit) break;
        }
        nameStyle.fontSize = nameFontSize;
    }

    void OnGUI()
    {
        InitStyles();
        FitNameFont();

        float scale = Screen.height / 600f;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));
        float sw = Screen.width / scale;
        float sh = Screen.height / scale;

        GUI.Label(new Rect(padLeft, 18, 400, 45), Loc.T("shop.title"), titleStyle);
        DrawWallet(new Rect(sw - padRight - 200, 22, 200, 30));

        float gridX = padLeft;
        float gridY = gridTop;
        float gridW = columns * cardW + (columns - 1) * cardGap;

        int rows = Mathf.CeilToInt(BallPalette.ThemeCount / (float)columns);
        float contentH = rows * (CardH + cardGap);
        var viewport = new Rect(gridX, gridY, gridW + 20f, sh - gridY - gridBottom);

        scroll = GUI.BeginScrollView(viewport, scroll,
            new Rect(0, 0, gridW, contentH));

        for (int i = 0; i < BallPalette.ThemeCount; i++)
        {
            float x = (i % columns) * (cardW + cardGap);
            float y = (i / columns) * (CardH + cardGap);
            DrawCard(new Rect(x, y, cardW, CardH), i);
        }

        GUI.EndScrollView();

        DrawCurrentPanel(new Rect(sw - padRight - panelW, gridY, panelW, 260));

        if (AudioManager.Button(new Rect(padLeft, sh - 60, 150, 42), Loc.T("shop.back"), buttonStyle))
            SceneManager.LoadScene("MainMenu");
    }

    void DrawWallet(Rect r)
    {
        var style = Loc.Fit(new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = Money.Ink }
        });
        Money.DrawRightAligned(r.xMax, r.y + 14, ProgressManager.Coins, style, 24f);
    }

    /// <summary>
    /// 三个状态要在余光里就能分开，不能靠读右下角那个词：
    ///   未解锁      → 预览图角上一枚绿色价签（钞票 + 数字）
    ///   已解锁未装备 → 什么都没有（没价签 = 已经是你的了，货架常识）
    ///   已装备      → 整张卡 2px 深色描边 + 一枚 ✓ 角标
    ///
    /// 刻意不给未解锁的卡蒙灰或加栅栏：这个商店卖的就是颜色，
    /// 把颜色关掉来表示"你还没买到颜色"，玩家就没法判断值不值这个价了。
    /// 信号放在卡片的框上，预览内容一点不动。
    /// </summary>
    void DrawCard(Rect r, int index)
    {
        bool owned = ProgressManager.IsThemeOwned(index);
        bool equipped = ProgressManager.EquippedTheme == index;
        int price = BallPalette.ThemePrices[index];

        // 所有卡统一 1px 浅边。已装备靠左上角那枚 ✓ 角标和按钮文案区分就够了 ——
        // 原来给它加 2px 深色描边，边框还会把色板条的两条分隔线一起压黑
        DrawBox(r, Line, 1);

        var preview = new Rect(r.x + 1, r.y + 1, r.width - 2, PreviewH);
        DrawThemePreview(preview, index);

        if (equipped)
            DrawChip(new Rect(preview.x + 8, preview.y + 8, 0, 0), "✓ " + Loc.T("shop.equipped"), Ink, Color.white);
        else if (!owned)
            DrawPriceChip(preview.xMax - 8, preview.y + 8, price);

        // 预览图和下半张卡之间的分隔线：预览是"这个主题长什么样"，
        // 下面是"它叫什么、多少钱" —— 两种信息，给一条线隔开
        GUI.color = Line;
        GUI.DrawTexture(new Rect(r.x + 1, preview.yMax, r.width - 2, 1), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // 色板条：按明度从浅到深排，读起来是一条色阶而不是一把散色。
        // 排的是副本 —— 数组本身的顺序不能动，蛇形关（6-10）靠 BallPalette.Ramp
        // 沿着这个顺序插值成蛇的身体，涩谷夜那套还特意把琥珀放在队首，
        // 免得渐变经过灰绿。展示顺序和取色顺序是两件事。
        var colors = BallPalette.ForTheme(index).OrderByDescending(Luminance).ToArray();
        float swatchY = preview.yMax + SwatchTop;
        float swatchGap = 6f;
        float totalW = colors.Length * SwatchSize + (colors.Length - 1) * swatchGap;
        float startX = r.x + (r.width - totalW) / 2f;
        for (int i = 0; i < colors.Length; i++)
            DrawCircle(new Rect(startX + i * (SwatchSize + swatchGap), swatchY, SwatchSize, SwatchSize), colors[i]);

        // 色板条下面再来一条线，把它夹成一个独立的带子 ——
        // 色板是"这套有哪些颜色"，和上面的预览、下面的名字都不是一回事
        GUI.color = Line;
        GUI.DrawTexture(new Rect(r.x + 1, swatchY + SwatchSize + SwatchTop, r.width - 2, 1), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // 名字和按钮同一行：名字靠左、按钮靠右
        float rowY = preview.yMax + RowTop;
        SetTextColor(nameStyle, owned ? Ink : Muted);   // 没买的退一档灰，多一层弱信号
        GUI.Label(new Rect(r.x + NameLeft, rowY, NameWidth, RowH),
                  BallPalette.ThemeName(index), nameStyle);

        var btnRect = new Rect(r.xMax - BtnRight - BtnW, rowY, BtnW, RowH);

        if (equipped)
        {
            GUI.enabled = false;
            AudioManager.Button(btnRect, Loc.T("shop.equipped"), buttonStyle);
            GUI.enabled = true;
        }
        else if (owned)
        {
            if (AudioManager.Button(btnRect, Loc.T("shop.equip"), buttonStyle))
            {
                ProgressManager.EquippedTheme = index;
                if (AudioManager.Instance != null) AudioManager.Instance.PlayEquip();
            }
        }
        else
        {
            // 买不起时按钮置灰。差额不另外写出来 —— 右上角的价签和右上角的钱包
            // 已经把两个数都摆在那儿了，中间再算一遍是重复
            GUI.enabled = ProgressManager.Coins >= price;
            if (AudioManager.Button(btnRect, Loc.T("shop.unlock"), buttonStyle))
            {
                if (ProgressManager.TrySpendCoins(price))
                {
                    ProgressManager.UnlockTheme(index);
                    ProgressManager.EquippedTheme = index;
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayPurchase();
                }
            }
            GUI.enabled = true;
        }
    }

    /// <summary>
    /// 压在预览图角上的小标签。宽度按文字算，rect 只用来定位：
    /// rightAligned = true 时 (x, y) 是右上角，否则是左上角。
    /// </summary>
    void DrawChip(Rect anchor, string text, Color bg, Color fg, bool rightAligned = false)
    {
        var content = new GUIContent(text);
        float w = chipStyle.CalcSize(content).x + 14;
        float h = 22;
        var chip = new Rect(rightAligned ? anchor.x - w : anchor.x, anchor.y, w, h);

        GUI.color = bg;
        GUI.DrawTexture(chip, Texture2D.whiteTexture);
        GUI.color = Color.white;

        chipStyle.normal.textColor = fg;
        GUI.Label(chip, content, chipStyle);
    }

    /// <summary>价签：近白底 + 深藏青数字，右上角右对齐。绿色只由钞票图标提供</summary>
    void DrawPriceChip(float right, float top, int price)
    {
        chipStyle.normal.textColor = Money.Ink;
        float iconH = 15f;
        float w = Money.Width(price, chipStyle, iconH) + 16f;
        float h = 24f;
        var chip = new Rect(right - w, top, w, h);

        GUI.color = Money.Chip;
        GUI.DrawTexture(chip, Texture2D.whiteTexture);
        GUI.color = Color.white;
        DrawBox(chip, new Color(0f, 0f, 0f, 0.12f));   // 一圈极淡的边，压在浅色预览图上也不会糊掉

        Money.Draw(chip.x + 8f, chip.center.y, price, chipStyle, iconH);
    }

    /// <summary>
    /// 预览图：用主题自己的配色和背景，在框里随机摆一把球 —— 就是这个主题
    /// 在游戏里的样子。刻意不用外部图片：全项目的图形都是代码生成的，
    /// 而且这样加新主题就是加一行配色，不用再配一张图。
    ///
    /// 随机种子由 index 决定，所以每个主题的球位固定不变，不会每帧乱跳。
    /// </summary>
    void DrawThemePreview(Rect r, int index)
    {
        var colors = BallPalette.ForTheme(index);

        // 占位图也要用主题自己的背景色，否则深色主题在商店里看着还是白的，
        // 买回去才发现是黑底 —— 预览的意义就没了
        var bg = BallPalette.BackgroundForTheme(index);
        GUI.color = BallPalette.IsDarkTheme(index) ? bg : new Color(0.97f, 0.97f, 0.97f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;

        int count = 14;
        var rng = new System.Random(index * 7919 + 13);
        for (int i = 0; i < count; i++)
        {
            float size = 16f + (float)rng.NextDouble() * 12f;
            float x = r.x + 8 + (float)rng.NextDouble() * (r.width - 16 - size);
            float y = r.y + 8 + (float)rng.NextDouble() * (r.height - 16 - size);
            DrawCircle(new Rect(x, y, size, size), colors[i % colors.Length]);
        }

    }

    void DrawCurrentPanel(Rect r)
    {
        GUI.Label(new Rect(r.x, r.y - 4, r.width, 34),
            Loc.T("shop.current"), Loc.Fit(new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                normal = { textColor = Ink }
            }));

        var box = new Rect(r.x, r.y + 34, r.width, PanelPreviewH + 42);
        DrawBox(box, Line);
        DrawThemePreview(new Rect(box.x + 1, box.y + 1, box.width - 2, PanelPreviewH), ProgressManager.EquippedTheme);

        // DrawCard 会按"买没买"改 nameStyle 的颜色，这里必须自己设回来
        SetTextColor(nameStyle, Ink);
        GUI.Label(new Rect(box.x + 12, box.yMax - 34, box.width - 24, 26),
            BallPalette.ThemeName(ProgressManager.EquippedTheme), nameStyle);

        GUI.Label(new Rect(r.x, box.yMax + 10, r.width, 40),
            Loc.T("shop.note"), smallStyle);
    }

    static void DrawBox(Rect r, Color border, float t = 1f)
    {
        GUI.color = border;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.x, r.yMax - t, r.width, t), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, r.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    /// <summary>
    /// 给一个 GUIStyle 的**所有状态**设同一个文字色。
    ///
    /// 必须全设：GUIStyle 的 normal / hover / active / focused 是各自独立的，
    /// 而内置 skin 里除 normal 之外几乎都是透明的。只设 normal 的话，
    /// IMGUI 一旦走到别的状态（鼠标悬停在控件上就会），文字直接凭空消失。
    /// 2026-08-02 踩过：商店卡片的主题名一碰鼠标就没了。
    /// </summary>
    static void SetTextColor(GUIStyle s, Color c)
    {
        s.normal.textColor = c;
        s.hover.textColor = c;
        s.active.textColor = c;
        s.focused.textColor = c;
        s.onNormal.textColor = c;
        s.onHover.textColor = c;
        s.onActive.textColor = c;
        s.onFocused.textColor = c;
    }

    /// <summary>感知亮度，只用来给色板条排序</summary>
    static float Luminance(Color c) => c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;

    static void DrawCircle(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, SpriteHelper.Circle.texture);
        GUI.color = Color.white;
    }
}
