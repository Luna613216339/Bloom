using UnityEngine;

public class Cage : MonoBehaviour
{
    [Header("Cage Size")]
    [SerializeField] private float width = 3f;
    [SerializeField] private float height = 4f;

    [Header("Door Gaps")]
    [SerializeField] private float doorGap = 1.2f;
    [SerializeField] private bool hasLeftDoor = true;
    [SerializeField] private bool hasRightDoor = true;

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

        CreateSide("Left", -width / 2f, hasLeftDoor);
        CreateSide("Right", width / 2f, hasRightDoor);
    }

    void CreateSide(string label, float x, bool hasDoor)
    {
        if (hasDoor)
        {
            float sideSegment = (height - doorGap) / 2f;
            float segOffset = (doorGap + sideSegment) / 2f;
            CreateWallSegment("Wall" + label + "Upper", new Vector2(x, segOffset),
                new Vector2(wallThickness, sideSegment));
            CreateWallSegment("Wall" + label + "Lower", new Vector2(x, -segOffset),
                new Vector2(wallThickness, sideSegment));
        }
        else
        {
            CreateWallSegment("Wall" + label, new Vector2(x, 0),
                new Vector2(wallThickness, height));
        }
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

        var col = go.AddComponent<BoxCollider2D>();
        col.sharedMaterial = Ball.bounceMat;
    }

    void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        float fullWidth = width + wallThickness;

        Gizmos.color = new Color(wallColor.r, wallColor.g, wallColor.b, 0.8f);

        Gizmos.DrawCube(new Vector3(0, height / 2f, 0),
            new Vector3(fullWidth, wallThickness, 0.1f));
        Gizmos.DrawCube(new Vector3(0, -height / 2f, 0),
            new Vector3(fullWidth, wallThickness, 0.1f));

        DrawSideGizmo(-width / 2f, hasLeftDoor);
        DrawSideGizmo(width / 2f, hasRightDoor);
    }

    void DrawSideGizmo(float x, bool hasDoor)
    {
        if (hasDoor)
        {
            float sideSegment = (height - doorGap) / 2f;
            float segOffset = (doorGap + sideSegment) / 2f;
            Gizmos.color = new Color(wallColor.r, wallColor.g, wallColor.b, 0.8f);
            Gizmos.DrawCube(new Vector3(x, segOffset, 0),
                new Vector3(wallThickness, sideSegment, 0.1f));
            Gizmos.DrawCube(new Vector3(x, -segOffset, 0),
                new Vector3(wallThickness, sideSegment, 0.1f));

            Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
            Gizmos.DrawWireCube(new Vector3(x, 0, 0),
                new Vector3(wallThickness, doorGap, 0.1f));
        }
        else
        {
            Gizmos.color = new Color(wallColor.r, wallColor.g, wallColor.b, 0.8f);
            Gizmos.DrawCube(new Vector3(x, 0, 0),
                new Vector3(wallThickness, height, 0.1f));
        }
    }
}
