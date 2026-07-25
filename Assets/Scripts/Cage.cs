using UnityEngine;

public class Cage : MonoBehaviour
{
    [Header("Cage Size")]
    [SerializeField] private float width = 3f;
    [SerializeField] private float height = 4f;

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
        float wallLength = width + wallThickness;
        CreateWallSegment("WallTop", new Vector2(0, height / 2f),
            new Vector2(wallLength, wallThickness));
        CreateWallSegment("WallBottom", new Vector2(0, -height / 2f),
            new Vector2(wallLength, wallThickness));
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

        float wallLength = width + wallThickness;

        Gizmos.color = new Color(wallColor.r, wallColor.g, wallColor.b, 0.8f);
        Gizmos.DrawCube(new Vector3(0, height / 2f, 0),
            new Vector3(wallLength, wallThickness, 0.1f));
        Gizmos.DrawCube(new Vector3(0, -height / 2f, 0),
            new Vector3(wallLength, wallThickness, 0.1f));

        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawWireCube(new Vector3(-width / 2f, 0, 0),
            new Vector3(wallThickness, height, 0.1f));
        Gizmos.DrawWireCube(new Vector3(width / 2f, 0, 0),
            new Vector3(wallThickness, height, 0.1f));

        Gizmos.color = new Color(wallColor.r, wallColor.g, wallColor.b, 0.2f);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(width, height, 0.1f));
    }
}
