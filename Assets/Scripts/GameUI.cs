using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUI : MonoBehaviour
{
    public static GameUI Instance { get; private set; }

    private string levelName;
    private int targetCount;
    private int totalCount;
    private int currentCount;
    private bool showResult;
    private bool passed;
    private bool hasNextLevel;

    private bool showRunOver;
    private int roundsCleared;
    private int coinsEarned;

    private GUIStyle hudLabelStyle;
    private GUIStyle hudCountStyle;
    private GUIStyle resultTextStyle;
    private GUIStyle buttonStyle;
    private GUIStyle coinStyle;
    private int styleVersion = -1;

    void Awake()
    {
        Instance = this;
    }

    void InitStyles()
    {
        if (styleVersion == Loc.Version) return;
        styleVersion = Loc.Version;
        coinStyle = null;

        // 深色主题下 HUD 要反过来。主题一局之内不会变（换主题得去商店，那是另一个场景），
        // 所以在这里定一次就够了
        bool dark = GameManager.Instance != null && GameManager.Instance.IsDarkBackground;

        hudLabelStyle = Loc.Fit(new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            alignment = TextAnchor.UpperRight,
            normal = { textColor = BallPalette.MutedInkFor(dark) }
        });

        hudCountStyle = Loc.Fit(new GUIStyle(GUI.skin.label)
        {
            fontSize = 38,
            alignment = TextAnchor.UpperRight,
            fontStyle = FontStyle.Bold,
            normal = { textColor = BallPalette.InkFor(dark) }
        });

        resultTextStyle = Loc.Fit(new GUIStyle(GUI.skin.label)
        {
            fontSize = 52,
            alignment = TextAnchor.MiddleCenter
        });

        buttonStyle = Loc.Fit(new GUIStyle(GUI.skin.button)
        {
            fontSize = 30
        });
    }

    public void ShowGameplay(string levelName, int target, int total)
    {
        this.levelName = levelName;
        this.targetCount = target;
        this.totalCount = total;
        this.currentCount = 0;
        this.showResult = false;
        this.showRunOver = false;
    }

    /// <summary>无尽模式整轮结束。这里没有 Replay，只有从第 1 轮重开</summary>
    public void ShowRunOver(int roundsCleared, int coinsEarned)
    {
        this.roundsCleared = roundsCleared;
        this.coinsEarned = coinsEarned;
        this.showRunOver = true;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayFail();
    }

    public void UpdateCount(int count)
    {
        currentCount = count;
    }

    public void ShowResult(bool passed, int triggered, int target, bool hasNext)
    {
        this.currentCount = triggered;
        this.targetCount = target;
        this.hasNextLevel = hasNext;

        if (AudioManager.Instance != null)
        {
            if (passed) AudioManager.Instance.PlaySuccess();
            else AudioManager.Instance.PlayFail();
        }

        if (passed && hasNext)
        {
            GameManager.Instance.NextLevel();
        }
        else
        {
            this.showResult = true;
            this.passed = passed;
        }
    }

    void OnGUI()
    {
        InitStyles();

        float scale = Screen.height / 600f;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));

        float sw = Screen.width / scale;
        float sh = Screen.height / scale;
        float pad = 15f;

        GUI.Label(new Rect(0, pad, sw - pad, 35), levelName, hudLabelStyle);
        GUI.Label(new Rect(0, pad + 35, sw - pad, 45), $"{currentCount} / {targetCount}", hudCountStyle);

        var gm = GameManager.Instance;
        if (gm != null && gm.IsEndless)
        {
            if (coinStyle == null)
                coinStyle = Loc.Fit(new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22,
                    alignment = TextAnchor.UpperRight,
                    normal = { textColor = new Color(0.85f, 0.65f, 0.15f) }
                });
            GUI.Label(new Rect(0, pad + 82, sw - pad, 30), $"◆ {gm.CoinsThisRun}", coinStyle);
        }

        if (showRunOver)
        {
            DrawRunOver(sw, sh);
            return;
        }

        if (!showResult) return;

        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float pw = 500;
        float py = (sh - 250) / 2f;
        float px = (sw - pw) / 2f;

        float btnW = 180;
        float btnH = 55;

        if (passed && !hasNextLevel)
        {
            resultTextStyle.fontSize = 60;
            resultTextStyle.normal.textColor = new Color(0.4f, 1f, 0.5f);
            GUI.Label(new Rect(px, py - 20, pw, 90), Loc.T("game.congrats"), resultTextStyle);
            resultTextStyle.fontSize = 52;

            float btnY = py + 100;
            float btnX = px + (pw - btnW) / 2f;
            if (AudioManager.Button(new Rect(btnX, btnY, btnW, btnH), Loc.T("game.menu"), buttonStyle))
                SceneManager.LoadScene("MainMenu");
        }
        else
        {
            resultTextStyle.normal.textColor = passed
                ? new Color(0.4f, 1f, 0.5f)
                : new Color(1f, 0.4f, 0.4f);

            string msg = passed
                ? Loc.F("game.passed", currentCount, targetCount)
                : Loc.F("game.tryagain", currentCount, targetCount);
            GUI.Label(new Rect(px, py, pw, 90), msg, resultTextStyle);

            float btnY = py + 120;
            float gap = 25f;
            float totalBtnW = btnW * 2 + gap;
            float btnStartX = px + (pw - totalBtnW) / 2f;

            if (AudioManager.Button(new Rect(btnStartX, btnY, btnW, btnH), Loc.T("game.replay"), buttonStyle))
                GameManager.Instance.Retry();

            if (AudioManager.Button(new Rect(btnStartX + btnW + gap, btnY, btnW, btnH), Loc.T("game.menu"), buttonStyle))
                SceneManager.LoadScene("MainMenu");
        }
    }

    void DrawRunOver(float sw, float sh)
    {
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float pw = 560;
        float px = (sw - pw) / 2f;
        float py = (sh - 300) / 2f;

        resultTextStyle.fontSize = 52;
        resultTextStyle.normal.textColor = new Color(1f, 0.4f, 0.4f);
        GUI.Label(new Rect(px, py, pw, 70), Loc.T("run.over"), resultTextStyle);

        var lineStyle = Loc.Fit(new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
        });
        GUI.Label(new Rect(px, py + 75, pw, 35),
            Loc.F("run.cleared", roundsCleared, ProgressManager.BestRound), lineStyle);

        lineStyle.normal.textColor = new Color(1f, 0.85f, 0.35f);
        GUI.Label(new Rect(px, py + 115, pw, 35), Loc.F("run.coins", coinsEarned), lineStyle);

        // 死亡当下是玩家最想花钱的时刻，商店入口不能少
        float btnW = 165;
        float btnH = 55;
        float gap = 20f;
        float btnY = py + 180;
        float totalW = btnW * 3 + gap * 2;
        float startX = px + (pw - totalW) / 2f;

        if (AudioManager.Button(new Rect(startX, btnY, btnW, btnH), Loc.T("run.new"), buttonStyle))
            GameManager.Instance.Retry();

        if (AudioManager.Button(new Rect(startX + btnW + gap, btnY, btnW, btnH), Loc.T("menu.shop"), buttonStyle))
            SceneManager.LoadScene("Shop");

        if (AudioManager.Button(new Rect(startX + (btnW + gap) * 2, btnY, btnW, btnH), Loc.T("game.menu"), buttonStyle))
            SceneManager.LoadScene("MainMenu");

    }

}
