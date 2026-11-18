using UnityEngine;

/// <summary>
/// 基准皮下的门（Look.Skin）：把门画成会动的造型，三种对应美术方案 2.6 / 3.6 的 A、B、C。
///   A 伸缩门叶：关着是两片对上的门叶，开着各自缩回两侧只露一小截。
///   B 百叶：关着是一排短条把门洞填满，开着全收到一侧叠成一摞。
///   C 转门：一根横杆加一个轴，关着横在门洞上，开着转 90° 顺着通道立着。
/// 只是画面。门的碰撞体、开关逻辑、原来那 0.15 秒的缩放都还是 Door 自己管，这里只读 IsOpen。
/// </summary>
public class DoorSkin : MonoBehaviour
{
    const float MoveTime = 0.2f;
    const int Slats = 6;

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
        switch (Look.DoorFx)
        {
            case FxV.A:
                parts = new[] { Part(thick, false), Part(thick, false) };
                break;
            case FxV.B:
                parts = new LineRenderer[Slats];
                for (int i = 0; i < Slats; i++) parts[i] = Part(len / Slats * 0.5f, true);
                break;
            default:
                parts = new[] { Part(thick * 0.5f, true) };
                var go = new GameObject("Pivot");
                go.transform.SetParent(transform, false);
                go.transform.localScale = Vector3.one * thick * 0.9f;
                pivot = go.AddComponent<SpriteRenderer>();
                pivot.sprite = SpriteHelper.Circle;
                pivot.color = color;
                pivot.sortingOrder = 4;
                // 轴画成一颗带纸色芯的小圈，免得被看成一颗球
                var hole = new GameObject("PivotHole");
                hole.transform.SetParent(go.transform, false);
                hole.transform.localScale = Vector3.one * 0.5f;
                var hs = hole.AddComponent<SpriteRenderer>();
                hs.sprite = SpriteHelper.Circle;
                hs.color = Look.Backdrop;
                hs.sortingOrder = 5;
                break;
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
        switch (Look.DoorFx)
        {
            case FxV.A:
            {
                // 两片平头的门叶：关着时在正中对上、留一道细缝；开着时各缩回去只露一小截
                float e = Fx.OutCubic(open01);
                float stub = Mathf.Max(thick * 0.45f, len * 0.09f);
                float leaf = Mathf.Lerp(h - 0.025f, stub, door.IsOpen ? e : 1f - Fx.OutCubic(1f - open01));
                Seg(parts[0], new Vector2(-h, 0f), new Vector2(-h + leaf, 0f), c);
                Seg(parts[1], new Vector2(h, 0f), new Vector2(h - leaf, 0f), c);
                break;
            }
            case FxV.B:
            {
                float w = len / Slats * 0.5f, hy = Mathf.Max(0.001f, thick * 0.5f - w * 0.5f);
                for (int i = 0; i < Slats; i++)
                {
                    // 离收拢那一侧越远的条越先动
                    float lag = (Slats - 1 - i) * 0.06f;
                    float e = Fx.InOut(Mathf.Clamp01(open01 * (1f + 0.06f * Slats) - (door.IsOpen ? lag : (i * 0.06f))));
                    float closedX = -h + (i + 0.5f) * len / Slats;
                    float openX = -h + w * 0.6f + i * w * 0.62f;
                    float x = Mathf.Lerp(closedX, openX, e);
                    Seg(parts[i], new Vector2(x, -hy), new Vector2(x, hy), c);
                }
                break;
            }
            default:
            {
                float e = door.IsOpen ? Fx.OutBack(open01) : 1f - Fx.OutBack(1f - open01);
                float a = e * 90f * Mathf.Deg2Rad;
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (h - thick * 0.25f);
                Seg(parts[0], -d, d, c);
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
