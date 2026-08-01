using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    private struct LevelEntry
    {
        public string sceneName;
        public int startIndex;
    }

    private static readonly LevelEntry[] AllLevels =
    {
        new LevelEntry { sceneName = "Level1-5", startIndex = 0 },
        new LevelEntry { sceneName = "Level1-5", startIndex = 1 },
        new LevelEntry { sceneName = "Level1-5", startIndex = 2 },
        new LevelEntry { sceneName = "Level1-5", startIndex = 3 },
        new LevelEntry { sceneName = "Level1-5", startIndex = 4 },
        new LevelEntry { sceneName = "Level6", startIndex = 0 },
        new LevelEntry { sceneName = "Level7", startIndex = 0 },
        new LevelEntry { sceneName = "Level8", startIndex = 0 },
        new LevelEntry { sceneName = "Level9", startIndex = 0 },
        new LevelEntry { sceneName = "Level10", startIndex = 0 },
    };

    private GUIStyle titleStyle;
    private GUIStyle playBtnStyle;
    private GUIStyle levelBtnStyle;
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
        levelBtnStyle = Loc.Fit(new GUIStyle(GUI.skin.button) { fontSize = 20 });
    }

    void OnGUI()
    {
        InitStyles();

        float scale = Screen.height / 600f;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));

        float sw = Screen.width / scale;
        float sh = Screen.height / scale;
        int unlocked = ProgressManager.UnlockedLevel;

        GUI.Label(new Rect(0, sh * 0.2f, sw, 60), "Bloom", titleStyle);

        var authorStyle = Loc.Fit(new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.45f, 0.45f, 0.45f) }
        });
        GUI.Label(new Rect(0, sh * 0.2f + 55, sw, 30), Loc.T("menu.author"), authorStyle);

        // 关卡格子永远画满 1-10，没解锁的灰着。
        // 原来这里有个"进度为 0 就只显示一个 Play 按钮"的分支，去掉了 ——
        // 它连带把 Endless / Shop 整块藏了，新玩家（以及第 1 关就死掉的玩家）
        // 根本看不到无尽模式，等于取消解锁门槛只取消了一半。
        int cols = 5;
        float btnSize = 65;
        float gap = 12;
        float totalW = cols * btnSize + (cols - 1) * gap;
        float startX = (sw - totalW) / 2f;
        float startY = sh * 0.4f;

        for (int i = 0; i < AllLevels.Length; i++)
        {
            int row = i / cols;
            int col = i % cols;
            float x = startX + col * (btnSize + gap);
            float y = startY + row * (btnSize + gap);
            int levelNum = i + 1;

            bool playable = levelNum <= unlocked;
            GUI.enabled = playable;
            if (AudioManager.Button(new Rect(x, y, btnSize, btnSize), levelNum.ToString(), levelBtnStyle)
                && playable)
                LoadLevel(i);
            GUI.enabled = true;
        }

        DrawEndlessSection(sw, startY + 2 * (btnSize + gap) + 28);
        DrawLanguageButton(sw, sh);
    }

    /// <summary>
    /// 语言开关放右下角。按钮上写的是"要切过去的语言"而不是当前语言 ——
    /// 按钮该说它会做什么，不是它现在是什么。
    /// </summary>
    void DrawLanguageButton(float sw, float sh)
    {
        var style = Loc.Fit(new GUIStyle(GUI.skin.button) { fontSize = 17 });
        if (AudioManager.Button(new Rect(sw - 130, sh - 56, 100, 36), Loc.SwitchLabel, style))
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
    /// 无尽模式和商店从一开始就能进，不再要求先通关十关。
    /// </summary>
    void DrawEndlessSection(float sw, float y)
    {
        var dividerColor = new Color(0.85f, 0.85f, 0.85f);
        float lineW = 400;
        GUI.color = dividerColor;
        GUI.DrawTexture(new Rect((sw - lineW) / 2f, y, lineW, 1), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float btnW = 170;
        float btnH = 50;
        float gap = 16f;
        float totalW = btnW * 2 + gap;
        float startX = (sw - totalW) / 2f;

        if (AudioManager.Button(new Rect(startX, y + 22, btnW, btnH), Loc.T("menu.endless"), playBtnStyle))
            SceneManager.LoadScene("Endless");
        if (AudioManager.Button(new Rect(startX + btnW + gap, y + 22, btnW, btnH), Loc.T("menu.shop"), playBtnStyle))
            SceneManager.LoadScene("Shop");

        var hintStyle = Loc.Fit(new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
        });
        GUI.Label(new Rect(0, y + 22 + btnH + 6, sw, 24),
            Loc.F("menu.stats", ProgressManager.BestRound), hintStyle);
    }

    void LoadLevel(int index)
    {
        GameManager.RequestedStartLevel = AllLevels[index].startIndex;
        SceneManager.LoadScene(AllLevels[index].sceneName);
    }
}
