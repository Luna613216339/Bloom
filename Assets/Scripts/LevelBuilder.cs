using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 把一份 LevelDef 搭进场景：墙、门、笼子、蛇、摆好的球、随机散布的球。
/// 全部挂在一个 LevelRoot 下面，重开一关时整个 root 销毁重建，不用一个个 ResetState。
/// </summary>
public class LevelBuilder
{
    public GameObject Root { get; private set; }
    public List<Door> Doors { get; } = new List<Door>();
    public List<SchoolSpawner> Schools { get; } = new List<SchoolSpawner>();

    readonly LevelDef def;
    readonly List<(Vector2 c, Vector2 half, float rot)> solidRects = new List<(Vector2, Vector2, float)>();

    static readonly Color WallColor = new Color(0.21960784f, 0.21960784f, 0.21960784f);

    LevelBuilder(LevelDef def) { this.def = def; }

    public static LevelBuilder Build(LevelDef def)
    {
        var b = new LevelBuilder(def);
        b.Root = new GameObject("LevelRoot");
        foreach (var w in def.Walls) b.BuildWall(w);
        foreach (var c in def.Cages) b.BuildCage(c);
        for (int i = 0; i < def.Doors.Count; i++) b.BuildDoor(def.Doors[i], i);
        foreach (var s in def.Schools) b.BuildSchool(s);
        return b;
    }

    public void Destroy()
    {
        if (Root != null) Object.Destroy(Root);
        Root = null;
        Doors.Clear();
        Schools.Clear();
    }

    void BuildWall(WallDef w)
    {
        var go = new GameObject("Wall");
        go.transform.SetParent(Root.transform, false);
        go.transform.position = w.Center;
        go.transform.rotation = Quaternion.Euler(0f, 0f, w.Rot);
        go.transform.localScale = new Vector3(w.Size.x, w.Size.y, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteHelper.Square;
        sr.color = Look.Structure(WallColor);
        sr.sortingOrder = 3;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;     // 尺寸全在 transform 的缩放里，别让它去量 sprite
        col.sharedMaterial = Ball.bounceMat;

        solidRects.Add((w.Center, w.Size * 0.5f, w.Rot));
    }

    void BuildCage(CageDef c)
    {
        var go = new GameObject("Cage");
        go.SetActive(false);
        go.transform.SetParent(Root.transform, false);
        go.transform.position = c.Center;
        go.transform.rotation = Quaternion.Euler(0f, 0f, c.Rot);
        var cage = go.AddComponent<Cage>();
        cage.Configure(c.Width, c.Height, c.DoorGap, c.LeftDoor, c.RightDoor, c.Thickness, c.Color);
        go.SetActive(true);
        // 散布球避让用，按整个笼子的外框算（笼子里本来也不该随机撒球）
        solidRects.Add((c.Center, new Vector2(c.Width, c.Height) * 0.5f, c.Rot));
    }

    void BuildDoor(DoorDef d, int index)
    {
        var go = new GameObject("Door" + (index + 1));
        go.SetActive(false);
        go.transform.SetParent(Root.transform, false);
        go.transform.position = d.Center;
        go.transform.rotation = Quaternion.Euler(0f, 0f, d.Rot);
        go.transform.localScale = new Vector3(d.Size.x, d.Size.y, 1f);
        var door = go.AddComponent<Door>();
        door.Configure(d.StartsOpen, d.Color, d.Link, index);
        go.SetActive(true);
        Doors.Add(door);
    }

    void BuildSchool(SchoolDef s)
    {
        var go = new GameObject("School");
        go.transform.SetParent(Root.transform, false);
        var school = go.AddComponent<SchoolSpawner>();
        school.Configure(s);
        Schools.Add(school);
    }

    /// <summary>摆好的球和随机散布的球。蛇由 SchoolSpawner.Init 自己生</summary>
    public void SpawnBalls()
    {
        int colorIndex = 0;
        var palette = GameManager.Instance.Palette;
        var idleBalls = new List<Ball>();
        foreach (var b in def.Balls)
        {
            float size = b.Size > 0f ? b.Size : def.BallSize;
            var ball = Ball.Create(b.Pos, b.Vel, BallPalette.Cycle(palette, colorIndex++),
                size, def.ReactionMaxScale, def.ReactionDuration, def.ExpandSpeed);
            if (b.Vel == Vector2.zero)
            {
                ball.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                idleBalls.Add(ball);
            }
            ApplyKind(ball, b.Kind, b.SplitAxisDeg);
        }

        if (def.Idle != null && def.Idle.Mode != IdleMode.None && idleBalls.Count > 0)
        {
            var go = new GameObject("Idle");
            go.transform.SetParent(Root.transform, false);
            go.AddComponent<IdleGroup>().Configure(def.Idle, idleBalls);
        }

        foreach (var sc in def.Scatter)
        {
            var rng = sc.Seed < 0 ? new System.Random() : new System.Random(sc.Seed);
            float speed = sc.Speed >= 0f ? sc.Speed : def.BallSpeed;
            for (int i = 0; i < sc.Count; i++)
            {
                Vector2 pos = PickFreePoint(sc.Region, rng, def.BallSize * 0.5f);
                float a = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                var vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed;
                var ball = Ball.Create(pos, vel, BallPalette.Cycle(palette, colorIndex++),
                    def.BallSize, def.ReactionMaxScale, def.ReactionDuration, def.ExpandSpeed);
                ApplyKind(ball, sc.Kind, float.NaN);
            }
        }
    }

    static void ApplyKind(Ball ball, BallKind kind, float splitAxis)
    {
        switch (kind)
        {
            case BallKind.Split: ball.SetAsSplitBall(splitAxis); break;
            case BallKind.Hard: ball.SetAsHardBall(); break;
        }
    }

    Vector2 PickFreePoint(Rect region, System.Random rng, float radius)
    {
        for (int attempt = 0; attempt < 64; attempt++)
        {
            float x = Mathf.Lerp(region.xMin, region.xMax, (float)rng.NextDouble());
            float y = Mathf.Lerp(region.yMin, region.yMax, (float)rng.NextDouble());
            var p = new Vector2(x, y);
            if (!InsideSolid(p, radius)) return p;
        }
        return region.center;
    }

    bool InsideSolid(Vector2 p, float radius)
    {
        foreach (var (c, half, rot) in solidRects)
        {
            var local = Quaternion.Euler(0f, 0f, -rot) * (Vector3)(p - c);
            if (Mathf.Abs(local.x) < half.x + radius && Mathf.Abs(local.y) < half.y + radius)
                return true;
        }
        return false;
    }
}
