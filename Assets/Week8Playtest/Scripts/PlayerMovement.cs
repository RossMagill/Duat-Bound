using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;   
    [SerializeField] private InputActionReference jumpAction;

    [Header("Movement")]
    public float moveSpeed = 100f;
    public float acceleration = 50f;
    public float deceleration = 60f;
    public float maxHorizontalSpeed = 30f;

    [Header("Jump")]
    public float jumpForce = 8f;
    public LayerMask groundMask;
    public float groundCheckDistance = 0.1f;

    [Header("Gravity")]
    public float ascendGravityMultiplier = 1.8f;
    public float fallGravityMultiplier = 3.2f;
    public float lowJumpGravityMultiplier = 4.0f;


    [Header("Slice Lock")]
    public float sliceZ = 0f;

    private Rigidbody rb;
    private BoxCollider box;
    private bool jumpQueued;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        box = GetComponent<BoxCollider>();

        rb.constraints = RigidbodyConstraints.FreezePositionZ |
                         RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationY |
                         RigidbodyConstraints.FreezeRotationZ;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void OnEnable()
    {
        moveAction?.action.Enable();
        jumpAction?.action.Enable();
    }
    void OnDisable()
    {
        moveAction?.action.Disable();
        jumpAction?.action.Disable();
    }

    void Update()
    {
        if (jumpAction != null && jumpAction.action.WasPressedThisFrame())
            jumpQueued = true;
    }

    void FixedUpdate()
    {
        // Lock to Z
        var p = rb.position;
        if (Mathf.Abs(p.z - sliceZ) > 0.0001f)
            rb.position = new Vector3(p.x, p.y, sliceZ);

        // Horizontal move
        float inputX = moveAction ? moveAction.action.ReadValue<float>() : 0f;
        float targetX = inputX * moveSpeed;
        float rate = (Mathf.Abs(inputX) > 0.01f) ? acceleration : deceleration;
        float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetX, rate * Time.fixedDeltaTime);
        newX = Mathf.Clamp(newX, -maxHorizontalSpeed, maxHorizontalSpeed);

        Vector3 v = rb.linearVelocity;
        v.x = newX;

        if (jumpQueued && IsGrounded())
        {
            v.y = jumpForce;
            jumpQueued = false;
        }
        else
        {
            jumpQueued = false;
        }

        float g = Physics.gravity.y; // negative
        bool rising = rb.linearVelocity.y > 0.01f;
        bool falling = rb.linearVelocity.y < -0.01f;
        bool jumpHeld = jumpAction && jumpAction.action.IsPressed();

        if (falling)
        {
            v += Vector3.up * (g * (fallGravityMultiplier - 1f) * Time.fixedDeltaTime);
        }
        else if (rising)
        {
            float mult = jumpHeld ? ascendGravityMultiplier : lowJumpGravityMultiplier;
            v += Vector3.up * (g * (mult - 1f) * Time.fixedDeltaTime);
        }

        rb.linearVelocity = v;
    }

    bool IsGrounded()
    {
        print("Grounded");
        Vector3 origin = box.bounds.center;
        float distance = box.bounds.extents.y + groundCheckDistance;
        return Physics.Raycast(origin, Vector3.down, distance, groundMask, QueryTriggerInteraction.Ignore);
    }
}
