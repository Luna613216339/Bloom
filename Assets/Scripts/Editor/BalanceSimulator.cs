using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 无尽模式的平衡测试。参数从当前打开场景里的 GameManager 读，
/// 所以流程是：在 Inspector 里改参数 → 跑这个 → 看 Console → 不满意再改。
/// </summary>
public static class BalanceSimulator
{
    const int Rounds = 25;
    const int Trials = 200;         // 每轮跑多少局，越多越准也越慢
    const int GreedySamples = 120;  // "点最密处"时试探多少个候选位置

    [MenuItem("Tools/Bloom/Run Balance Test")]
    static void Run()
    {
        var gm = Object.FindFirstObjectByType<GameManager>();
        if (gm == null)
        {
            EditorUtility.DisplayDialog("Bloom",
                "当前场景里没有 GameManager。\n请先打开 Assets/Scene/Endless.unity 再跑。", "好");
            return;
        }

        var cam = Object.FindFirstObjectByType<Camera>();
        float ortho = cam != null ? cam.orthographicSize : 5f;
        var cfg = gm.EditorEndless;

        var sim = new ChainSimulation(ortho, gm.EditorBallSpeed, gm.EditorBallSize,
            gm.EditorReactionMaxScale, gm.EditorReactionDuration);

        var rows = new (int round, int balls, int target, float rand, float greedy)[Rounds];
        var rng = new System.Random(20260801);

        for (int r = 1; r <= Rounds; r++)
        {
            EditorUtility.DisplayProgressBar("Bloom 平衡测试",
                $"Round {r} / {Rounds}", (float)r / Rounds);

            int balls = cfg.BallCount(r);
            int target = cfg.TargetCount(r);
            int randPass = 0, greedyPass = 0;

            for (int t = 0; t < Trials; t++)
            {
                sim.Spawn(balls, rng);
                sim.RandomPoint(rng, out float rx, out float ry);
                if (sim.Resolve(balls, rx, ry) >= target) randPass++;

                sim.Spawn(balls, rng);
                sim.FindDensestPoint(balls, rng, GreedySamples, out float gx, out float gy);
                if (sim.Resolve(balls, gx, gy) >= target) greedyPass++;
            }

            rows[r - 1] = (r, balls, target,
                randPass / (float)Trials, greedyPass / (float)Trials);
        }

        EditorUtility.ClearProgressBar();
        Report(rows);
    }

    static void Report((int round, int balls, int target, float rand, float greedy)[] rows)
    {
        var log = new StringBuilder();
        var csv = new StringBuilder("round,balls,target,greedy_clear_rate,random_clear_rate\n");

        log.AppendLine($"Bloom 平衡测试 —— 每轮 {Trials} 局");
        log.AppendLine("看【点最密处】那一列，那是真人的水平。随机点击只是参考下限。");
        log.AppendLine("轮次  球数  目标   点最密处   (随机点击)");

        foreach (var row in rows)
        {
            log.AppendLine($"{row.round,3}  {row.balls,4}  {row.target,4}   " +
                           $"{row.greedy,7:P0}   {row.rand,9:P0}");
            csv.AppendLine($"{row.round},{row.balls},{row.target},{row.greedy:F4},{row.rand:F4}");
        }

        log.AppendLine();
        log.AppendLine(SurvivalSummary(rows, r => r.greedy, "点最密处（主要看这个）"));
        log.AppendLine(SurvivalSummary(rows, r => r.rand, "随机点击（参考下限）"));

        float maxGap = 0f;
        foreach (var row in rows) maxGap = Mathf.Max(maxGap, row.greedy - row.rand);
        log.AppendLine();
        log.AppendLine($"两种打法的最大差距：{maxGap:P0}  " +
                       "（差距小 = 点哪里都差不多，这一关没有技术含量；差距大 = 玩家的选择有意义）");

        string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
            "BalanceReport.csv");
        File.WriteAllText(path, csv.ToString());
        log.AppendLine();
        log.AppendLine($"CSV 已写到：{path}");

        Debug.Log(log.ToString());
    }

    /// <summary>
    /// 通关率反推一轮挑战能爬多远：活到第 N 轮的概率 = 前 N-1 轮通关率连乘
    /// </summary>
    static string SurvivalSummary(
        (int round, int balls, int target, float rand, float greedy)[] rows,
        System.Func<(int round, int balls, int target, float rand, float greedy), float> pick,
        string label)
    {
        float survive = 1f;
        float expected = 0f;
        int median = 0;

        foreach (var row in rows)
        {
            survive *= pick(row);
            expected += survive;
            if (median == 0 && survive < 0.5f) median = row.round;
        }

        string medianText = median == 0 ? $"{rows.Length}+ 轮" : $"第 {median} 轮";
        return $"{label}：中位数死在{medianText}，平均通过 {expected:F1} 轮";
    }
}
