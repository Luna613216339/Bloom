using UnityEngine;

/// <summary>
/// 商店货币的显示方式，集中在这里。
///
/// 为什么单独一个类：钱会出现在四个地方（游戏内 HUD、结算页、商店钱包、卡片价签），
/// 之前它们各画各的，才会出现"商店钱包画了个圆、别处用 ◆"这种不一致。
/// 现在只有一个地方知道钱长什么样。
///
/// 币值关系：场上一枚金球 = 10 块钱。这个比例不需要在界面上解释 ——
/// 金币上印着 $，爆开时弹出 $，捡到之后钱包涨 —— 玩家自己会连起来。
/// </summary>
public static class Money
{
    /// <summary>钞票原图 595×354，比例固定，缩放时按这个算宽度</summary>
    const float Aspect = 595f / 354f;

    /// <summary>浅底上的钱数用深藏青（取自钞票的描边色），不要用金色 —— 货币已经不是金币了</summary>
    public static readonly Color Ink = new Color(0.10f, 0.10f, 0.30f);

    /// <summary>深底上的钱数用钞票的浅绿</summary>
    public static readonly Color Bright = new Color(0.66f, 0.85f, 0.47f);

    /// <summary>价签底色，取钞票的浅绿</summary>
    public static readonly Color Chip = new Color(0.66f, 0.85f, 0.47f);

    public static Color InkFor(bool darkBackground) => darkBackground ? Bright : Ink;

    /// <summary>图标 + 数字的总宽度，用来做右对齐或居中</summary>
    public static float Width(int amount, GUIStyle numberStyle, float iconH)
    {
        float gap = iconH * 0.35f;
        return iconH * Aspect + gap + numberStyle.CalcSize(new GUIContent(amount.ToString())).x;
    }

    /// <summary>
    /// 在 (x, y) 处画「钞票 + 数字」，y 是这一行的垂直中心。
    /// 返回画完之后的右边界，方便接着排别的东西。
    /// </summary>
    public static float Draw(float x, float centerY, int amount, GUIStyle numberStyle, float iconH)
    {
        float iconW = iconH * Aspect;
        float gap = iconH * 0.35f;

        var tex = SpriteHelper.Banknote;
        if (tex != null)
            GUI.DrawTexture(new Rect(x, centerY - iconH / 2f, iconW, iconH), tex,
                            ScaleMode.ScaleToFit);

        string n = amount.ToString();
        float nw = numberStyle.CalcSize(new GUIContent(n)).x;
        float nh = numberStyle.CalcSize(new GUIContent(n)).y;
        GUI.Label(new Rect(x + iconW + gap, centerY - nh / 2f, nw + 4f, nh), n, numberStyle);

        return x + iconW + gap + nw;
    }

    /// <summary>右对齐版本：传右边界，自己往左排</summary>
    public static void DrawRightAligned(float right, float centerY, int amount,
                                        GUIStyle numberStyle, float iconH)
    {
        Draw(right - Width(amount, numberStyle, iconH), centerY, amount, numberStyle, iconH);
    }

    /// <summary>居中版本</summary>
    public static void DrawCentered(float centerX, float centerY, int amount,
                                    GUIStyle numberStyle, float iconH)
    {
        Draw(centerX - Width(amount, numberStyle, iconH) / 2f, centerY, amount, numberStyle, iconH);
    }
}
