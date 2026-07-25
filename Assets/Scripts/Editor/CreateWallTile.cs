using UnityEditor;
using UnityEngine;

public class CreateWallTile
{
    [MenuItem("Tools/Bloom/Create Wall Sprite")]
    public static void Create()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art"))
            AssetDatabase.CreateFolder("Assets", "Art");

        var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        var wallColor = new Color(0.25f, 0.25f, 0.3f);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
                tex.SetPixel(x, y, wallColor);
        tex.Apply();

        string path = "Assets/Art/WallSprite.png";
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.Refresh();

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.SaveAndReimport();

        Debug.Log("Wall sprite created at " + path + ". Drag it into a Tile Palette to start painting walls.");
    }
}
