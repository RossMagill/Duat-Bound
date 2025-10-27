using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("Movement")]
    public Vector3 localOffset = new Vector3(5f, 0f, 0f); // how far from start to move
    public float duration = 2f;                            // time to go from A to B
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool startAtB = false;

    [Header("Options")]
    public bool pauseAtEnds = true;
    public float endPause = 0.25f;

    Rigidbody rb;
    Vector3 startPos, endPos;
    float t;              // 0..1 along the path
    int dir = 1;          // +1 going to B, -1 going to A
    float pauseTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        startPos = transform.position;
        endPos = startPos + transform.TransformVector(localOffset);
        t = startAtB ? 1f : 0f;
        dir = startAtB ? -1 : 1;
    }

    void FixedUpdate()
    {
        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.fixedDeltaTime;
            rb.MovePosition(GetPositionAt(t)); // hold still
            return;
        }

        // progress along path at constant speed with easing for feel
        float speed = 1f / Mathf.Max(0.0001f, duration);
        t += dir * speed * Time.fixedDeltaTime;
        t = Mathf.Clamp01(t);

        // move via Rigidbody
        rb.MovePosition(GetPositionAt(t));

        // flip at ends
        if (t == 0f || t == 1f)
        {
            dir *= -1;
            if (pauseAtEnds && endPause > 0f) pauseTimer = endPause;
        }
    }

    Vector3 GetPositionAt(float tt)
    {
        float k = ease.Evaluate(tt);
        return Vector3.Lerp(startPos, endPos, k);
    }

    void OnDrawGizmosSelected()
    {
        // draw path in editor
        Gizmos.color = Color.cyan;
        Vector3 a = Application.isPlaying ? startPos : transform.position;
        Vector3 b = Application.isPlaying ? endPos : transform.position + transform.TransformVector(localOffset);
        Gizmos.DrawLine(a, b);
        Gizmos.DrawSphere(a, 0.1f);
        Gizmos.DrawSphere(b, 0.1f);
    }

    void OnCollisionEnter(Collision c)
    {
        if (!c.collider.CompareTag("Player")) return;

        // Only parent if the player is on top (contact normal points up)
        foreach (var contact in c.contacts)
        {
            if (Vector3.Dot(contact.normal, Vector3.up) > 0.5f)
            {
                c.transform.SetParent(transform);
                break;
            }
        }
    }

    void OnCollisionExit(Collision c)
    {
        if (c.collider.CompareTag("Player"))
            c.transform.SetParent(null);
    }
}
