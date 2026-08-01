using System;
using UnityEngine;

/// <summary>
/// 无尽模式的难度旋钮。挂在 GameManager 上，全部在 Inspector 里可调，改完不用重新编译。
///
/// 曲线要点：球多不等于难。密度越高连锁越容易自己传下去，所以想变简单就加球，
/// 想变难就提比例。两个旋钮方向是相反的。
///
/// 基准参考正式关卡：第 1 关 3/20 = 15%，最难的第 5 关 16/60 = 27%。
/// </summary>
[Serializable]
public class EndlessConfig
{
    [Header("场上球数（越多越简单：密度高，连锁传得远）")]
    [Tooltip("第 1 轮场上有多少球。球太多会导致目标 10 颗却打出 50 颗，超额大到目标失去意义")]
    public float startBalls = 76f;
    [Tooltip("每过一轮多几颗")]
    public float ballsPerRound = 2.4f;
    [Tooltip("球数上限，再多屏幕会挤")]
    public int maxBalls = 120;

    [Header("通关目标（这才是主要的难度旋钮）")]
    [Tooltip("第 1 轮要打中几颗球")]
    public float startTarget = 8f;
    [Tooltip("每过一轮，目标多几颗。调大 = 更快变难，一轮挑战更短")]
    public float targetPerRound = 2.2f;
    [Tooltip("保险丝：目标最多不超过场上球数的这个比例")]
    public float maxTargetRatio = 0.75f;

    [Header("金球")]
    [Tooltip("每轮随机散落几颗金球")]
    public int goldBallCount = 3;
    [Tooltip("一颗金球值多少金币")]
    public int goldBallReward = 15;

    [Header("金币")]
    [Tooltip("达标之后每多打中一颗球给多少金币")]
    public int coinsPerOverkill = 1;
    [Tooltip("每隔几轮给一次里程碑奖励")]
    public int milestoneEvery = 5;
    [Tooltip("第 N 次里程碑给 N × 这个数")]
    public int milestoneStep = 20;

    public int BallCount(int round)
    {
        return Mathf.Min(Mathf.RoundToInt(startBalls + ballsPerRound * round), maxBalls);
    }

    public int TargetCount(int round)
    {
        int balls = BallCount(round);
        int target = Mathf.RoundToInt(startTarget + targetPerRound * (round - 1));
        int ceiling = Mathf.FloorToInt(balls * maxTargetRatio);
        return Mathf.Clamp(target, 1, Mathf.Max(1, ceiling));
    }

    /// <summary>只是显示用，实际难度由 TargetCount 决定</summary>
    public float TargetRatio(int round)
    {
        return TargetCount(round) / (float)BallCount(round);
    }

    /// <summary>超额奖励：达标之后每多打中一颗算一份。让"打得漂亮"和"够了"不是同一件事</summary>
    public int OverkillReward(int triggered, int target)
    {
        return Mathf.Max(0, triggered - target) * coinsPerOverkill;
    }

    public int MilestoneBonus(int round)
    {
        if (milestoneEvery <= 0 || round % milestoneEvery != 0) return 0;
        return milestoneStep * (round / milestoneEvery);
    }
}
