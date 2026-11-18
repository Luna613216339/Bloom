using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 硬球的壳（原型规则和基准皮下用，见 Look.HardOverlay）。只是画面：读 Ball 告诉它"掉了一层"，把那一层拿掉、炸出碎石。
/// 球自己画成一颗和普通球一样大的芯，壳一层层套在外面，所以硬球比普通球大一圈 —— 一眼看得出"厚"。
///
/// 三种造型（Look.HardLook，美术方案 2.2 第二轮）：
///   A 厚壳：每一层是一圈实心厚环，还剩几圈就还要再挨几下（最里面那颗芯算最后一下）。
///   B 棱角壳：同样一层层套，但壳的外沿是六边形 —— 满屏都是圆，只有它带棱角。
///   C 甲片：只有一圈壳，切成几片，还剩几片就还要挨几下（最后一片掉的时候球一起炸）。
/// 「两面夹」规则下不管哪种造型，壳都是沿缝分开的两片，火从哪边来掉哪片。
/// </summary>
public class HardShell : MonoBehaviour
{
    class Piece { public MeshRenderer mr; public float start, sweep, inner, outer; public bool hexOuter; public int side; }

    readonly List<Piece> pieces = new List<Piece>();
    Ball ball;
    Color color;

    const float Gap = 0.02f;        // 芯和壳、壳和壳之间那道缝（世界单位）
    const float Thick = 0.09f;      // 一层壳的厚度

    public static HardShell Attach(Ball ball, int hits, Color shellColor)
    {
        var go = new GameObject("HardShell");
        go.transform.position = ball.transform.position;
        var s = go.AddComponent<HardShell>();
        s.ball = ball;
        s.color = shellColor;
        s.Build(hits, ball.Scale * 0.5f);
        return s;
    }

    void Build(int hits, float r)
    {
        bool hex = Look.HardLook == FxV.B;
        if (Look.HardRule == HardRuleKind.Sides)
        {
            // 两片：缝沿 seam 方向，0 号片在缝的法线正方向那一侧（和 Ball.HardHit 里分侧的算法一致）
            float seam = Look.HardSeamDeg, cut = 7f;
            Add(seam + cut, 180f - 2f * cut, r + Gap, r + Gap + Thick * 1.15f, false, hex, 0);
            Add(seam + 180f + cut, 180f - 2f * cut, r + Gap, r + Gap + Thick * 1.15f, false, hex, 1);
            return;
        }
        if (Look.HardLook == FxV.C)
        {
            // 甲片：hits 片，竖着的缝在正上方开始排，每片之间留一道缝
            float each = 360f / hits, cut = hits == 2 ? 7f : 8f;
            for (int i = 0; i < hits; i++)
                Add(90f + each * i + cut, each - 2f * cut, r + Gap, r + Gap + Thick * 1.2f, false, false, -1);
            return;
        }
        // 厚壳 / 棱角壳：hits - 1 层，从里往外
        float inner = r + Gap;
        for (int k = 0; k < hits - 1; k++)
        {
            float outer = inner + Thick;
            Add(0f, 360f, inner, outer, hex && k > 0, hex, -1);
            inner = outer + Gap;
        }
    }

    void Add(float start, float sweep, float inner, float outer, bool hexInner, bool hexOuter, int side)
    {
        var mr = Shapes.Band(transform, "Shell", start, sweep, inner, outer, hexInner, hexOuter, color, 2);
        pieces.Add(new Piece { mr = mr, start = start, sweep = sweep, inner = inner, outer = outer, hexOuter = hexOuter, side = side });
    }

    void LateUpdate()
    {
        if (ball == null) { Destroy(gameObject); return; }
        transform.position = ball.transform.position;
    }

    /// <summary>
    /// 掉一层。from 是打它的那把火的位置；side ≥ 0 时指定掉哪一侧的那片（两面夹规则）。
    /// all 为真 = 球要炸了，剩下的壳一起碎掉。
    /// </summary>
    public void Break(Vector2 from, int side, bool all)
    {
        Vector2 pos = transform.position;
        Vector2 d = from - pos;
        float fireDeg = d.sqrMagnitude < 0.0001f ? 90f : Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        while (pieces.Count > 0)
        {
            int pick = pieces.Count - 1;                // 默认掉最外面那一层
            if (side >= 0) { pick = pieces.FindIndex(p => p.side == side); if (pick < 0) break; }
            else if (pieces[pick].sweep < 359f)
            {
                // 甲片：掉离火最近的那一片
                float best = float.MaxValue;
                for (int i = 0; i < pieces.Count; i++)
                {
                    float mid = pieces[i].start + pieces[i].sweep * 0.5f;
                    float diff = Mathf.Abs(Mathf.DeltaAngle(mid, fireDeg));
                    if (diff < best) { best = diff; pick = i; }
                }
            }
            var pc = pieces[pick];
            pieces.RemoveAt(pick);
            Fx.ShellDebris(pos, pc.start, pc.sweep, pc.inner, pc.outer, color, d);
            Destroy(pc.mr.gameObject);
            if (!all) break;
            side = -1;
        }
    }
}
