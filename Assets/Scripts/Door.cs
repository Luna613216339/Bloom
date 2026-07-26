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
    private SpriteRenderer sr;
    private BoxCollider2D col;
    private Vector3 baseScale;
    private bool isHovered;
    private Coroutine animCoroutine;

    private static readonly Color HoverColor = new Color(0.2f, 1f, 0.4f);
    private const float PulseSpeed = 5f;
    private const float PulseScaleMin = 1.05f;
    private const float PulseScaleMax = 1.2f;

    void Awake()
    {
        EnsureComponents();
        isOpen = startsOpen;
        baseScale = transform.localScale;
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
            col = gameObject.AddComponent<BoxCollider2D>();
        if (Ball.bounceMat != null)
            col.sharedMaterial = Ball.bounceMat;

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
        col.isTrigger = isOpen;

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

    IEnumerator AnimateToggle()
    {
        float elapsed = 0f;

        if (isOpen)
        {
            sr.sprite = SpriteHelper.Crosshatch;
            while (elapsed < animDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animDuration;
                float alpha = Mathf.Lerp(closedColor.a, 0f, t);
                float scale = Mathf.Lerp(1f, 0.6f, t);
                sr.color = new Color(closedColor.r, closedColor.g, closedColor.b, alpha);
                transform.localScale = baseScale * scale;
                yield return null;
            }
            sr.enabled = false;
            transform.localScale = baseScale;
        }
        else
        {
            sr.enabled = true;
            sr.sprite = SpriteHelper.Crosshatch;
            while (elapsed < animDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animDuration;
                float alpha = Mathf.Lerp(0f, closedColor.a, t);
                float scale = Mathf.Lerp(0.6f, 1f, t);
                sr.color = new Color(closedColor.r, closedColor.g, closedColor.b, alpha);
                transform.localScale = baseScale * scale;
                yield return null;
            }
            ApplyVisual(instant: true);
        }

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
        {
            if (isOpen)
            {
                sr.enabled = false;
            }
            else
            {
                transform.localScale = baseScale;
                sr.color = closedColor;
            }
        }
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
                if (animCoroutine == null)
                {
                    transform.localScale = baseScale;
                    if (isOpen) sr.enabled = false;
                    else sr.color = closedColor;
                }
                return;
            }

            if (animCoroutine != null) return;

            float pulse = (Mathf.Sin(Time.time * PulseSpeed) + 1f) * 0.5f;

            if (isOpen)
            {
                sr.enabled = true;
                sr.sprite = SpriteHelper.Crosshatch;
                float alpha = Mathf.Lerp(0.18f, 0.35f, pulse);
                sr.color = new Color(closedColor.r, closedColor.g, closedColor.b, alpha);
                float s = Mathf.Lerp(0.95f, 1.05f, pulse);
                transform.localScale = baseScale * s;
            }
            else
            {
                float s = Mathf.Lerp(PulseScaleMin, PulseScaleMax, pulse);
                transform.localScale = baseScale * s;
                Color c = Color.Lerp(closedColor, HoverColor, 0.5f + pulse * 0.5f);
                sr.color = c;
            }
        }
    }

    void ApplyVisual(bool instant)
    {
        col.isTrigger = isOpen;

        if (isOpen)
        {
            sr.enabled = false;
        }
        else
        {
            sr.enabled = true;
            sr.sprite = SpriteHelper.Crosshatch;
            sr.color = closedColor;
            transform.localScale = baseScale;
        }
    }
}
