using System.Collections.Generic;
using UnityEngine;

public class SchoolSpawner : MonoBehaviour
{
    [Header("Formation")]
    [SerializeField] private float ballSpacing = 0.5f;
    [SerializeField] private float waveAmplitude = 0.3f;
    [SerializeField] private float waveSpeed = 3f;
    [SerializeField] private bool enableWave = true;
    [SerializeField] private float ballSize = 0.35f;

    [Header("Escape Balls")]
    [SerializeField] private int escapeBallCount = 0;
    [SerializeField] private float dangerRadius = 2f;
    [SerializeField] private float fleeSpeed = 6f;
    [SerializeField] private Color escapeBallColor = new Color(1f, 0.2f, 0.2f);

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private bool loopPath = true;

    [Header("Circle Path")]
    [SerializeField] private bool useCirclePath = false;
    [SerializeField] private float circleRadius = 2f;
    [SerializeField] private bool clockwise = false;

    [Header("Ball Count")]
    [Tooltip("If > 0, use this instead of GameManager's ballCount")]
    [SerializeField] private int overrideBallCount = 0;

    [Header("Ball Params")]
    [SerializeField] private float reactionMaxScale = 1.3f;
    [SerializeField] private float reactionDuration = 1.5f;
    [SerializeField] private float expandSpeed = 1.5f;

    [Header("Path Visualization")]
    [SerializeField] private bool showPathLine = false;
    [SerializeField] private Color pathLineColor = new Color(0.3f, 0.6f, 1f, 0.3f);
    [SerializeField] private float pathLineWidth = 0.03f;

    private int ballCount;
    private Transform[] waypoints;
    private float[] segmentLengths;
    private float totalPathLength;
    private float headDistance;
    private List<Ball> spawnedBalls = new List<Ball>();
    private ContactFilter2D wallFilter;
    private RaycastHit2D[] rayResults = new RaycastHit2D[4];
    private bool[] stalled;

    private static readonly Color[] BallColors =
    {
        new Color(1f, 0.35f, 0.35f),
        new Color(1f, 0.6f, 0.2f),
        new Color(1f, 0.9f, 0.3f),
        new Color(0.4f, 1f, 0.5f),
        new Color(0.3f, 0.8f, 1f),
        new Color(0.7f, 0.5f, 1f),
        new Color(1f, 0.5f, 0.8f),
    };

    void Start()
    {
        if (showPathLine)
            CreatePathLine();
    }

    void CreatePathLine()
    {
        var lineGO = new GameObject("PathLine");
        lineGO.transform.SetParent(transform, false);
        var lr = lineGO.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.startWidth = pathLineWidth;
        lr.endWidth = pathLineWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = pathLineColor;
        lr.endColor = pathLineColor;
        lr.sortingOrder = -1;

        if (useCirclePath)
        {
            int segments = 64;
            lr.positionCount = segments + 1;
            Vector2 center = transform.position;
            for (int i = 0; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                Vector3 p = (Vector3)center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * circleRadius;
                lr.SetPosition(i, p);
            }
        }
        else if (waypoints != null && waypoints.Length >= 2)
        {
            int count = loopPath ? waypoints.Length + 1 : waypoints.Length;
            lr.positionCount = count;
            for (int i = 0; i < waypoints.Length; i++)
                lr.SetPosition(i, waypoints[i].position);
            if (loopPath)
                lr.SetPosition(waypoints.Length, waypoints[0].position);
        }
    }

    public void Init()
    {
        spawnedBalls.Clear();
        ballCount = overrideBallCount > 0 ? overrideBallCount : GameManager.Instance.CurrentBallCount;
        if (!useCirclePath)
            CollectWaypoints();
        ComputePathLengths();

        wallFilter = new ContactFilter2D();
        wallFilter.useTriggers = false;
        wallFilter.useLayerMask = true;
        wallFilter.layerMask = 1;

        if (!useCirclePath && (waypoints == null || waypoints.Length < 2)) return;

        headDistance = (ballCount - 1) * ballSpacing;
        SpawnSchool();

        stalled = new bool[ballCount];
    }

    void FixedUpdate()
    {
        if (!useCirclePath && (waypoints == null || waypoints.Length < 2)) return;

        headDistance += moveSpeed * Time.fixedDeltaTime;
        if ((loopPath || useCirclePath) && headDistance > totalPathLength)
            headDistance -= totalPathLength;

        UpdateBallPositions();
    }

    void CollectWaypoints()
    {
        var wps = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (child.gameObject.activeInHierarchy)
                wps.Add(child);
        }
        waypoints = wps.ToArray();
    }

    void ComputePathLengths()
    {
        if (useCirclePath)
        {
            totalPathLength = 2f * Mathf.PI * circleRadius;
            return;
        }
        if (waypoints == null || waypoints.Length < 2) return;

        int segCount = loopPath ? waypoints.Length : waypoints.Length - 1;
        segmentLengths = new float[segCount];
        totalPathLength = 0f;

        for (int i = 0; i < segCount; i++)
        {
            int next = (i + 1) % waypoints.Length;
            segmentLengths[i] = Vector2.Distance(waypoints[i].position, waypoints[next].position);
            totalPathLength += segmentLengths[i];
        }
    }

    Vector2 GetPositionAtDistance(float dist)
    {
        if (useCirclePath)
        {
            float angle = dist / circleRadius;
            if (clockwise) angle = -angle;
            angle += transform.eulerAngles.z * Mathf.Deg2Rad;
            Vector2 center = transform.position;
            return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * circleRadius;
        }

        if (loopPath)
        {
            dist %= totalPathLength;
            if (dist < 0) dist += totalPathLength;
        }
        else
        {
            dist = Mathf.Clamp(dist, 0, totalPathLength);
        }

        float accumulated = 0f;
        for (int i = 0; i < segmentLengths.Length; i++)
        {
            if (accumulated + segmentLengths[i] >= dist)
            {
                float t = (dist - accumulated) / segmentLengths[i];
                int next = (i + 1) % waypoints.Length;
                return Vector2.Lerp(waypoints[i].position, waypoints[next].position, t);
            }
            accumulated += segmentLengths[i];
        }

        return waypoints[waypoints.Length - 1].position;
    }

    Vector2 GetDirectionAtDistance(float dist)
    {
        Vector2 a = GetPositionAtDistance(dist - 0.01f);
        Vector2 b = GetPositionAtDistance(dist + 0.01f);
        Vector2 dir = (b - a).normalized;
        return dir == Vector2.zero ? Vector2.right : dir;
    }

    void SpawnSchool()
    {
        int normalCount = ballCount - escapeBallCount;

        for (int i = 0; i < ballCount; i++)
        {
            float dist = headDistance - i * ballSpacing;
            Vector2 pos = GetPositionAtDistance(dist);
            Vector2 dir = GetDirectionAtDistance(dist);
            Vector2 perp = Vector2.Perpendicular(dir);
            if (enableWave)
            {
                float wave = Mathf.Sin(i * ballSpacing * 2f) * waveAmplitude;
                pos += perp * wave;
            }

            Color color = BallColors[i % BallColors.Length];

            Ball ball = Ball.Create(pos, Vector2.zero, color,
                ballSize, reactionMaxScale, reactionDuration, expandSpeed);

            ball.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;

            if (escapeBallCount > 0 && i >= normalCount)
            {
                ball.SetAsEscapeBall(dangerRadius, fleeSpeed, escapeBallColor);
                Vector2 pathDir = GetDirectionAtDistance(dist);
                ball.SetPreferredFleeDirection(-pathDir);
            }

            spawnedBalls.Add(ball);
        }
    }

    void UpdateBallPositions()
    {
        for (int i = 0; i < spawnedBalls.Count; i++)
        {
            if (spawnedBalls[i] == null) continue;
            if (spawnedBalls[i].CurrentState != Ball.BallState.Moving) continue;
            if (stalled[i]) continue;

            float dist = headDistance - i * ballSpacing;
            Vector2 targetPos = GetPositionAtDistance(dist);
            Vector2 dir = GetDirectionAtDistance(dist);
            Vector2 perp = Vector2.Perpendicular(dir);
            if (enableWave)
            {
                float wave = Mathf.Sin(Time.time * waveSpeed + i * ballSpacing * 2f) * waveAmplitude;
                targetPos += perp * wave;
            }

            var rb = spawnedBalls[i].GetComponent<Rigidbody2D>();
            Vector2 currentPos = rb.position;
            Vector2 moveVec = targetPos - currentPos;
            float moveDist = moveVec.magnitude;

            if (moveDist > 0.001f)
            {
                int hitCount = Physics2D.Raycast(currentPos, moveVec.normalized,
                    wallFilter, rayResults, moveDist);
                if (hitCount > 0)
                {
                    stalled[i] = true;
                    rb.bodyType = RigidbodyType2D.Dynamic;
                    Vector2 pathDir = GetDirectionAtDistance(dist);
                    float angle = Random.Range(-60f, 60f);
                    rb.linearVelocity = RotateVector(-pathDir, angle) * moveSpeed;
                    continue;
                }
            }

            rb.MovePosition(targetPos);
        }
    }

    static Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

#if UNITY_EDITOR
    int GetGizmoBallCount()
    {
        if (overrideBallCount > 0) return overrideBallCount;
        var gm = FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            int count = gm.CurrentBallCount;
            if (count > 0) return count;
        }
        return 15;
    }

    void OnDrawGizmos()
    {
        if (useCirclePath)
        {
            DrawCircleGizmos();
            return;
        }

        var wps = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (child.gameObject.activeInHierarchy)
                wps.Add(child);
        }
        if (wps.Count < 2) return;

        // Compute path for gizmo
        int segCount = loopPath ? wps.Count : wps.Count - 1;
        float pathLen = 0f;
        var segLens = new float[segCount];
        for (int i = 0; i < segCount; i++)
        {
            int next = (i + 1) % wps.Count;
            segLens[i] = Vector2.Distance(wps[i].position, wps[next].position);
            pathLen += segLens[i];
        }

        // Draw path
        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.6f);
        for (int i = 0; i < wps.Count - 1; i++)
            Gizmos.DrawLine(wps[i].position, wps[i + 1].position);
        if (loopPath)
            Gizmos.DrawLine(wps[wps.Count - 1].position, wps[0].position);

        // Draw waypoint markers
        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.8f);
        foreach (var wp in wps)
            Gizmos.DrawWireSphere(wp.position, 0.15f);

        // Draw balls along path
        int count = GetGizmoBallCount();
        int normalCount = count - escapeBallCount;

        for (int i = 0; i < count; i++)
        {
            float dist = (count - 1 - i) * ballSpacing;
            if (dist > pathLen && !loopPath) break;

            // Walk along path to find position
            float d = dist % pathLen;
            float acc = 0f;
            Vector3 pos = wps[0].position;

            for (int s = 0; s < segCount; s++)
            {
                if (acc + segLens[s] >= d)
                {
                    float t = (d - acc) / segLens[s];
                    int next = (s + 1) % wps.Count;
                    pos = Vector3.Lerp(wps[s].position, wps[next].position, t);
                    break;
                }
                acc += segLens[s];
            }

            float wave = enableWave ? Mathf.Sin(i * ballSpacing * 2f) * waveAmplitude : 0f;
            // Get direction for perpendicular offset
            float d2 = (dist + 0.1f) % pathLen;
            acc = 0f;
            Vector3 pos2 = pos;
            for (int s = 0; s < segCount; s++)
            {
                if (acc + segLens[s] >= d2)
                {
                    float t = (d2 - acc) / segLens[s];
                    int next = (s + 1) % wps.Count;
                    pos2 = Vector3.Lerp(wps[s].position, wps[next].position, t);
                    break;
                }
                acc += segLens[s];
            }
            Vector2 forward = ((Vector2)(pos2 - pos)).normalized;
            Vector2 perpDir = Vector2.Perpendicular(forward);
            pos += (Vector3)(perpDir * wave);

            bool isEscape = i >= normalCount;
            if (isEscape)
            {
                Gizmos.color = new Color(escapeBallColor.r, escapeBallColor.g,
                    escapeBallColor.b, 0.9f);
                Gizmos.DrawWireSphere(pos, ballSize * 0.5f + 0.05f);
                Gizmos.color = new Color(escapeBallColor.r, escapeBallColor.g,
                    escapeBallColor.b, 0.4f);
            }
            else
            {
                Color c = BallColors[i % BallColors.Length];
                Gizmos.color = new Color(c.r, c.g, c.b, 0.5f);
            }

            Gizmos.DrawSphere(pos, ballSize * 0.5f);
        }
    }

    void DrawCircleGizmos()
    {
        Vector2 center = transform.position;

        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.6f);
        int segments = 48;
        for (int i = 0; i < segments; i++)
        {
            float a1 = i * Mathf.PI * 2f / segments;
            float a2 = (i + 1) * Mathf.PI * 2f / segments;
            Vector3 p1 = (Vector3)center + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1)) * circleRadius;
            Vector3 p2 = (Vector3)center + new Vector3(Mathf.Cos(a2), Mathf.Sin(a2)) * circleRadius;
            Gizmos.DrawLine(p1, p2);
        }

        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.8f);
        Gizmos.DrawWireSphere(center, 0.1f);

        int count = GetGizmoBallCount();
        int normalCount = count - escapeBallCount;

        for (int i = 0; i < count; i++)
        {
            float dist = (count - 1 - i) * ballSpacing;
            float angle = dist / circleRadius;
            if (clockwise) angle = -angle;
            angle += transform.eulerAngles.z * Mathf.Deg2Rad;

            Vector3 pos = (Vector3)center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * circleRadius;

            if (enableWave)
            {
                float wave = Mathf.Sin(i * ballSpacing * 2f) * waveAmplitude;
                Vector2 radial = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                pos += (Vector3)(radial * wave);
            }

            bool isEscape = i >= normalCount;
            if (isEscape)
            {
                Gizmos.color = new Color(escapeBallColor.r, escapeBallColor.g,
                    escapeBallColor.b, 0.9f);
                Gizmos.DrawWireSphere(pos, ballSize * 0.5f + 0.05f);
                Gizmos.color = new Color(escapeBallColor.r, escapeBallColor.g,
                    escapeBallColor.b, 0.4f);
            }
            else
            {
                Color c = BallColors[i % BallColors.Length];
                Gizmos.color = new Color(c.r, c.g, c.b, 0.5f);
            }

            Gizmos.DrawSphere(pos, ballSize * 0.5f);
        }
    }
#endif
}
