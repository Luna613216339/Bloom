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
    const float CardChromeH = 102f;              // 预览图下面的色板 + 名字 + 按钮
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
            fontSize = 20,
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
    }

    void OnGUI()
    {
        InitStyles();

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

        DrawBox(r, equipped ? Ink : Line, equipped ? 2 : 1);

        var preview = new Rect(r.x + 1, r.y + 1, r.width - 2, PreviewH);
        DrawThemePreview(preview, index);

        if (equipped)
            DrawChip(new Rect(preview.x + 8, preview.y + 8, 0, 0), "✓ " + Loc.T("shop.equipped"), Ink, Color.white);
        else if (!owned)
            DrawPriceChip(preview.xMax - 8, preview.y + 8, price);

        // 色板条
        var colors = BallPalette.ForTheme(index);
        float swatchY = preview.yMax + 8;
        float swatchSize = 18f;
        float swatchGap = 6f;
        float totalW = colors.Length * swatchSize + (colors.Length - 1) * swatchGap;
        float startX = r.x + (r.width - totalW) / 2f;
        for (int i = 0; i < colors.Length; i++)
            DrawCircle(new Rect(startX + i * (swatchSize + swatchGap), swatchY, swatchSize, swatchSize), colors[i]);

        float textY = swatchY + swatchSize + 10;
        // 没买的主题连名字都退一档，让"这张还不是你的"多一层弱信号
        nameStyle.normal.textColor = owned ? Ink : Muted;
        GUI.Label(new Rect(r.x + 12, textY, r.width - 24, 26), BallPalette.ThemeName(index), nameStyle);

        var btnRect = new Rect(r.x + r.width - 108, textY + 28, 96, 34);

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
            // 买不起的时候把差额说出来，比一个灰按钮有用 —— 玩家知道还差多少才会去打
            int gap = price - ProgressManager.Coins;
            if (gap > 0)
                GUI.Label(new Rect(r.x + 12, textY + 32, r.width - 120, 22),
                          Loc.F("shop.short", gap), smallStyle);

            GUI.enabled = gap <= 0;
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

    /// <summary>价签：钞票浅绿底 + 深藏青数字，右上角右对齐</summary>
    void DrawPriceChip(float right, float top, int price)
    {
        chipStyle.normal.textColor = Money.Ink;
        float iconH = 15f;
        float w = Money.Width(price, chipStyle, iconH) + 14f;
        float h = 24f;
        var chip = new Rect(right - w, top, w, h);

        GUI.color = Money.Chip;
        GUI.DrawTexture(chip, Texture2D.whiteTexture);
        GUI.color = Color.white;

        Money.Draw(chip.x + 7f, chip.center.y, price, chipStyle, iconH);
    }

    // Resources.Load 每帧调一次太浪费，查过一次就记住结果（包括"没这张图"）
    static Texture2D[] previewCache;
    static bool[] previewProbed;

    static Texture2D PreviewTexture(int index)
    {
        if (previewCache == null)
        {
            previewCache = new Texture2D[BallPalette.ThemeCount];
            previewProbed = new bool[BallPalette.ThemeCount];
        }
        if (!previewProbed[index])
        {
            previewProbed[index] = true;
            previewCache[index] = Resources.Load<Texture2D>("ThemePreview/" + BallPalette.ThemeIds[index]);
        }
        return previewCache[index];
    }

    /// <summary>
    /// 有图就画图，没图就退回占位（主题色摆一排球）。
    /// 这样可以一张一张往 Resources/ThemePreview 里放，不用等七张齐了才能跑。
    /// </summary>
    void DrawThemePreview(Rect r, int index)
    {
        var tex = PreviewTexture(index);
        if (tex != null)
        {
            GUI.DrawTexture(r, tex, ScaleMode.ScaleAndCrop);
            return;
        }

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

        var tagStyle = Loc.Fit(new GUIStyle(smallStyle));
        tagStyle.normal.textColor = BallPalette.MutedInkFor(BallPalette.IsDarkTheme(index));
        GUI.Label(new Rect(r.x + 8, r.yMax - 24, r.width - 16, 20), Loc.T("shop.placeholder"), tagStyle);
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
        nameStyle.normal.textColor = Ink;
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

    static void DrawCircle(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, SpriteHelper.Circle.texture);
        GUI.color = Color.white;
    }
}
