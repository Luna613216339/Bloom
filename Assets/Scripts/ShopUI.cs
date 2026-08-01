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
    const int Columns = 2;
    const float CardW = 300f;
    const float CardH = 210f;
    const float CardGap = 28f;
    const float PanelW = 260f;

    private GUIStyle titleStyle;
    private GUIStyle nameStyle;
    private GUIStyle priceStyle;
    private GUIStyle smallStyle;
    private GUIStyle buttonStyle;
    private bool stylesReady;

    private Vector2 scroll;

    static readonly Color Ink = new Color(0.15f, 0.15f, 0.15f);
    static readonly Color Muted = new Color(0.5f, 0.5f, 0.5f);
    static readonly Color Gold = new Color(0.85f, 0.65f, 0.15f);
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
        if (stylesReady) return;
        stylesReady = true;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 34,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Ink }
        };
        nameStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            normal = { textColor = Ink }
        };
        priceStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = Gold }
        };
        smallStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            normal = { textColor = Muted }
        };
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 17 };
    }

    void OnGUI()
    {
        InitStyles();

        float scale = Screen.height / 600f;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));
        float sw = Screen.width / scale;
        float sh = Screen.height / scale;

        float pad = 30f;

        GUI.Label(new Rect(pad, 18, 400, 45), "Skin Shop", titleStyle);
        DrawWallet(new Rect(sw - pad - 200, 22, 200, 30));

        float gridX = pad;
        float gridY = 78f;
        float gridW = Columns * CardW + (Columns - 1) * CardGap;

        int rows = Mathf.CeilToInt(BallPalette.ThemeCount / (float)Columns);
        float contentH = rows * (CardH + CardGap);
        var viewport = new Rect(gridX, gridY, gridW + 20f, sh - gridY - 70f);

        scroll = GUI.BeginScrollView(viewport, scroll,
            new Rect(0, 0, gridW, contentH));

        for (int i = 0; i < BallPalette.ThemeCount; i++)
        {
            float x = (i % Columns) * (CardW + CardGap);
            float y = (i / Columns) * (CardH + CardGap);
            DrawCard(new Rect(x, y, CardW, CardH), i);
        }

        GUI.EndScrollView();

        DrawCurrentPanel(new Rect(sw - pad - PanelW, gridY, PanelW, 260));

        if (AudioManager.Button(new Rect(pad, sh - 60, 150, 42), "Back", buttonStyle))
            SceneManager.LoadScene("MainMenu");
    }

    void DrawWallet(Rect r)
    {
        var coin = new Rect(r.x + r.width - 150, r.y + 2, 24, 24);
        DrawCircle(coin, Gold);
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = Gold }
        };
        GUI.Label(new Rect(coin.x + 32, r.y, 140, 28), ProgressManager.Coins.ToString(), style);
    }

    void DrawCard(Rect r, int index)
    {
        DrawBox(r, Line);

        var preview = new Rect(r.x + 1, r.y + 1, r.width - 2, 108);
        DrawThemePreview(preview, index);

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
        GUI.Label(new Rect(r.x + 12, textY, r.width - 24, 26), BallPalette.ThemeNames[index], nameStyle);

        bool owned = ProgressManager.IsThemeOwned(index);
        bool equipped = ProgressManager.EquippedTheme == index;
        int price = BallPalette.ThemePrices[index];

        var btnRect = new Rect(r.x + r.width - 108, textY + 28, 96, 34);

        if (equipped)
        {
            GUI.enabled = false;
            AudioManager.Button(btnRect, "Equipped", buttonStyle);
            GUI.enabled = true;
        }
        else if (owned)
        {
            if (AudioManager.Button(btnRect, "Equip", buttonStyle))
            {
                ProgressManager.EquippedTheme = index;
                if (AudioManager.Instance != null) AudioManager.Instance.PlayEquip();
            }
        }
        else
        {
            GUI.Label(new Rect(r.x + 12, textY + 30, r.width - 130, 26), $"◆ {price}", priceStyle);
            GUI.enabled = ProgressManager.Coins >= price;
            if (AudioManager.Button(btnRect, "Unlock", buttonStyle))
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

        if (owned && !equipped)
            GUI.Label(new Rect(r.x + 12, textY + 32, 120, 22), "Owned", smallStyle);
    }

    /// <summary>占位预览：用主题色在框里摆一排球。等美术出图后整个换掉</summary>
    void DrawThemePreview(Rect r, int index)
    {
        var colors = BallPalette.ForTheme(index);
        GUI.color = new Color(0.97f, 0.97f, 0.97f);
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

        GUI.Label(new Rect(r.x + 8, r.yMax - 24, r.width - 16, 20), "preview placeholder", smallStyle);
    }

    void DrawCurrentPanel(Rect r)
    {
        GUI.Label(new Rect(r.x, r.y - 4, r.width, 34),
            "Current Theme", new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                normal = { textColor = Ink }
            });

        var box = new Rect(r.x, r.y + 34, r.width, 150);
        DrawBox(box, Line);
        DrawThemePreview(new Rect(box.x + 1, box.y + 1, box.width - 2, 108), ProgressManager.EquippedTheme);

        GUI.Label(new Rect(box.x + 12, box.yMax - 34, box.width - 24, 26),
            BallPalette.ThemeNames[ProgressManager.EquippedTheme], nameStyle);

        GUI.Label(new Rect(r.x, box.yMax + 10, r.width, 40),
            "Themes apply to Endless only.\nStory levels keep their own colors.", smallStyle);
    }

    static void DrawBox(Rect r, Color border)
    {
        GUI.color = border;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, 1), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.x, r.yMax - 1, r.width, 1), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.x, r.y, 1, r.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.xMax - 1, r.y, 1, r.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    static void DrawCircle(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, SpriteHelper.Circle.texture);
        GUI.color = Color.white;
    }
}
