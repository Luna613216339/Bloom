using System.Collections;
using UnityEngine;

[ExecuteAlways]
public class Door : MonoBehaviour
{
    [Header("Door Settings")]
    [SerializeField] private bool startsOpen = false;
    [SerializeField] private Color closedColor = new Color(0.35f, 0.35f, 0.35f);
    [SerializeField] private float animDuration = 0.15f;

    private bool isOpen;
    private int link;
    public int Link => link;
    public int Index { get; private set; }
    public bool IsOpen => isOpen;
    private SpriteRenderer sr;
    private BoxCollider2D col;
    private Vector3 baseScale;
    private bool isHovered;
    private Coroutine animCoroutine;
    private Sprite closedSprite;
    private Sprite openSprite;

    private static readonly Color HoverColor = new Color(0.2f, 1f, 0.4f);
    private const float PulseSpeed = 5f;
    private const float PulseScaleMin = 1.05f;
    private const float PulseScaleMax = 1.2f;
    /// <summary>白模里开着的门画成空心框，这是框的不透明度</summary>
    private const float OpenAlpha = 0.32f;

    void Awake()
    {
        EnsureComponents();
        isOpen = startsOpen;
        baseScale = transform.localScale;
        closedColor = Look.Door(link != 0, closedColor);
        float aspect = baseScale.y > 0.0001f ? baseScale.x / baseScale.y : 1f;
        if (Look.On)
        {
            closedSprite = link != 0 ? SpriteHelper.DoorLinked(aspect) : SpriteHelper.DoorClosed(aspect);
            openSprite = SpriteHelper.DoorOpen(aspect);
            // 贴图按门的长宽比生成，Sliced + size(1,1) 让它正好填满 1×1，再由 transform 缩放成门的尺寸
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = Vector2.one;
        }
        else
        {
            closedSprite = SpriteHelper.Crosshatch;
            openSprite = null;
            sr.drawMode = SpriteDrawMode.Simple;
        }
        ApplyVisual(instant: true);
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
        {
            col = gameObject.AddComponent<BoxCollider2D>();
            // 运行时 AddComponent 会按 SpriteRenderer 当前的 sprite 自动定尺寸，
            // 而这时 sprite 还是空的，碰撞体会变成 0×0 —— 球直接穿门。门的尺寸全靠 transform 的缩放，
            // 所以碰撞体固定 1×1。
            col.size = Vector2.one;
            col.offset = Vector2.zero;
        }
        if (Ball.bounceMat != null)
            col.sharedMaterial = Ball.bounceMat;

        var rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
    }

    /// <summary>
    /// 由 LevelBuilder 在物体还没激活（Awake 还没跑）时调用。
    /// link 不为 0 的门是联动门：点一扇，同组的每一扇都翻转一次。
    /// </summary>
    public void Configure(bool startsOpen, Color color, int link, int index)
    {
        this.startsOpen = startsOpen;
        this.closedColor = color;
        this.link = link;
        Index = index;
    }

    public bool ContainsPoint(Vector2 point)
    {
        return col.OverlapPoint(point);
    }

    public void Toggle() => Toggle(true);

    public void Toggle(bool propagate)
    {
        if (propagate && link != 0)
        {
            foreach (var other in FindObjectsByType<Door>(FindObjectsSortMode.None))
                if (other != this && other.link == link) other.Toggle(false);
        }

        isOpen = !isOpen;
        col.isTrigger = isOpen;

        // 联动的那几扇不出声，否则点一下叠好几声
        if (propagate && Application.isPlaying && AudioManager.Instance != null)
            AudioManager.Instance.PlayDoor(isOpen);

        if (Application.isPlaying)
        {
            if (animCoroutine != null)
                StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(AnimateToggle());
        }
        else
        {
            ApplyVisual(instant: true);
        }
    }

    public void ResetState()
    {
        if (animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
            animCoroutine = null;
        }
        isOpen = startsOpen;
        isHovered = false;
        ApplyVisual(instant: true);
    }

    /// <summary>开门：实心门缩小淡出；关门：反过来。动画结束后交给 ApplyVisual 摆成静态的样子</summary>
    IEnumerator AnimateToggle()
    {
        float elapsed = 0f;
        float fromA = isOpen ? closedColor.a : 0f;
        float toA = isOpen ? 0f : closedColor.a;
        float fromS = isOpen ? 1f : 0.6f;
        float toS = isOpen ? 0.6f : 1f;

        sr.enabled = true;
        sr.sprite = closedSprite;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animDuration;
            sr.color = new Color(closedColor.r, closedColor.g, closedColor.b, Mathf.Lerp(fromA, toA, t));
            transform.localScale = baseScale * Mathf.Lerp(fromS, toS, t);
            yield return null;
        }
        ApplyVisual(instant: true);
        animCoroutine = null;
    }

    void OnMouseEnter()
    {
        if (!Application.isPlaying || isHovered) return;
        if (GameManager.Instance == null) return;
        var state = GameManager.Instance.State;
        if (state != GameManager.GameState.WaitingForInput &&
            state != GameManager.GameState.ChainReacting) return;
        isHovered = true;
    }

    void OnMouseExit()
    {
        if (!Application.isPlaying || !isHovered) return;
        isHovered = false;
        if (animCoroutine == null)
            ApplyVisual(instant: true);
    }

    void Update()
    {
        if (!Application.isPlaying) return;

        if (isHovered)
        {
            if (GameManager.Instance == null) return;
            var state = GameManager.Instance.State;
            if (state != GameManager.GameState.WaitingForInput &&
                state != GameManager.GameState.ChainReacting)
            {
                isHovered = false;
                if (animCoroutine == null) ApplyVisual(instant: true);
                return;
            }

            if (animCoroutine != null) return;

            float pulse = (Mathf.Sin(Time.time * PulseSpeed) + 1f) * 0.5f;

            if (isOpen)
            {
                sr.enabled = true;
                if (openSprite != null)
                {
                    // 白模：空心框变实、轻微呼吸
                    sr.sprite = openSprite;
                    sr.color = new Color(closedColor.r, closedColor.g, closedColor.b, Mathf.Lerp(0.55f, 0.9f, pulse));
                }
                else
                {
                    sr.sprite = closedSprite;
                    sr.color = new Color(closedColor.r, closedColor.g, closedColor.b, Mathf.Lerp(0.18f, 0.35f, pulse));
                }
                transform.localScale = baseScale * Mathf.Lerp(0.95f, 1.05f, pulse);
            }
            else
            {
                float s = Mathf.Lerp(PulseScaleMin, PulseScaleMax, pulse);
                transform.localScale = baseScale * s;
                // 白模只有一个强调色，留给"着火"；门的悬停反馈靠放大和变浅
                Color target = Look.On ? new Color(0.62f, 0.62f, 0.62f) : HoverColor;
                sr.color = Color.Lerp(closedColor, target, 0.5f + pulse * 0.5f);
            }
        }
    }

    void ApplyVisual(bool instant)
    {
        col.isTrigger = isOpen;
        transform.localScale = baseScale;

        if (isOpen)
        {
            if (openSprite != null)
            {
                sr.enabled = true;
                sr.sprite = openSprite;
                sr.color = new Color(closedColor.r, closedColor.g, closedColor.b, OpenAlpha);
            }
            else
            {
                sr.enabled = false;
            }
        }
        else
        {
            sr.enabled = true;
            sr.sprite = closedSprite;
            sr.color = closedColor;
        }
    }
}
