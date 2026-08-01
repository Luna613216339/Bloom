using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static int RequestedStartLevel = 0;

    public enum GameState { WaitingForInput, ChainReacting, LevelComplete }
    public GameState State { get; private set; }
    public List<Ball> AllBalls { get; } = new List<Ball>();

    [Header("Global Settings (shared by all levels)")]
    [SerializeField] private float ballSpeed = 1.8f;
    [SerializeField] private float ballSize = 0.4f;
    [SerializeField] private float reactionMaxScale = 1.3f;
    [SerializeField] private float reactionDuration = 1.5f;

    [Serializable]
    public class LevelConfig
    {
        public string levelName = "Level 1";
        public int ballCount = 20;
        public int targetCount = 5;
    }

    [Header("Per-Level Settings")]
    [SerializeField] private LevelConfig[] levels = new LevelConfig[]
    {
        new LevelConfig { levelName = "Level 1", ballCount = 20, targetCount = 5 },
        new LevelConfig { levelName = "Level 2", ballCount = 25, targetCount = 10 },
        new LevelConfig { levelName = "Level 3", ballCount = 30, targetCount = 15 },
        new LevelConfig { levelName = "Level 4", ballCount = 35, targetCount = 22 },
        new LevelConfig { levelName = "Level 5", ballCount = 40, targetCount = 30 },
    };

    [Header("Level Flow")]
    [SerializeField] private string nextScene = "";
    [SerializeField] private int globalLevelStart = 1;

    [Header("Endless Mode（勾上之后，上面的 Levels 数组不生效）")]
    [Tooltip("参数逐轮由下面的公式生成，失败即结束整轮挑战")]
    [SerializeField] private bool endlessMode = false;
    [SerializeField] private EndlessConfig endless = new EndlessConfig();

    private int currentLevel;
    private int triggeredCount;
    private int goldThisRound;
    private int activeReactions;
    private Camera cam;
    private const float GameAspect = 16f / 9f;
    private readonly List<GameObject> screenBounds = new List<GameObject>();

    private readonly LevelConfig endlessConfig = new LevelConfig();

    public bool IsEndless => endlessMode;

    /// <summary>无尽模式当前轮次，从 1 起</summary>
    public int Round { get; private set; } = 1;

    /// <summary>本轮挑战累计的金币，失败结算时才真正入账</summary>
    public int CoinsThisRun { get; private set; }

    private LevelConfig Config => endlessMode
        ? endlessConfig
        : levels[Mathf.Clamp(currentLevel, 0, levels.Length - 1)];

    public int CurrentBallCount => Config.ballCount;

    /// <summary>全局关卡号（1-10），Level1-5 场景里随 currentLevel 递增</summary>
    public int GlobalLevel => globalLevelStart + currentLevel;

    /// <summary>
    /// 球的配色。正式关卡锁死作者配色，无尽模式才用商店买的主题。
    /// SchoolSpawner 也从这里取。
    /// </summary>
    public Color[] Palette => endlessMode
        ? BallPalette.ForTheme(ProgressManager.EquippedTheme)
        : BallPalette.ForLevel(GlobalLevel);

    /// <summary>
    /// 背景色。正式关卡永远白底（关卡配色是照白底调的），只有无尽模式跟着主题走。
    /// </summary>
    public Color Background => endlessMode
        ? BallPalette.BackgroundForTheme(ProgressManager.EquippedTheme)
        : Color.white;

    /// <summary>HUD、点击圈这些灰度元素要靠它决定往亮还是往暗走</summary>
    public bool IsDarkBackground => endlessMode
        && BallPalette.IsDarkTheme(ProgressManager.EquippedTheme);

#if UNITY_EDITOR
    // 给 Editor/BalanceSimulator 读参数用。模拟器必须和 Inspector 上的值完全一致，
    // 否则算出来的通关率是假的。
    public EndlessConfig EditorEndless => endless;
    public float EditorBallSpeed => ballSpeed;
    public float EditorBallSize => ballSize;
    public float EditorReactionMaxScale => reactionMaxScale;
    public float EditorReactionDuration => reactionDuration;
#endif

    void Awake()
    {
        Instance = this;
        cam = Camera.main;
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Background;
        Ball.SetupPhysics();
    }

    void Start()
    {
        currentLevel = RequestedStartLevel;
        RequestedStartLevel = 0;
        StartLevel();
    }

    void StartLevel()
    {
        ClearBalls();
        triggeredCount = 0;
        goldThisRound = 0;
        activeReactions = 0;
        State = GameState.WaitingForInput;

        if (endlessMode)
        {
            endlessConfig.levelName = Loc.F("game.round", Round);
            endlessConfig.ballCount = endless.BallCount(Round);
            endlessConfig.targetCount = endless.TargetCount(Round);

            // 第 1 轮 = 新的一局。首次进场和 Retry 都走这里，只用挂一个钩子
            if (Round == 1) RunLogger.BeginRun();
        }

        // 正式关卡的名字由关号算出来，不用 Inspector 里的 levelName —— 那个没法翻译。
        // levels[] 的 levelName 现在只是编辑器里的标签，方便你在数组里认出是哪一关
        string title = endlessMode ? Config.levelName : Loc.F("game.level", GlobalLevel);
        GameUI.Instance.ShowGameplay(title, Config.targetCount, Config.ballCount);
        CreateScreenBounds();

        var schools = FindObjectsByType<SchoolSpawner>(FindObjectsSortMode.None);
        if (schools.Length > 0)
        {
            foreach (var school in schools)
                school.Init();
        }
        else
            SpawnBalls();
    }

    void ClearBalls()
    {
        for (int i = AllBalls.Count - 1; i >= 0; i--)
        {
            if (AllBalls[i] != null)
                Destroy(AllBalls[i].gameObject);
        }
        AllBalls.Clear();

        foreach (var obj in screenBounds)
        {
            if (obj != null) Destroy(obj);
        }
        screenBounds.Clear();

        foreach (var cr in FindObjectsByType<ClickReaction>(FindObjectsSortMode.None))
            Destroy(cr.gameObject);

        foreach (var door in FindObjectsByType<Door>(FindObjectsSortMode.None))
            door.ResetState();
    }

    void CreateScreenBounds()
    {
        float h = 2f * cam.orthographicSize;
        float w = h * GameAspect;
        float thickness = 1f;

        CreateWall("BoundTop",    new Vector2(0, h / 2f + thickness / 2f),  new Vector2(w + thickness * 2, thickness));
        CreateWall("BoundBottom", new Vector2(0, -h / 2f - thickness / 2f), new Vector2(w + thickness * 2, thickness));
        CreateWall("BoundLeft",   new Vector2(-w / 2f - thickness / 2f, 0), new Vector2(thickness, h + thickness * 2));
        CreateWall("BoundRight",  new Vector2(w / 2f + thickness / 2f, 0),  new Vector2(thickness, h + thickness * 2));
    }

    void CreateWall(string name, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = size;
        col.sharedMaterial = Ball.bounceMat;
        screenBounds.Add(go);
    }

    void SpawnBalls()
    {
        float h = 2f * cam.orthographicSize;
        float w = h * GameAspect;
        float margin = 0.5f;

        // 金球是这批球里随机的几颗，不是额外加的 —— 否则场上总数变了，平衡数值全乱
        var goldIndices = PickGoldIndices(Config.ballCount);

        for (int i = 0; i < Config.ballCount; i++)
        {
            float x = UnityEngine.Random.Range(-w / 2f + margin, w / 2f - margin);
            float y = UnityEngine.Random.Range(-h / 2f + margin, h / 2f - margin);
            Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
            Color color = BallPalette.Cycle(Palette, i);

            var ball = Ball.Create(new Vector2(x, y), dir * ballSpeed, color,
                ballSize, reactionMaxScale, reactionDuration);

            if (goldIndices != null && goldIndices.Contains(i))
                ball.SetAsGoldBall();
        }
    }

    /// <summary>
    /// 随机挑几颗当金球。位置纯随机，不刻意摆在边角制造取舍 ——
    /// 玩家看不见那种设计，只会觉得游戏怪怪的。金球是彩蛋，不是考题。
    /// </summary>
    HashSet<int> PickGoldIndices(int ballCount)
    {
        if (!endlessMode || endless.goldBallCount <= 0) return null;

        var set = new HashSet<int>();
        int want = Mathf.Min(endless.goldBallCount, ballCount);
        while (set.Count < want)
            set.Add(UnityEngine.Random.Range(0, ballCount));
        return set;
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Vector3 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0f;

        if (State == GameState.WaitingForInput || State == GameState.ChainReacting)
        {
            foreach (var door in FindObjectsByType<Door>(FindObjectsSortMode.None))
            {
                if (door.ContainsPoint(worldPos))
                {
                    door.Toggle();
                    return;
                }
            }
        }

        if (State == GameState.WaitingForInput)
        {
            ClickReaction.Create(worldPos, reactionMaxScale, reactionDuration);
            State = GameState.ChainReacting;
        }
    }

    public void RegisterBall(Ball ball) => AllBalls.Add(ball);
    public void UnregisterBall(Ball ball) => AllBalls.Remove(ball);

    public void OnBallTriggered(Ball ball)
    {
        triggeredCount++;

        bool isGold = ball != null && ball.IsGoldBall;

        if (AudioManager.Instance != null)
        {
            if (isGold) AudioManager.Instance.PlayGold();
            else AudioManager.Instance.PlayChain(triggeredCount);
        }

        // 金球算进通关数（它就是一颗普通球外加奖励），否则玩家看着数字对不上会困惑
        if (endlessMode && isGold)
        {
            goldThisRound++;
            CoinsThisRun += endless.goldBallReward;
        }

        GameUI.Instance.UpdateCount(triggeredCount);
    }

    public void ReactionStarted() => activeReactions++;

    public void ReactionEnded()
    {
        activeReactions--;
        if (activeReactions <= 0 && State == GameState.ChainReacting)
        {
            State = GameState.LevelComplete;
            bool passed = triggeredCount >= Config.targetCount;

            if (endlessMode)
            {
                EndRound(passed);
                return;
            }

            bool hasNext = currentLevel < levels.Length - 1 || !string.IsNullOrEmpty(nextScene);

            if (passed)
                ProgressManager.CompleteLevel(globalLevelStart + currentLevel);

            GameUI.Instance.ShowResult(passed, triggeredCount, Config.targetCount, hasNext);
        }
    }

    /// <summary>无尽模式一轮结束。过了就无缝进下一轮，没过整轮挑战就到此为止</summary>
    void EndRound(bool passed)
    {
        if (passed)
        {
            CoinsThisRun += endless.OverkillReward(triggeredCount, Config.targetCount);
            CoinsThisRun += endless.MilestoneBonus(Round);
            ProgressManager.ReportClearedRound(Round);
            LogRound(true);
            GameUI.Instance.ShowResult(true, triggeredCount, Config.targetCount, true);
        }
        else
        {
            LogRound(false);
            ProgressManager.AddCoins(CoinsThisRun);
            GameUI.Instance.ShowRunOver(Round - 1, CoinsThisRun);
        }
    }

    void LogRound(bool passed)
    {
        RunLogger.RecordRound(Round, Config.ballCount, Config.targetCount,
                              triggeredCount, goldThisRound, passed, CoinsThisRun);
    }

    public void NextLevel()
    {
        if (endlessMode)
        {
            Round++;
            StartLevel();
            return;
        }

        if (currentLevel < levels.Length - 1)
        {
            currentLevel++;
            StartLevel();
        }
        else if (!string.IsNullOrEmpty(nextScene))
        {
            SceneManager.LoadScene(nextScene);
        }
    }

    /// <summary>正式关卡是重玩本关；无尽模式是从第 1 轮重开一整轮，否则最高记录就没意义了</summary>
    public void Retry()
    {
        if (endlessMode)
        {
            Round = 1;
            CoinsThisRun = 0;
        }
        StartLevel();
    }
}
