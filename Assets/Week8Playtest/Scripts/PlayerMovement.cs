using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference resetAction;
    [SerializeField] private InputActionReference changeScene;


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
        resetAction?.action.Enable();
        changeScene?.action.Enable();
    }
    void OnDisable()
    {
        moveAction?.action.Disable();
        jumpAction?.action.Disable();
        resetAction?.action.Disable();
        changeScene?.action.Disable();
    }

    void Update()
    {
        if (jumpAction != null && jumpAction.action.WasPressedThisFrame())
            jumpQueued = true;

        if (resetAction != null && resetAction.action.WasPressedThisFrame())
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
        
        if (changeScene.action.WasPressedThisFrame())
        {
            int curr = SceneManager.GetActiveScene().buildIndex;
            print(curr);
            if (curr == 0)
            {
                SceneManager.LoadScene("Week8B");
                return;
            } else if (curr == 1)
            {
                SceneManager.LoadScene("Week8C");
                return;
            } else if (curr == 2)
            {
                SceneManager.LoadScene("Week8A");
                return;
            }
        }
    }

void FixedUpdate()
{
    // Lock to Z-slice (Z = 0)
    var p = rb.position;
    if (Mathf.Abs(p.z - sliceZ) > 0.0001f)
        rb.position = new Vector3(p.x, p.y, sliceZ);

    // Horizontal movement
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

    // --- CHANGED: use v.y (the up-to-date velocity), not rb.linearVelocity.y
    bool rising  = v.y >  0.01f;
    bool falling = v.y < -0.01f;

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

    // --- CHANGED: cache wallDir and use v.y (current) for the falling check
    float wallDir = IsWalled();
    if (wallDir != 0 && !IsGrounded() && v.y < 0f)
    {
        if (inputX > 0 && wallDir == 1)
        {
            print("right");
            v.y = -5f;
        }
        else if (inputX < 0 && wallDir == -1)
        {
            print("left");
            v.y = -5f;
        }
    }

    rb.linearVelocity = v;
}

    bool IsGrounded()
    {
        var b = box.bounds;
        float y = b.min.y + 0.01f;
        float d = groundCheckDistance;
        float inset = 0.02f;

        Vector3[] offsets =
        {
            new( 0f, 0f,  0f), // center
            new( b.extents.x - inset, 0f,  0f),
            new(-b.extents.x + inset, 0f,  0f),
            new( 0f, 0f,  b.extents.z - inset),
            new( 0f, 0f, -b.extents.z + inset),
            new( b.extents.x - inset, 0f,  b.extents.z - inset),
            new( b.extents.x - inset, 0f, -b.extents.z + inset),
            new(-b.extents.x + inset, 0f,  b.extents.z - inset),
            new(-b.extents.x + inset, 0f, -b.extents.z + inset),
        };

        foreach (var o in offsets)
        {
            Vector3 origin = new(b.center.x + o.x, y, b.center.z + o.z);
            if (Physics.Raycast(origin, Vector3.down, d, groundMask, QueryTriggerInteraction.Ignore))
                return true;
        }
        return false;
    }

    float IsWalled()
    {
        Vector3 origin = box.bounds.center;

        float distance = box.bounds.extents.x + groundCheckDistance;

        bool leftWall = Physics.Raycast(origin, Vector3.left, distance, groundMask, QueryTriggerInteraction.Ignore);
        bool rightWall = Physics.Raycast(origin, Vector3.right, distance, groundMask, QueryTriggerInteraction.Ignore);

        if (leftWall && !rightWall)
        {
            return -1;
        }
        else if (rightWall && !leftWall)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
}
