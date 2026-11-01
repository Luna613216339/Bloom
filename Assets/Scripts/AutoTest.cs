using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 用真实游戏代码自动跑关卡。给 AI 验证"每一关过得去"和"解谜关不会被随手一点就过掉"用。
///
/// 用法（打一个 macOS 包，然后无头运行）：
///   Bloom.app/Contents/MacOS/Bloom -batchmode -nographics -autotest plan.json -autoout result.json
///
/// 每帧固定推进 1/60 秒（Time.captureDeltaTime），所以跑多快结果都一样，而且不受机器性能影响。
/// 正式玩家永远碰不到这段：没有 -autotest 参数它什么都不做。
/// </summary>
public static class AutoTest
{
    public static bool Active { get; private set; }

    [Serializable] public class Plan { public Job[] jobs; }

    [Serializable]
    public class Job
    {
        public string name;
        public int level;
        public Act[] actions;       // 脚本化的一次尝试
        public Sweep sweep;         // 或者：扫一遍网格点击
        public float maxTime = 12f;
    }

    /// <summary>type = "click"（x, y）或 "door"（door = 建关顺序里第几扇，0 起）</summary>
    [Serializable]
    public class Act
    {
        public float t;
        public string type = "click";
        public float x, y;
        public int door;
    }

    [Serializable]
    public class Sweep
    {
        public int nx = 12, ny = 7;
        public float[] times = { 0.5f };
        public Act[] pre;           // 每次尝试前先做的事（比如开某扇门）
    }

    [Serializable]
    public class Result
    {
        public string name;
        public int level;
        public string id;
        public int target;
        public int total;
        public int triggered;
        public bool passed;
        public float time;
        public int trials;
        public int passes;
        public float passRate;
        public int best;
        public float bestX, bestY, bestT;
        public string[] heat;
    }

    [Serializable] public class Report { public Result[] results; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        string planPath = Arg("-autotest");
        if (string.IsNullOrEmpty(planPath)) return;
        if (Active) return;

        Active = true;
        Time.captureDeltaTime = 1f / 60f;
        var go = new GameObject("AutoTestDriver");
        UnityEngine.Object.DontDestroyOnLoad(go);
        var driver = go.AddComponent<Driver>();
        driver.planPath = planPath;
        driver.outPath = Arg("-autoout") ?? "autotest-result.json";
    }

    static string Arg(string key)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == key) return args[i + 1];
        return null;
    }

    class Driver : MonoBehaviour
    {
        public string planPath;
        public string outPath;
        readonly List<Result> results = new List<Result>();

        void Start() => StartCoroutine(RunAll());

        IEnumerator RunAll()
        {
            Plan plan;
            try
            {
                plan = JsonUtility.FromJson<Plan>(File.ReadAllText(planPath));
            }
            catch (Exception e)
            {
                Debug.LogError("[AutoTest] 读不了计划文件: " + e.Message);
                Quit(2);
                yield break;
            }

            foreach (var job in plan.jobs)
            {
                var def = LevelCatalog.Get(job.level);
                var r = new Result { name = job.name, level = job.level, id = def?.Id, target = def?.Target ?? 0,
                    total = def?.TotalBalls ?? 0 };
                if (def == null)
                {
                    Debug.LogError($"[AutoTest] 没有第 {job.level} 关");
                    results.Add(r);
                    continue;
                }

                if (job.sweep != null && job.sweep.nx > 0)
                    yield return RunSweep(job, r);
                else
                    yield return RunScripted(job, r);

                results.Add(r);
                Debug.Log($"[AutoTest] {r.name}: level {r.level} ({r.id}) " +
                          (job.sweep != null && job.sweep.nx > 0
                              ? $"pass {r.passes}/{r.trials} = {r.passRate:P0}, best {r.best}/{r.target} at ({r.bestX:F1},{r.bestY:F1}) t={r.bestT:F1}"
                              : $"{(r.passed ? "PASS" : "FAIL")} {r.triggered}/{r.target} in {r.time:F1}s"));
            }

            var report = new Report { results = results.ToArray() };
            File.WriteAllText(outPath, JsonUtility.ToJson(report, true));
            Debug.Log("[AutoTest] 写出 " + outPath);
            Quit(0);
        }

        IEnumerator RunScripted(Job job, Result r)
        {
            var acts = new List<Act>(job.actions ?? new Act[0]);
            yield return RunTrial(job.level, acts, job.maxTime, r);
        }

        IEnumerator RunSweep(Job job, Result r)
        {
            var sw = job.sweep;
            float x0 = -8.4f, x1 = 8.4f, y0 = -4.6f, y1 = 4.6f;
            var heat = new List<string>();
            r.best = -1;

            foreach (float t in sw.times)
            {
                heat.Add($"t={t:F1}");
                for (int iy = sw.ny - 1; iy >= 0; iy--)
                {
                    var row = new System.Text.StringBuilder();
                    for (int ix = 0; ix < sw.nx; ix++)
                    {
                        float x = sw.nx == 1 ? 0f : Mathf.Lerp(x0, x1, ix / (float)(sw.nx - 1));
                        float y = sw.ny == 1 ? 0f : Mathf.Lerp(y0, y1, iy / (float)(sw.ny - 1));
                        var acts = new List<Act>(sw.pre ?? new Act[0]);
                        acts.Add(new Act { t = t, type = "click", x = x, y = y });
                        var one = new Result();
                        yield return RunTrial(job.level, acts, job.maxTime, one);

                        r.trials++;
                        if (one.passed) r.passes++;
                        if (one.triggered > r.best)
                        {
                            r.best = one.triggered; r.bestX = x; r.bestY = y; r.bestT = t;
                        }
                        float ratio = r.target > 0 ? one.triggered / (float)r.target : 0f;
                        row.Append(one.passed ? '#' : (char)('0' + Mathf.Clamp(Mathf.FloorToInt(ratio * 10f), 0, 9)));
                    }
                    heat.Add(row.ToString());
                }
            }

            r.passRate = r.trials > 0 ? r.passes / (float)r.trials : 0f;
            r.heat = heat.ToArray();
        }

        IEnumerator RunTrial(int level, List<Act> acts, float maxTime, Result r)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.IsEndless || gm.GlobalLevel != level
                || SceneManager.GetActiveScene().name != "Level")
            {
                GameManager.RequestedStartLevel = level;
                SceneManager.LoadScene("Level");
                yield return null;
                yield return null;
                gm = GameManager.Instance;
            }
            else
            {
                gm.Retry();
                yield return null;
            }

            acts.Sort((a, b) => a.t.CompareTo(b.t));
            float t = 0f;
            int next = 0;
            while (true)
            {
                while (next < acts.Count && acts[next].t <= t)
                {
                    var a = acts[next++];
                    if (a.type == "door") gm.ToggleDoor(a.door);
                    else gm.HandlePointer(new Vector2(a.x, a.y));
                }
                if (gm.State == GameManager.GameState.LevelComplete) break;
                if (t > maxTime) break;
                yield return null;
                t += Time.deltaTime;
            }

            r.triggered = gm.TriggeredCount;
            r.target = gm.TargetCount;
            r.passed = gm.State == GameManager.GameState.LevelComplete && gm.TriggeredCount >= gm.TargetCount;
            r.time = t;
        }

        static void Quit(int code)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(code);
#endif
        }
    }
}
