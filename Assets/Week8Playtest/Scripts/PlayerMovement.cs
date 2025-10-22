using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference moveAction; // drag in Gameplay/Move

    [Header("Movement")]
    public float moveSpeed = 100f;
    public float acceleration = 50f;
    public float deceleration = 60f;
    public float maxHorizontalSpeed = 30f;

    [Header("Slice Lock")]
    public float sliceZ = 0f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezePositionZ |
                         RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationY |
                         RigidbodyConstraints.FreezeRotationZ;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.WakeUp();
    }

    void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();
    }

    void OnDisable()
    {
        if (moveAction != null) moveAction.action.Disable();
    }

    void FixedUpdate()
    {
        // Locks to Z slice
        var p = rb.position;
        if (Mathf.Abs(p.z - sliceZ) > 0.0001f)
            rb.position = new Vector3(p.x, p.y, sliceZ);

        // Read input directly
        float inputX = (moveAction != null) ? moveAction.action.ReadValue<float>() : 0f;

        // Target horizontal velocity
        float targetX = inputX * moveSpeed;

        // Accel/Decel toward target
        float rate = (Mathf.Abs(inputX) > 0.01f) ? acceleration : deceleration;
        float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetX, rate * Time.fixedDeltaTime);
        newX = Mathf.Clamp(newX, -maxHorizontalSpeed, maxHorizontalSpeed);

        // Apply
        Vector3 v = rb.linearVelocity;
        v.x = newX;
        rb.linearVelocity = v;
    }
}
