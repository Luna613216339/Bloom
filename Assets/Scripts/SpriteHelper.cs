using UnityEngine;

public static class SpriteHelper
{
    private static Sprite _circle;
    private static Sprite _square;

    public static Sprite Circle
    {
        get
        {
            if (_circle == null)
                _circle = CreateCircleSprite(64);
            return _circle;
        }
    }

    public static Sprite Square
    {
        get
        {
            if (_square == null)
                _square = CreateSquareSprite(32);
            return _square;
        }
    }

    static Sprite CreateCircleSprite(int resolution)
    {
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        float center = resolution / 2f;
        float radius = center - 1f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(radius - dist);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), Vector2.one * 0.5f, resolution);
    }

    static Sprite CreateSquareSprite(int resolution)
    {
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        for (int y = 0; y < resolution; y++)
            for (int x = 0; x < resolution; x++)
                tex.SetPixel(x, y, Color.white);

        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), Vector2.one * 0.5f, resolution);
    }
}
