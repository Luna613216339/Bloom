using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    private GUIStyle titleStyle;
    private GUIStyle playBtnStyle;
    private GUIStyle levelBtnStyle;
    private GUIStyle sampleBtnStyle;
    private GUIStyle langBtnStyle;
    private GUIStyle authorStyle;
    private GUIStyle hintStyle;
    private int styleVersion = -1;

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
            fontSize = 48,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.15f, 0.15f, 0.15f) }
        });

        playBtnStyle = Loc.Fit(new GUIStyle(GUI.skin.button) { fontSize = 24 });
        levelBtnStyle = Loc.Fit(new GUIStyle(GUI.skin.button) { fontSize = 18 });
        // 样关（还没批准的新机制）用斜体，和正式关分开
        sampleBtnStyle = Loc.Fit(new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Italic });
        // 语言按钮永远用中文字体：它的文案是"要切过去的语言"，
        // 英文界面下显示的是「中文」两个汉字，内置字体渲染不出来
        langBtnStyle = Loc.FitAlwaysCjk(new GUIStyle(GUI.skin.button) { fontSize = 17 });

        authorStyle = Loc.Fit(new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.45f, 0.45f, 0.45f) }
        });
        hintStyle = Loc.Fit(new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
        });
    }

    void OnGUI()
    {
        InitStyles();

        float scale = Screen.height / 600f;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));

        float sw = Screen.width / scale;
        float sh = Screen.height / scale;
        int unlocked = ProgressManager.UnlockedLevel;

        GUI.Label(new Rect(0, sh * 0.17f, sw, 60), "Bloom", titleStyle);
        GUI.Label(new Rect(0, sh * 0.17f + 55, sw, 30), Loc.T("menu.author"), authorStyle);

        // 关卡格子一次画全，没解锁的灰着。格子数跟着 LevelCatalog 走，
        // 每行 6 个，关卡再多也只是多一行。
        int count = LevelCatalog.Count;
        int cols = 6;
        int rows = (count + cols - 1) / cols;
        float btnSize = 50;
        float gap = 8;
        float totalW = cols * btnSize + (cols - 1) * gap;
        float startX = (sw - totalW) / 2f;
        float startY = sh * 0.17f + 100;

        for (int i = 0; i < count; i++)
        {
            int row = i / cols;
            int col = i % cols;
            float x = startX + col * (btnSize + gap);
            float y = startY + row * (btnSize + gap);
            int levelNum = i + 1;
            var def = LevelCatalog.Get(levelNum);

            bool playable = levelNum <= unlocked;
            GUI.enabled = playable;
            var style = def != null && def.Sample ? sampleBtnStyle : levelBtnStyle;
            if (AudioManager.Button(new Rect(x, y, btnSize, btnSize), levelNum.ToString(), style)
                && playable)
                LoadLevel(levelNum);
            GUI.enabled = true;
        }

        DrawEndlessSection(sw, startY + rows * (btnSize + gap) + 10);
        DrawLanguageButton(sw, sh);
    }

    /// <summary>
    /// 语言开关放右下角。按钮上写的是"要切过去的语言"而不是当前语言 ——
    /// 按钮该说它会做什么，不是它现在是什么。
    /// </summary>
    void DrawLanguageButton(float sw, float sh)
    {
        if (AudioManager.Button(new Rect(sw - 130, sh - 56, 100, 36), Loc.SwitchLabel, langBtnStyle))
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
            GUI.Label(new Rect(sw - 430, sh - 52, 290, 28), Loc.T("menu.nofont"), warn);
        }
    }

    /// <summary>
    /// 无尽模式和商店从一开始就能进，不再要求先通关。
    /// </summary>
    void DrawEndlessSection(float sw, float y)
    {
        var dividerColor = new Color(0.85f, 0.85f, 0.85f);
        float lineW = 400;
        GUI.color = dividerColor;
        GUI.DrawTexture(new Rect((sw - lineW) / 2f, y, lineW, 1), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float btnW = 170;
        float btnH = 46;
        float gap = 16f;
        float totalW = btnW * 2 + gap;
        float startX = (sw - totalW) / 2f;

        if (AudioManager.Button(new Rect(startX, y + 14, btnW, btnH), Loc.T("menu.endless"), playBtnStyle))
            SceneManager.LoadScene("Endless");
        if (AudioManager.Button(new Rect(startX + btnW + gap, y + 14, btnW, btnH), Loc.T("menu.shop"), playBtnStyle))
            SceneManager.LoadScene("Shop");

        GUI.Label(new Rect(0, y + 14 + btnH + 4, sw, 24),
            Loc.F("menu.stats", ProgressManager.BestRound), hintStyle);
    }

    void LoadLevel(int levelNumber)
    {
        GameManager.RequestedStartLevel = levelNumber;
        SceneManager.LoadScene("Level");
    }
}
