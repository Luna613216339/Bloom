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

    private GUIStyle hudLabelStyle;
    private GUIStyle hudCountStyle;
    private GUIStyle resultTextStyle;
    private GUIStyle buttonStyle;
    private bool stylesReady;

    void Awake()
    {
        Instance = this;
    }

    void InitStyles()
    {
        if (stylesReady) return;
        stylesReady = true;

        hudLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            alignment = TextAnchor.UpperRight,
            normal = { textColor = new Color(0.55f, 0.55f, 0.55f) }
        };

        hudCountStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            alignment = TextAnchor.UpperRight,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.3f, 0.3f, 0.3f) }
        };

        resultTextStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 36,
            alignment = TextAnchor.MiddleCenter
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 22
        };
    }

    public void ShowGameplay(string levelName, int target, int total)
    {
        this.levelName = levelName;
        this.targetCount = target;
        this.totalCount = total;
        this.currentCount = 0;
        this.showResult = false;
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

        float sw = Screen.width;
        float sh = Screen.height;
        float pad = 15f;

        GUI.Label(new Rect(0, pad, sw - pad, 22), levelName, hudLabelStyle);
        GUI.Label(new Rect(0, pad + 22, sw - pad, 28), $"{currentCount} / {targetCount}", hudCountStyle);

        if (!showResult) return;

        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float pw = 400;
        float py = (sh - 200) / 2f;
        float px = (sw - pw) / 2f;

        resultTextStyle.normal.textColor = passed
            ? new Color(0.4f, 1f, 0.5f)
            : new Color(1f, 0.4f, 0.4f);

        string msg = passed
            ? $"Passed!  {currentCount} / {targetCount}"
            : $"Try Again  {currentCount} / {targetCount}";
        GUI.Label(new Rect(px, py, pw, 80), msg, resultTextStyle);

        float btnW = 140;
        float btnH = 45;
        float btnY = py + 100;
        float gap = 20f;
        float totalBtnW = btnW * 2 + gap;
        float btnStartX = px + (pw - totalBtnW) / 2f;

        if (GUI.Button(new Rect(btnStartX, btnY, btnW, btnH), "Replay", buttonStyle))
            GameManager.Instance.Retry();

        if (GUI.Button(new Rect(btnStartX + btnW + gap, btnY, btnW, btnH), "Menu", buttonStyle))
            SceneManager.LoadScene("MainMenu");
    }
}
