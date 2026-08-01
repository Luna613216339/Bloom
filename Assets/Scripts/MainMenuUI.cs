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
    private bool stylesReady;

    void Awake()
    {
        var cam = Camera.main;
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.white;
    }

    void InitStyles()
    {
        if (stylesReady) return;
        stylesReady = true;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 48,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.15f, 0.15f, 0.15f) }
        };

        playBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 24 };
        levelBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 20 };
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

        var authorStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.45f, 0.45f, 0.45f) }
        };
        GUI.Label(new Rect(0, sh * 0.2f + 55, sw, 30), "Author: Jie Li", authorStyle);

        if (unlocked <= 1)
        {
            float btnW = 180;
            float btnH = 55;
            if (AudioManager.Button(new Rect((sw - btnW) / 2f, sh * 0.45f, btnW, btnH), "Play", playBtnStyle))
                LoadLevel(0);
        }
        else
        {
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

                if (levelNum <= unlocked)
                {
                    if (AudioManager.Button(new Rect(x, y, btnSize, btnSize), levelNum.ToString(), levelBtnStyle))
                        LoadLevel(i);
                }
                else
                {
                    GUI.enabled = false;
                    AudioManager.Button(new Rect(x, y, btnSize, btnSize), levelNum.ToString(), levelBtnStyle);
                    GUI.enabled = true;
                }
            }

            DrawEndlessSection(sw, startY + 2 * (btnSize + gap) + 28);
        }
    }

    /// <summary>
    /// 通关十关之前按钮是灰的，不是藏起来的 —— 玩家第一次进菜单就该知道后面还有东西。
    /// </summary>
    void DrawEndlessSection(float sw, float y)
    {
        var dividerColor = new Color(0.85f, 0.85f, 0.85f);
        float lineW = 400;
        GUI.color = dividerColor;
        GUI.DrawTexture(new Rect((sw - lineW) / 2f, y, lineW, 1), Texture2D.whiteTexture);
        GUI.color = Color.white;

        bool unlocked = ProgressManager.EndlessUnlocked;
        float btnW = 170;
        float btnH = 50;
        float gap = 16f;
        float totalW = btnW * 2 + gap;
        float startX = (sw - totalW) / 2f;

        GUI.enabled = unlocked;
        if (AudioManager.Button(new Rect(startX, y + 22, btnW, btnH), "Endless", playBtnStyle) && unlocked)
            SceneManager.LoadScene("Endless");
        if (AudioManager.Button(new Rect(startX + btnW + gap, y + 22, btnW, btnH), "Shop", playBtnStyle) && unlocked)
            SceneManager.LoadScene("Shop");
        GUI.enabled = true;

        var hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
        };
        string hint = unlocked
            ? $"Best: Round {ProgressManager.BestRound}     Pollen {ProgressManager.Coins}"
            : "Clear all 10 levels to unlock";
        GUI.Label(new Rect(0, y + 22 + btnH + 6, sw, 24), hint, hintStyle);
    }

    void LoadLevel(int index)
    {
        GameManager.RequestedStartLevel = AllLevels[index].startIndex;
        SceneManager.LoadScene(AllLevels[index].sceneName);
    }
}
