using UnityEngine;

public class Cage : MonoBehaviour
{
    [Header("Cage Size")]
    [SerializeField] private float width = 3f;
    [SerializeField] private float height = 4f;

    [Header("Door Gap")]
    [SerializeField] private float doorGap = 1.2f;

    [Header("Wall Appearance")]
    [SerializeField] private float wallThickness = 0.15f;
    [SerializeField] private Color wallColor = Color.white;

    void Awake()
    {
        if (Application.isPlaying)
            CreateWalls();
    }

    void CreateWalls()
    {
        float fullWidth = width + wallThickness;

        CreateWallSegment("WallTop", new Vector2(0, height / 2f),
            new Vector2(fullWidth, wallThickness));
        CreateWallSegment("WallBottom", new Vector2(0, -height / 2f),
            new Vector2(fullWidth, wallThickness));

        float sideSegment = (height - doorGap) / 2f;
        float segOffset = (doorGap + sideSegment) / 2f;

        CreateWallSegment("WallLeftUpper", new Vector2(-width / 2f, segOffset),
            new Vector2(wallThickness, sideSegment));
        CreateWallSegment("WallLeftLower", new Vector2(-width / 2f, -segOffset),
            new Vector2(wallThickness, sideSegment));

        CreateWallSegment("WallRightUpper", new Vector2(width / 2f, segOffset),
            new Vector2(wallThickness, sideSegment));
        CreateWallSegment("WallRightLower", new Vector2(width / 2f, -segOffset),
            new Vector2(wallThickness, sideSegment));
    }

    void CreateWallSegment(string name, Vector2 localPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteHelper.Square;
        sr.color = wallColor;
        sr.sortingOrder = 3;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        go.AddComponent<BoxCollider2D>();
    }

    void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        float fullWidth = width + wallThickness;
        float sideSegment = (height - doorGap) / 2f;
        float segOffset = (doorGap + sideSegment) / 2f;

        Gizmos.color = new Color(wallColor.r, wallColor.g, wallColor.b, 0.8f);

        Gizmos.DrawCube(new Vector3(0, height / 2f, 0),
            new Vector3(fullWidth, wallThickness, 0.1f));
        Gizmos.DrawCube(new Vector3(0, -height / 2f, 0),
            new Vector3(fullWidth, wallThickness, 0.1f));

        Gizmos.DrawCube(new Vector3(-width / 2f, segOffset, 0),
            new Vector3(wallThickness, sideSegment, 0.1f));
        Gizmos.DrawCube(new Vector3(-width / 2f, -segOffset, 0),
            new Vector3(wallThickness, sideSegment, 0.1f));
        Gizmos.DrawCube(new Vector3(width / 2f, segOffset, 0),
            new Vector3(wallThickness, sideSegment, 0.1f));
        Gizmos.DrawCube(new Vector3(width / 2f, -segOffset, 0),
            new Vector3(wallThickness, sideSegment, 0.1f));

        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawWireCube(new Vector3(-width / 2f, 0, 0),
            new Vector3(wallThickness, doorGap, 0.1f));
        Gizmos.DrawWireCube(new Vector3(width / 2f, 0, 0),
            new Vector3(wallThickness, doorGap, 0.1f));
    }
}
