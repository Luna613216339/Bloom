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
    static bool verbose;
    static bool trace;
    static string shotDir;

    /// <summary>自动测试里的假钱包：不碰真实存档（编辑器和包共用同一份 PlayerPrefs），又能让续命按钮出现</summary>
    public static int FakeCoins = 999;

    [Serializable] public class Plan { public Job[] jobs; }

    [Serializable]
    public class Job
    {
        public string name;
        public int level;
        public string id;           // 填了就按关卡 id 找关（关号会随增删关变，id 不变），优先于 level
        public Act[] actions;       // 脚本化的一次尝试
        public Sweep sweep;         // 或者：扫一遍网格点击
        public float maxTime = 12f;
    }

    /// <summary>
    /// type = "click"（x, y）、"door"（door = 建关顺序里第几扇，0 起）、
    /// "shot"（截图到 -autoshots 目录，文件名 name；只在带窗口运行时有图）
    /// </summary>
    [Serializable]
    public class Act
    {
        public float t;
        public string type = "click";
        public float x, y;
        public int door;
        public string name;
    }

    [Serializable]
    public class Sweep
    {
        public int nx, ny;              // 0 = 这个 job 不是扫格（JsonUtility 会把缺省的对象字段建成默认实例，所以不能靠 null）
        public float[] times = { 0.5f };
        public Act[] pre;           // 每次尝试前先做的事（比如开某扇门）
        public float cx, cy, w, h;  // 只扫一块区域：中心和宽高。w 为 0 = 全屏
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
        // 带窗口跑截图时窗口拿不到焦点，不开这个游戏循环会停在那里等焦点
        Application.runInBackground = true;
        var go = new GameObject("AutoTestDriver");
        UnityEngine.Object.DontDestroyOnLoad(go);
        var driver = go.AddComponent<Driver>();
        driver.planPath = planPath;
        driver.outPath = Arg("-autoout") ?? "autotest-result.json";
        verbose = Array.IndexOf(Environment.GetCommandLineArgs(), "-autoverbose") >= 0;
        trace = Array.IndexOf(Environment.GetCommandLineArgs(), "-autotrace") >= 0;
        shotDir = Arg("-autoshots");
        if (!string.IsNullOrEmpty(shotDir)) Directory.CreateDirectory(shotDir);
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
                if (!string.IsNullOrEmpty(job.id))
                {
                    job.level = 0;
                    foreach (var l in LevelCatalog.All) if (l.Id == job.id) job.level = l.Number;
                    if (job.level == 0)
                    {
                        Debug.LogError($"[AutoTest] 没有 id 为 {job.id} 的关");
                        results.Add(new Result { name = job.name, id = job.id });
                        continue;
                    }
                }
                if (job.level <= 0)
                {
                    // 0 = 主菜单截图，-1 = 无尽模式冒烟（点一下看一轮能不能正常结算），-2 = 商店截图
                    if (job.level == -1) yield return RunEndless(job);
                    else yield return RunMenu(job, job.level == -2 ? "Shop" : "MainMenu");
                    continue;
                }
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

        /// <summary>无尽模式冒烟：进场、点一下、等结算。过了会停在结算面板（自动测试里不自动进下一轮）</summary>
        IEnumerator RunEndless(Job job)
        {
            var r = new Result { name = job.name, level = -1, id = "endless" };
            SceneManager.LoadScene("Endless");
            yield return null;
            yield return null;
            var gm = GameManager.Instance;
            var acts = new List<Act>(job.actions ?? new Act[0]);
            acts.Sort((a, b) => a.t.CompareTo(b.t));
            int resultBefore = gm.ResultCounter;
            float t = 0f;
            int next = 0;
            bool finished = false;
            while (t < job.maxTime)
            {
                while (next < acts.Count && acts[next].t <= t)
                {
                    var a = acts[next++];
                    if (a.type == "shot") Shot(a.name);
                    else if (a.type == "continue")
                    {
                        bool ok = gm.TryContinue();
                        Debug.Log($"[AutoTest] endless continue at t={t:F1}: {(ok ? "ok" : "refused")} (coins={ProgressManager.Coins})");
                        resultBefore = gm.ResultCounter;    // 续命之后等的是下一次结算
                    }
                    else gm.HandlePointer(new Vector2(a.x, a.y));
                }
                if (gm.ResultCounter != resultBefore && next >= acts.Count) { finished = true; break; }
                yield return null;
                t += Time.deltaTime;
            }
            r.triggered = gm.LastResultTriggered;
            r.target = gm.LastResultTarget;
            r.total = gm.CurrentBallCount;
            r.passed = finished && gm.LastResultPassed;
            r.time = t;
            results.Add(r);
            Debug.Log($"[AutoTest] {r.name}: endless round {gm.Round} {(finished ? (r.passed ? "PASS" : "fail") : "unfinished")} {r.triggered}/{r.target}, coins this run {gm.CoinsThisRun}");
        }

        /// <summary>level = 0 / -2：只是打开主菜单 / 商店截个图</summary>
        IEnumerator RunMenu(Job job, string scene)
        {
            SceneManager.LoadScene(scene);
            yield return null;
            yield return null;
            float t = 0f;
            var acts = new List<Act>(job.actions ?? new Act[0]);
            acts.Sort((a, b) => a.t.CompareTo(b.t));
            int next = 0;
            while (t < job.maxTime)
            {
                while (next < acts.Count && acts[next].t <= t)
                {
                    var a = acts[next++];
                    if (a.type == "shot") Shot(a.name);
                }
                if (next >= acts.Count) break;
                yield return null;
                t += Time.deltaTime;
            }
            yield return null;
            yield return null;
        }

        static void Shot(string name)
        {
            if (string.IsNullOrEmpty(shotDir)) return;
            ScreenCapture.CaptureScreenshot(Path.Combine(shotDir, name + ".png"));
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
            if (sw.w > 0f) { x0 = sw.cx - sw.w / 2f; x1 = sw.cx + sw.w / 2f; y0 = sw.cy - sw.h / 2f; y1 = sw.cy + sw.h / 2f; }
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
                        // 只有一列 / 一行时取区域中心（全屏时就是 0），否则定点扫会忽略 cx / cy
                        float x = sw.nx == 1 ? (x0 + x1) / 2f : Mathf.Lerp(x0, x1, ix / (float)(sw.nx - 1));
                        float y = sw.ny == 1 ? (y0 + y1) / 2f : Mathf.Lerp(y0, y1, iy / (float)(sw.ny - 1));
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

            if (verbose)
            {
                var sb = new System.Text.StringBuilder($"[AutoTest] level {gm.GlobalLevel} ready, {gm.AllBalls.Count} balls:");
                foreach (var b in gm.AllBalls)
                    sb.Append($" {b.Kind.ToString()[0]}({b.transform.position.x:F2},{b.transform.position.y:F2})");
                Debug.Log(sb.ToString());
            }

            acts.Sort((a, b) => a.t.CompareTo(b.t));
            int resultBefore = gm.ResultCounter;
            float t = 0f;
            int next = 0;
            bool finished = false;
            while (true)
            {
                while (next < acts.Count && acts[next].t <= t)
                {
                    var a = acts[next++];
                    if (a.type == "door") gm.ToggleDoor(a.door);
                    else if (a.type == "shot") Shot(a.name);
                    else if (a.type == "continue")
                    {
                        bool ok = gm.TryContinue();
                        if (verbose) Debug.Log($"[AutoTest] continue at t={t:F1}: {(ok ? "ok" : "refused")}");
                        resultBefore = gm.ResultCounter;    // 续命之后等的是下一次结算
                    }
                    else gm.HandlePointer(new Vector2(a.x, a.y));
                }
                // 还有截图没拍完、或者后面还要续命，就先别收：结算面板也要拍到
                bool pending = next < acts.Count && (acts[next].type == "shot" || acts[next].type == "continue");
                if (gm.ResultCounter != resultBefore && !pending) { finished = true; break; }
                if (t > maxTime) break;
                if (trace && Time.frameCount % 6 == 0)
                {
                    var sb = new System.Text.StringBuilder($"[Trace] t={t:F2}");
                    foreach (var d in FindObjectsByType<Door>(FindObjectsSortMode.None))
                    {
                        var dc = d.GetComponent<BoxCollider2D>();
                        var drb = d.GetComponent<Rigidbody2D>();
                        sb.Append($" door{d.Index}:{(d.IsOpen ? "open" : "closed")}@({d.transform.position.x:F2},{d.transform.position.y:F2}) trig={dc.isTrigger} en={dc.enabled} rb={(dc.attachedRigidbody != null)} rbType={drb.bodyType} sim={drb.simulated} b={dc.bounds.size.x:F2}x{dc.bounds.size.y:F2} layer={d.gameObject.layer}");
                    }
                    foreach (var b in gm.AllBalls)
                        if (b.Kind == BallKind.Fragment || gm.AllBalls.Count <= 24)
                        {
                            var rb = b.GetComponent<Rigidbody2D>();
                            sb.Append($" frag({b.transform.position.x:F2},{b.transform.position.y:F2}) v=({rb.linearVelocity.x:F1},{rb.linearVelocity.y:F1}) {b.CurrentState} {rb.bodyType}");
                        }
                    Debug.Log(sb.ToString());
                }
                yield return null;
                t += Time.deltaTime;
            }

            r.triggered = finished ? gm.LastResultTriggered : gm.TriggeredCount;
            r.target = finished ? gm.LastResultTarget : gm.TargetCount;
            r.passed = finished && gm.LastResultPassed;
            if (verbose)
            {
                var sb = new System.Text.StringBuilder("[AutoTest] trial:");
                foreach (var a in acts) sb.Append(a.type == "door" ? $" door{a.door}@{a.t:F1}" : $" click({a.x:F1},{a.y:F1})@{a.t:F1}");
                sb.Append($" → {r.triggered}/{r.target} {(finished ? (r.passed ? "PASS" : "fail") : "unfinished")} t={t:F1}");
                Debug.Log(sb.ToString());
            }
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
