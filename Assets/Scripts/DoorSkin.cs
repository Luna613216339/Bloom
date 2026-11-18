using UnityEngine;

/// <summary>
/// 基准皮下的门（Look.Skin）：把门画成会动的造型，对应美术方案 2.6 / 3.6 第二轮的三版（Look.DoorFx）。
///   A 电梯门：两片门板在正中对上，开着各自缩回两侧只露一小截。（第一轮的 A，修掉了中间那道缝）
///   D 伸缩门：一整片从门洞一头伸出来，开着缩回那一头。
///   E 闸刀门：一头有轴，像扳电闸一样扳开 90°。
/// 第一轮的百叶、转门已经删掉。
/// 只是画面。门的碰撞体、开关逻辑、原来那 0.15 秒的缩放都还是 Door 自己管，这里只读 IsOpen。
/// </summary>
public class DoorSkin : MonoBehaviour
{
    const float MoveTime = 0.2f;

    Door door;
    SpriteRenderer baseRenderer;
    float len, thick;          // 门洞的长（沿墙）和宽（墙的厚度）
    float open01;
    Color color;
    LineRenderer[] parts;
    SpriteRenderer pivot;

    public static void Attach(Door door, SpriteRenderer baseRenderer)
    {
        var go = new GameObject("DoorSkin");
        go.transform.SetParent(door.transform.parent, false);
        go.transform.position = door.transform.position;
        var s = door.BaseScale;
        // 局部 x 轴 = 门洞的长边
        go.transform.rotation = door.transform.rotation * Quaternion.Euler(0f, 0f, s.x >= s.y ? 0f : 90f);
        var skin = go.AddComponent<DoorSkin>();
        skin.door = door;
        skin.baseRenderer = baseRenderer;
        skin.len = Mathf.Max(s.x, s.y);
        skin.thick = Mathf.Min(s.x, s.y);
        skin.open01 = door.IsOpen ? 1f : 0f;
        skin.color = Look.Door(door.Link != 0, Color.gray);
        skin.Build();
        skin.Draw();
    }

    LineRenderer Part(float width, bool round)
    {
        var go = new GameObject("Part");
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.sharedMaterial = RingLine.Mat;
        lr.useWorldSpace = false;
        lr.positionCount = 2;
        lr.startWidth = lr.endWidth = width;
        lr.numCapVertices = round ? 6 : 0;
        lr.sortingOrder = 3;
        return lr;
    }

    void Build()
    {
        // 三种都是一整块平头的门板，关着的时候把门洞填满、不留缝
        parts = Look.DoorFx == FxV.A ? new[] { Part(thick, false), Part(thick, false) } : new[] { Part(thick, false) };
        if (Look.DoorFx == FxV.E)
        {
            // 闸刀门的轴：门洞一头一颗小圆钉
            var go = new GameObject("Pivot");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(-len * 0.5f, 0f, 0f);
            go.transform.localScale = Vector3.one * thick * 0.5f;
            pivot = go.AddComponent<SpriteRenderer>();
            pivot.sprite = SpriteHelper.Circle;
            pivot.color = Look.Backdrop;
            pivot.sortingOrder = 4;
        }
    }

    void LateUpdate()
    {
        if (door == null) { Destroy(gameObject); return; }
        baseRenderer.enabled = false;
        float target = door.IsOpen ? 1f : 0f;
        if (!Mathf.Approximately(open01, target))
            open01 = Mathf.MoveTowards(open01, target, Time.deltaTime / MoveTime);
        Draw();
    }

    void Draw()
    {
        Color c = door.Hovered ? Color.Lerp(color, Color.white, 0.3f) : color;
        float h = len * 0.5f;
        float e = door.IsOpen ? Fx.OutCubic(open01) : 1f - Fx.OutCubic(1f - open01);
        float stub = Mathf.Max(thick * 0.45f, len * 0.09f);
        switch (Look.DoorFx)
        {
            case FxV.A:
            {
                // 电梯门：两片门板在正中对上（稍微叠一点，中间不留缝）；开的时候各自缩回墙里，只露一小截
                float leaf = Mathf.Lerp(h + 0.004f, stub, e);
                Seg(parts[0], new Vector2(-h, 0f), new Vector2(-h + leaf, 0f), c);
                Seg(parts[1], new Vector2(h, 0f), new Vector2(h - leaf, 0f), c);
                break;
            }
            case FxV.D:
            {
                // 伸缩门：一整片从一头伸出来顶到另一头；开的时候缩回那一头，只露一小截
                float leaf = Mathf.Lerp(len, stub, e);
                Seg(parts[0], new Vector2(-h, 0f), new Vector2(-h + leaf, 0f), c);
                break;
            }
            default:
            {
                // 闸刀门：一头有轴，像扳电闸一样扳开 90°，开着时贴着门框立在一边（带一点回弹）
                float eb = door.IsOpen ? Fx.OutBack(open01) : 1f - Fx.OutBack(1f - open01);
                float a = eb * 90f * Mathf.Deg2Rad;
                var hinge = new Vector2(-h, 0f);
                Seg(parts[0], hinge, hinge + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * len, c);
                break;
            }
        }
    }

    static void Seg(LineRenderer lr, Vector2 a, Vector2 b, Color c)
    {
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
        lr.startColor = lr.endColor = c;
    }
}
