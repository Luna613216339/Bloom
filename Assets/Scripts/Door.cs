using UnityEngine;

[ExecuteAlways]
public class Door : MonoBehaviour
{
    [Header("Door Settings")]
    [SerializeField] private bool startsOpen = true;
    [SerializeField] private Color closedColor = new Color(0.35f, 0.35f, 0.35f);
    [SerializeField] private Color openColor = new Color(0.5f, 0.5f, 0.5f, 0.03f);

    private bool isOpen;
    private SpriteRenderer sr;
    private BoxCollider2D col;
    private Vector3 baseScale;
    private bool isHovered;

    private const float HoverScaleMultiplier = 1.1f;
    private const float HoverBrightness = 0.15f;
    private const float HoverAlphaBoost = 0.1f;

    void Awake()
    {
        EnsureComponents();
        isOpen = startsOpen;
        baseScale = transform.localScale;
        ApplyVisual();
    }

    void EnsureComponents()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
        }

        col = GetComponent<BoxCollider2D>();
        if (col == null)
            col = gameObject.AddComponent<BoxCollider2D>();

        var rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
    }

    public bool ContainsPoint(Vector2 point)
    {
        return col.OverlapPoint(point);
    }

    public void Toggle()
    {
        isOpen = !isOpen;
        ApplyVisual();
    }

    public void ResetState()
    {
        isOpen = startsOpen;
        ApplyVisual();
    }

    void OnMouseEnter()
    {
        if (!Application.isPlaying || isHovered) return;
        if (GameManager.Instance == null) return;
        var state = GameManager.Instance.State;
        if (state != GameManager.GameState.WaitingForInput &&
            state != GameManager.GameState.ChainReacting) return;
        isHovered = true;
        baseScale = transform.localScale;
        transform.localScale = baseScale * HoverScaleMultiplier;
        ApplyVisual();
    }

    void OnMouseExit()
    {
        if (!Application.isPlaying || !isHovered) return;
        isHovered = false;
        transform.localScale = baseScale;
        ApplyVisual();
    }

    void Update()
    {
        if (!Application.isPlaying || !isHovered) return;
        if (GameManager.Instance == null) return;
        var state = GameManager.Instance.State;
        if (state != GameManager.GameState.WaitingForInput &&
            state != GameManager.GameState.ChainReacting)
        {
            isHovered = false;
            transform.localScale = baseScale;
            ApplyVisual();
        }
    }

    void ApplyVisual()
    {
        col.isTrigger = isOpen;
        sr.sprite = isOpen ? SpriteHelper.Square : SpriteHelper.Crosshatch;
        Color c = isOpen ? openColor : closedColor;
        if (isHovered)
        {
            c = new Color(
                Mathf.Min(1f, c.r + HoverBrightness),
                Mathf.Min(1f, c.g + HoverBrightness),
                Mathf.Min(1f, c.b + HoverBrightness),
                Mathf.Clamp01(c.a + HoverAlphaBoost)
            );
        }
        sr.color = c;
    }
}
