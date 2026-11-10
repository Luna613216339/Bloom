using UnityEngine;

/// <summary>
/// 外观层：正式关卡里每一样东西长什么样，都从这里取。玩法代码只问"这颗球现在该是什么颜色"，
/// 不自己决定。以后换美术风格只改这一个文件（和 BallPalette 里的色表）。
///
/// 现在是白模：白底，普通球一种中灰，结构深灰；强调色朱红表示"着火 / 在扩散"。
/// 特殊球带颜色：逃跑球蓝、硬球墨绿、分裂球和火星五彩（用户定的）。无尽模式和商店皮肤不走这里，照旧用主题配色。
/// </summary>
public static class Look
{
    /// <summary>白模开关。关掉就回到每关各自的作者配色</summary>
    public static bool Whitebox = true;

    /// <summary>
    /// 定向分裂球的瞄准虚线：从五彩球画出两条点线，照着火星真正会飞的路线描（碰到关着的门和墙会折）。
    /// 关掉就回到"看不见火星往哪飞"的旧样子，关卡照样能过。2026-11-10 Fable 定，要用户试玩确认。
    /// </summary>
    public static bool AimLines = true;

    /// <summary>瞄准虚线的颜色。朱红留给"着火"，虚线用灰</summary>
    public static Color Aim => new Color(0.42f, 0.42f, 0.42f, 0.55f);

    static bool Story => GameManager.Instance == null || !GameManager.Instance.IsEndless;

    /// <summary>此刻该不该按白模画：开关开着，而且不是无尽模式</summary>
    public static bool On => Whitebox && Story;

    /// <summary>普通球的配色。白模里所有普通球同一种中灰</summary>
    public static Color[] Palette(Color[] authored) => On ? BallPalette.Whitebox : authored;

    /// <summary>球被点着、正在膨胀时的颜色</summary>
    public static Color Burn(Color original, bool darkBackground)
    {
        if (On)
        {
            var a = BallPalette.Accent;
            return new Color(a.r, a.g, a.b, 0.72f);
        }
        // 半透明在白底上是"变浅"，在深色底上是"变暗"——后者会把霓虹球的光感抽掉，深色留多一点不透明度
        return new Color(original.r, original.g, original.b, darkBackground ? 0.7f : 0.45f);
    }

    /// <summary>玩家点出来的那个圈：它也是"火"，白模里用同一个强调色</summary>
    public static Color ClickRing(bool darkBackground)
    {
        if (On)
        {
            var a = BallPalette.Accent;
            return new Color(a.r, a.g, a.b, 0.38f);
        }
        float g = darkBackground ? 0.85f : 0.6f;
        return new Color(g, g, g, 0.45f);
    }

    /// <summary>火星的拖尾</summary>
    public static Color SparkTrail => On ? BallPalette.Accent : new Color(1f, 0.6f, 0.2f);

    /// <summary>逃跑球。贴图本身是明暗条纹，这里给的是底色</summary>
    public static Color Escape(Color authored) => On ? BallPalette.WhiteboxEscape : authored;

    /// <summary>硬球</summary>
    public static Color Hard(Color authored) => On ? BallPalette.WhiteboxHard : authored;

    /// <summary>墙、笼子</summary>
    public static Color Structure(Color authored) => On ? BallPalette.WhiteboxStructure : authored;

    /// <summary>门。白模里普通门和联动门颜色一样，靠花纹分（见 SpriteHelper.DoorClosed / DoorLinked）</summary>
    public static Color Door(bool linked, Color authored) => On ? BallPalette.WhiteboxStructure : authored;

    /// <summary>蛇的路径线</summary>
    public static Color Path(Color authored) => On ? new Color(0.5f, 0.5f, 0.5f, 0.22f) : authored;
}
