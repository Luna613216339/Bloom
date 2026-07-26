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

    private int currentLevel;
    private int triggeredCount;
    private int activeReactions;
    private Camera cam;
    private const float GameAspect = 16f / 9f;
    private readonly List<GameObject> screenBounds = new List<GameObject>();

    private LevelConfig Config => levels[currentLevel];
    public int CurrentBallCount => levels != null && levels.Length > 0 ? levels[Mathf.Clamp(currentLevel, 0, levels.Length - 1)].ballCount : 20;

    private static readonly Color[] BallColors =
    {
        new Color(1f, 0.35f, 0.35f),
        new Color(1f, 0.6f, 0.2f),
        new Color(1f, 0.9f, 0.3f),
        new Color(0.4f, 1f, 0.5f),
        new Color(0.3f, 0.8f, 1f),
        new Color(0.7f, 0.5f, 1f),
        new Color(1f, 0.5f, 0.8f),
    };

    void Awake()
    {
        Instance = this;
        cam = Camera.main;
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.white;
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
        activeReactions = 0;
        State = GameState.WaitingForInput;

        bool hasNext = currentLevel < levels.Length - 1 || !string.IsNullOrEmpty(nextScene);
        GameUI.Instance.ShowGameplay(Config.levelName, Config.targetCount, Config.ballCount);
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

        for (int i = 0; i < Config.ballCount; i++)
        {
            float x = UnityEngine.Random.Range(-w / 2f + margin, w / 2f - margin);
            float y = UnityEngine.Random.Range(-h / 2f + margin, h / 2f - margin);
            Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
            Color color = BallColors[i % BallColors.Length];

            Ball.Create(new Vector2(x, y), dir * ballSpeed, color,
                ballSize, reactionMaxScale, reactionDuration);
        }
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

    public void OnBallTriggered()
    {
        triggeredCount++;
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
            bool hasNext = currentLevel < levels.Length - 1 || !string.IsNullOrEmpty(nextScene);

            if (passed)
                ProgressManager.CompleteLevel(globalLevelStart + currentLevel);

            GameUI.Instance.ShowResult(passed, triggeredCount, Config.targetCount, hasNext);
        }
    }

    public void NextLevel()
    {
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

    public void Retry()
    {
        StartLevel();
    }
}
