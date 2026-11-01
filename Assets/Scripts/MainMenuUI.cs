using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 测试阶段的主菜单：关卡是一个竖向滚动列表，每行 关号 / 名字 / 一行中文特色 / 属于哪个池子。
/// 关卡越来越多，格子装不下，而且用户要边玩边记感受，得看见每关是干什么的。
/// 这不是最终形态。说明文字只有中文，用的是系统字体（见 Loc.TestMenuFont）。
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    private GUIStyle titleStyle;
    private GUIStyle playBtnStyle;
    private GUIStyle langBtnStyle;
    private GUIStyle authorStyle;
    private GUIStyle hintStyle;
    private GUIStyle rowBtnStyle;
    private GUIStyle numStyle;
    private GUIStyle nameStyle;
    private GUIStyle noteStyle;
    private GUIStyle tagStyle;
    private GUIStyle sectionStyle;
    private int styleVersion = -1;
    private Vector2 scroll;

    const float RowH = 34f;
    const float SectionH = 30f;
    const float ListW = 820f;

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
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.15f, 0.15f, 0.15f) }
        });

        playBtnStyle = Loc.Fit(new GUIStyle(GUI.skin.button) { fontSize = 20 });
        // 语言按钮永远用中文字体：它的文案是"要切过去的语言"，
        // 英文界面下显示的是「中文」两个汉字，内置字体渲染不出来
        langBtnStyle = Loc.FitAlwaysCjk(new GUIStyle(GUI.skin.button) { fontSize = 17 });

        authorStyle = Loc.Fit(new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.45f, 0.45f, 0.45f) }
        });
        hintStyle = Loc.Fit(new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
        });

        // 行本身不用内置按钮（深灰底压得说明文字看不清），自己画浅底，按钮只负责接点击
        rowBtnStyle = new GUIStyle(GUIStyle.none);
        numStyle = Loc.FitTestMenu(new GUIStyle(GUI.skin.label)
        {
            fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight,
            normal = { textColor = new Color(0.2f, 0.2f, 0.2f) }
        });
        nameStyle = Loc.FitTestMenu(new GUIStyle(GUI.skin.label)
        {
            fontSize = 17, alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.15f, 0.15f, 0.15f) }
        });
        noteStyle = Loc.FitTestMenu(new GUIStyle(GUI.skin.label)
        {
            fontSize = 14, alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip,
            normal = { textColor = new Color(0.42f, 0.42f, 0.42f) }
        });
        tagStyle = Loc.FitTestMenu(new GUIStyle(GUI.skin.label)
        {
            fontSize = 13, alignment = TextAnchor.MiddleRight,
            normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
        });
        sectionStyle = Loc.FitTestMenu(new GUIStyle(GUI.skin.label)
        {
            fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.LowerLeft,
            normal = { textColor = new Color(0.35f, 0.35f, 0.35f) }
        });
    }

    void OnGUI()
    {
        InitStyles();

        float scale = Screen.height / 600f;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));

        float sw = Screen.width / scale;
        float sh = Screen.height / scale;

        GUI.Label(new Rect(0, 14, sw, 44), "Bloom", titleStyle);
        GUI.Label(new Rect(0, 52, sw, 24), Loc.T("menu.author"), authorStyle);

        float listTop = 86f;
        float listBottom = sh - 78f;
        DrawLevelList(new Rect((sw - ListW) / 2f, listTop, ListW, listBottom - listTop));

        DrawBottomBar(sw, sh);
    }

    /// <summary>关卡列表。段落按 LevelDef.Group 分，连续相同的归一段</summary>
    void DrawLevelList(Rect area)
    {
        int unlocked = ProgressManager.UnlockedLevel;
        int count = LevelCatalog.Count;

        // 先量高度：每段一个标题 + 每关一行
        float contentH = 8f;
        string lastGroup = null;
        for (int i = 1; i <= count; i++)
        {
            var d = LevelCatalog.Get(i);
            if (d.Group != lastGroup) { contentH += SectionH; lastGroup = d.Group; }
            contentH += RowH;
        }

        // 列表底色，让滚动区域有个边界
        GUI.color = new Color(0.94f, 0.94f, 0.94f);
        GUI.DrawTexture(area, Texture2D.whiteTexture);
        GUI.color = Color.white;

        float innerW = area.width - 20f;
        scroll = GUI.BeginScrollView(area, scroll, new Rect(0, 0, innerW, contentH), false, true);

        float y = 4f;
        lastGroup = null;
        for (int i = 1; i <= count; i++)
        {
            var d = LevelCatalog.Get(i);
            if (d.Group != lastGroup)
            {
                lastGroup = d.Group;
                GUI.Label(new Rect(12, y, innerW - 24, SectionH - 4), d.Group, sectionStyle);
                y += SectionH;
            }

            var row = new Rect(8, y + 2, innerW - 16, RowH - 4);
            bool playable = i <= unlocked;
            bool hover = playable && row.Contains(Event.current.mousePosition);
            GUI.color = hover ? new Color(0.86f, 0.86f, 0.86f) : (playable ? Color.white : new Color(0.93f, 0.93f, 0.93f));
            GUI.DrawTexture(row, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.enabled = playable;
            bool pressed = AudioManager.Button(row, "", rowBtnStyle);

            // 行内容画在按钮上面：关号 | 名字 | 说明 | 标签
            float x = row.x + 6;
            GUI.Label(new Rect(x, row.y, 34, row.height), i.ToString(), numStyle);
            x += 42;
            string name = Loc.IsZh ? d.NameZh : d.NameEn;
            GUI.Label(new Rect(x, row.y, 118, row.height), name, nameStyle);
            x += 124;
            float tagW = 54;
            GUI.Label(new Rect(x, row.y, row.xMax - tagW - 8 - x, row.height), d.Note, noteStyle);
            GUI.Label(new Rect(row.xMax - tagW - 6, row.y, tagW, row.height), PoolTag(d.Pool), tagStyle);
            GUI.enabled = true;

            if (pressed && playable) LoadLevel(i);
            y += RowH;
        }

        GUI.EndScrollView();
    }

    static string PoolTag(LevelPool pool)
    {
        switch (pool)
        {
            case LevelPool.Sample: return Loc.IsZh ? "样关" : "sample";
            case LevelPool.Candidate: return Loc.IsZh ? "备选" : "cand.";
            case LevelPool.Experiment: return Loc.IsZh ? "实验" : "exp.";
            default: return Loc.IsZh ? "正式" : "story";
        }
    }

    /// <summary>底部一行：无尽模式、商店、最高记录，右下角语言开关</summary>
    void DrawBottomBar(float sw, float sh)
    {
        float btnW = 150;
        float btnH = 40;
        float gap = 12f;
        float x = (sw - ListW) / 2f;
        float y = sh - 62f;

        if (AudioManager.Button(new Rect(x, y, btnW, btnH), Loc.T("menu.endless"), playBtnStyle))
            SceneManager.LoadScene("Endless");
        if (AudioManager.Button(new Rect(x + btnW + gap, y, btnW, btnH), Loc.T("menu.shop"), playBtnStyle))
            SceneManager.LoadScene("Shop");

        GUI.Label(new Rect(x + (btnW + gap) * 2 + 6, y, 300, btnH),
            Loc.F("menu.stats", ProgressManager.BestRound), hintStyle);

        // 语言开关放右下角。按钮上写的是"要切过去的语言"而不是当前语言 ——
        // 按钮该说它会做什么，不是它现在是什么。
        if (AudioManager.Button(new Rect(x + ListW - 100, y + 2, 100, 36), Loc.SwitchLabel, langBtnStyle))
            Loc.Toggle();

        // 缺字体时中文会渲染成空白，这里直说，免得以为是 bug
        if (Loc.IsZh && !Loc.CjkReady)
        {
            var warn = Loc.Fit(new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.75f, 0.35f, 0.35f) }
            });
            GUI.Label(new Rect(x + ListW - 410, y + 4, 300, 28), Loc.T("menu.nofont"), warn);
        }
    }

    void LoadLevel(int levelNumber)
    {
        GameManager.RequestedStartLevel = levelNumber;
        SceneManager.LoadScene("Level");
    }
}
