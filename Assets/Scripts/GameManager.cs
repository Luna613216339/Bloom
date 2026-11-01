using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>主菜单选关时写：要进的关号（1 起）。0 = 第 1 关</summary>
    public static int RequestedStartLevel = 0;

    public enum GameState { WaitingForInput, ChainReacting, LevelComplete }
    public GameState State { get; private set; }
    public List<Ball> AllBalls { get; } = new List<Ball>();

    [Header("Global Settings（只有无尽模式用；正式关卡的参数在 LevelCatalog 里）")]
    [SerializeField] private float ballSpeed = 1.8f;
    [SerializeField] private float ballSize = 0.4f;
    [SerializeField] private float reactionMaxScale = 1.48f;
    [SerializeField] private float reactionDuration = 1.5f;

    [Header("Endless Mode（勾上就是无尽模式，不勾就是正式关卡）")]
    [Tooltip("参数逐轮由下面的公式生成，失败即结束整轮挑战")]
    [SerializeField] private bool endlessMode = false;
    [SerializeField] private EndlessConfig endless = new EndlessConfig();

    // ---- 续命（占位价，正式定价以后再说）----
    /// <summary>正式关卡再点一下的价钱。比无尽模式贵：正式关卡能无限重玩，续命是在买"省事"</summary>
    public const int StoryContinuePrice = 60;
    public const int EndlessContinuePrice = 30;
    /// <summary>一次尝试里最多续几次</summary>
    public const int MaxContinues = 1;

    private LevelDef level;
    private int currentLevel = 1;
    private int triggeredCount;
    private int goldThisRound;
    private int activeReactions;
    private int continuesUsed;
    private int bankedThisRun;
    private int sourceCounter;
    private bool lastPassed;
    private Camera cam;
    private LevelBuilder built;
    private const float GameAspect = 16f / 9f;
    private readonly List<GameObject> screenBounds = new List<GameObject>();

    // 无尽模式每轮的参数
    private int endlessBallCount;
    private int endlessTarget;

    public bool IsEndless => endlessMode;
    public LevelDef Level => level;

    /// <summary>无尽模式当前轮次，从 1 起</summary>
    public int Round { get; private set; } = 1;

    /// <summary>本轮挑战累计的金币，失败结算时才真正入账</summary>
    public int CoinsThisRun { get; private set; }

    public int CurrentBallCount => endlessMode ? endlessBallCount : level.TotalBalls;
    public int TargetCount => endlessMode ? endlessTarget : level.Target;
    public int TriggeredCount => triggeredCount;

    /// <summary>每结算一次 +1，自动测试靠它判断"这一次尝试结束了"（过关会立刻跳下一关，State 看不出来）</summary>
    public int ResultCounter { get; private set; }
    public bool LastResultPassed { get; private set; }
    public int LastResultTriggered { get; private set; }
    public int LastResultTarget { get; private set; }

    /// <summary>全局关卡号（1 起）</summary>
    public int GlobalLevel => currentLevel;

    /// <summary>
    /// 球的配色。正式关卡锁死作者配色，无尽模式才用商店买的主题。
    /// SchoolSpawner 也从这里取。
    /// </summary>
    public Color[] Palette => endlessMode
        ? BallPalette.ForTheme(ProgressManager.EquippedTheme)
        : level.Palette;

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
        currentLevel = Mathf.Clamp(RequestedStartLevel, 1, Mathf.Max(1, LevelCatalog.Count));
        RequestedStartLevel = 0;
        StartLevel();
    }

    void StartLevel()
    {
        ClearBalls();
        triggeredCount = 0;
        goldThisRound = 0;
        activeReactions = 0;
        continuesUsed = 0;
        lastPassed = false;
        State = GameState.WaitingForInput;

        string title;
        if (endlessMode)
        {
            endlessBallCount = endless.BallCount(Round);
            endlessTarget = endless.TargetCount(Round);
            title = Loc.F("game.round", Round);

            // 第 1 轮 = 新的一局。首次进场和 Retry 都走这里，只用挂一个钩子
            if (Round == 1) RunLogger.BeginRun();
        }
        else
        {
            level = LevelCatalog.Get(currentLevel);
            title = Loc.F("game.level", currentLevel);
        }

        GameUI.Instance.ShowGameplay(title, TargetCount, CurrentBallCount);
        CreateScreenBounds();

        if (endlessMode)
        {
            SpawnEndlessBalls();
        }
        else
        {
            built = LevelBuilder.Build(level);
            foreach (var school in built.Schools)
                school.Init();
            built.SpawnBalls();
        }
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

        if (built != null)
        {
            built.Destroy();
            built = null;
        }
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
        // 放在 Ignore Raycast 层：球照样弹，但蛇的"前方有没有门"射线不会把屏幕边界当成门。
        // 路径起点在屏幕外的蛇，队头一出生就在边界外面，否则会被自己的保险丝卡住永远不动。
        go.layer = 2;
        go.transform.position = position;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = size;
        col.sharedMaterial = Ball.bounceMat;
        screenBounds.Add(go);
    }

    void SpawnEndlessBalls()
    {
        float h = 2f * cam.orthographicSize;
        float w = h * GameAspect;
        float margin = 0.5f;

        // 金球是这批球里随机的几颗，不是额外加的 —— 否则场上总数变了，平衡数值全乱
        var goldIndices = PickGoldIndices(endlessBallCount);

        for (int i = 0; i < endlessBallCount; i++)
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
        if (AutoTest.Active) return;
        if (!Input.GetMouseButtonDown(0)) return;

        Vector3 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        HandlePointer(worldPos);
    }

    /// <summary>
    /// 玩家在世界坐标 p 处点了一下。先看是不是点在门上（任何时候都能开关门），
    /// 不是门而且还没点过，就是这一关唯一的那一下。自动测试也走这里。
    /// </summary>
    public void HandlePointer(Vector2 p)
    {
        if (State == GameState.WaitingForInput || State == GameState.ChainReacting)
        {
            foreach (var door in FindObjectsByType<Door>(FindObjectsSortMode.None))
            {
                if (door.ContainsPoint(p))
                {
                    door.Toggle();
                    return;
                }
            }
        }

        if (State == GameState.WaitingForInput)
        {
            ClickReaction.Create(p, ActiveMaxScale, ActiveDuration);
            State = GameState.ChainReacting;
        }
    }

    /// <summary>按建关顺序找第几扇门开关一下。自动测试用</summary>
    public bool ToggleDoor(int index)
    {
        if (built == null || index < 0 || index >= built.Doors.Count) return false;
        built.Doors[index].Toggle();
        return true;
    }

    float ActiveMaxScale => endlessMode ? reactionMaxScale : level.ReactionMaxScale;
    float ActiveDuration => endlessMode ? reactionDuration : level.ReactionDuration;

    /// <summary>每一把"火"（点击圈、被点着的球、火星）拿一个编号，硬球靠它分辨两把火</summary>
    public int NextSourceId() => ++sourceCounter;

    public void RegisterBall(Ball ball) => AllBalls.Add(ball);
    public void UnregisterBall(Ball ball) => AllBalls.Remove(ball);

    public void OnBallTriggered(Ball ball)
    {
        if (ball != null && !ball.CountsForTarget) return;
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
            lastPassed = triggeredCount >= TargetCount;
            LastResultPassed = lastPassed;
            LastResultTriggered = triggeredCount;
            LastResultTarget = TargetCount;
            ResultCounter++;

            if (endlessMode)
            {
                EndRound(lastPassed);
                return;
            }

            bool hasNext = currentLevel < LevelCatalog.Count;

            if (lastPassed && !AutoTest.Active)
                ProgressManager.CompleteLevel(currentLevel);

            GameUI.Instance.ShowResult(lastPassed, triggeredCount, TargetCount, hasNext);
        }
    }

    /// <summary>无尽模式一轮结束。过了就无缝进下一轮，没过整轮挑战就到此为止（除非续命）</summary>
    void EndRound(bool passed)
    {
        if (passed)
        {
            CoinsThisRun += endless.OverkillReward(triggeredCount, TargetCount);
            CoinsThisRun += endless.MilestoneBonus(Round);
            ProgressManager.ReportClearedRound(Round);
            LogRound(true);
            GameUI.Instance.ShowResult(true, triggeredCount, TargetCount, true);
        }
        else
        {
            LogRound(false);
            BankRun();
            GameUI.Instance.ShowRunOver(Round - 1, CoinsThisRun);
        }
    }

    /// <summary>
    /// 把这一局还没入账的金币存进钱包。失败时调；续命之后再失败只会把新增的那部分入账，
    /// 所以可以放心调多次。
    /// </summary>
    void BankRun()
    {
        int delta = CoinsThisRun - bankedThisRun;
        if (delta > 0) ProgressManager.AddCoins(delta);
        bankedThisRun = CoinsThisRun;
    }

    void LogRound(bool passed)
    {
        RunLogger.RecordRound(Round, CurrentBallCount, TargetCount,
                              triggeredCount, goldThisRound, passed, CoinsThisRun);
    }

    // ---- 续命 ----

    public int ContinuePrice => endlessMode ? EndlessContinuePrice : StoryContinuePrice;

    /// <summary>现在能不能花钱再点一下：没过关、没用完次数、这关允许、场上还有球、钱够</summary>
    public bool CanContinue
    {
        get
        {
            if (State != GameState.LevelComplete || lastPassed) return false;
            if (continuesUsed >= MaxContinues) return false;
            if (!endlessMode && !level.AllowContinue) return false;
            if (!HasLiveBalls()) return false;
            return ProgressManager.Coins >= ContinuePrice;
        }
    }

    bool HasLiveBalls()
    {
        foreach (var b in AllBalls)
        {
            if (b == null || !b.CountsForTarget) continue;
            if (b.CurrentState == Ball.BallState.Moving || b.CurrentState == Ball.BallState.Fleeing)
                return true;
        }
        return false;
    }

    /// <summary>花钱买一次额外点击。场上剩下的球原地接着跑，已经打中的数不清零</summary>
    public bool TryContinue()
    {
        if (!CanContinue) return false;
        if (!ProgressManager.TrySpendCoins(ContinuePrice)) return false;
        continuesUsed++;
        lastPassed = false;
        State = GameState.WaitingForInput;
        GameUI.Instance.HideResult();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPurchase();
        return true;
    }

    public void NextLevel()
    {
        if (endlessMode)
        {
            Round++;
            StartLevel();
            return;
        }

        if (currentLevel < LevelCatalog.Count)
        {
            currentLevel++;
            StartLevel();
        }
    }

    /// <summary>正式关卡是重玩本关；无尽模式是从第 1 轮重开一整轮，否则最高记录就没意义了</summary>
    public void Retry()
    {
        if (endlessMode)
        {
            Round = 1;
            CoinsThisRun = 0;
            bankedThisRun = 0;
        }
        StartLevel();
    }
}
