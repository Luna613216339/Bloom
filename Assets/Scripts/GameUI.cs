using UnityEngine;

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

    private GUIStyle bigTextStyle;
    private GUIStyle infoTextStyle;
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

        bigTextStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 48,
            alignment = TextAnchor.UpperCenter,
            normal = { textColor = new Color(0.15f, 0.15f, 0.15f) }
        };

        infoTextStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            alignment = TextAnchor.UpperCenter,
            normal = { textColor = new Color(0.4f, 0.4f, 0.4f) }
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
        else if (passed)
        {
            this.showResult = true;
            this.passed = true;
        }
        else
        {
            this.showResult = true;
            this.passed = false;
        }
    }

    void OnGUI()
    {
        InitStyles();

        float sw = Screen.width;
        float sh = Screen.height;

        GUI.Label(new Rect(0, 10, sw, 60), currentCount.ToString(), bigTextStyle);

        string info = $"{levelName}  —  Target: {targetCount} / {totalCount}";
        GUI.Label(new Rect(0, 65, sw, 40), info, infoTextStyle);

        if (!showResult) return;

        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float pw = 400;
        float ph = 200;
        float px = (sw - pw) / 2f;
        float py = (sh - ph) / 2f;

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

        if (!passed)
        {
            if (GUI.Button(new Rect(px + (pw - btnW) / 2f, btnY, btnW, btnH), "Replay", buttonStyle))
                GameManager.Instance.Retry();
        }
    }
}
