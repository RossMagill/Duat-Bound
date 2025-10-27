using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class PlayerMovement2 : MonoBehaviour
{
    [Header("Input (New Input System)")]
    [SerializeField] private InputActionReference moveAction;   // expects 1D axis (-1..1)
    [SerializeField] private InputActionReference jumpAction;

    [Header("Horizontal Movement")]
    public float moveSpeed = 12f;
    public float groundAcceleration = 120f;
    public float groundDeceleration = 140f;
    public float airAcceleration = 90f;
    public float airDeceleration = 40f;
    public float maxHorizontalSpeed = 16f;

    [Header("Jump")]
    public float jumpVelocity = 12f;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.12f;

    [Header("Gravity Multipliers")]
    public float ascendGravityMultiplier = 1.6f;     // rising + held
    public float fallGravityMultiplier = 2.6f;       // falling
    public float lowJumpGravityMultiplier = 3.6f;    // rising + released

    [Header("Grounding")]
    public LayerMask groundMask;
    public float groundCheckSkin = 0.08f;
    public float groundStickForce = 3f;

    [Header("Wall Slide / Jump")]
    public LayerMask wallMask;
    public float wallCheckDistance = 0.2f;
    public float wallSlideSpeed = 6f;
    public float wallCoyoteTime = 0.12f;
    public float wallJumpHorizontalVelocity = 12f;
    public float wallJumpVerticalVelocity = 12.5f;
    public float wallJumpInputLock = 0.10f;              // slightly longer lock
    public bool  requireIntoWallInputToSlide = true;
    public bool  slideOnlyWhenFalling = true;            // NEW: prevents “sticky while rising”
    public float sameWallJumpCooldown = 0.15f;           // NEW: stops pogo-climbing the same wall

    [Header("Slice Lock (2.5D)")]
    public float sliceZ = 0f;

    [Header("Debug")]
    public bool drawGroundCheckGizmo = false;
    public bool drawWallCheckGizmo = false;

    // --- Private state ---
    private Rigidbody rb;
    private BoxCollider box;

    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool  jumpHeld;
    private bool  wasGrounded;

    // Wall state
    private bool onWall;
    private int  wallSide; // -1 = left, +1 = right
    private float wallCoyoteTimer;
    private float wallJumpLockTimer;

    // NEW: Prevent repeated jumps on the same wall
    private int   lastWallSideJumpedFrom = 0;  // -1, 0, +1
    private float sameWallCooldownTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        box = GetComponent<BoxCollider>();

        rb.constraints = RigidbodyConstraints.FreezePositionZ |
                         RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationY |
                         RigidbodyConstraints.FreezeRotationZ;

        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearDamping = 0f;

        if (wallMask == 0) wallMask = groundMask; // sensible default
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
        if (jumpAction != null)
        {
            if (jumpAction.action.WasPressedThisFrame())
                jumpBufferTimer = jumpBufferTime;

            jumpHeld = jumpAction.action.IsPressed();
        }

        if (coyoteTimer > 0f)          coyoteTimer -= Time.deltaTime;
        if (jumpBufferTimer > 0f)      jumpBufferTimer -= Time.deltaTime;
        if (wallCoyoteTimer > 0f)      wallCoyoteTimer -= Time.deltaTime;
        if (wallJumpLockTimer > 0f)    wallJumpLockTimer -= Time.deltaTime;
        if (sameWallCooldownTimer > 0f) sameWallCooldownTimer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        // Lock to Z slice
        var p = rb.position;
        if (Mathf.Abs(p.z - sliceZ) > 0.0001f)
            rb.position = new Vector3(p.x, p.y, sliceZ);

        // Ground & wall sense
        bool grounded = IsGrounded();
        SenseWalls(out bool wallL, out bool wallR);
        onWall   = (!grounded) && (wallL || wallR);
        wallSide = wallL ? -1 : (wallR ? +1 : 0);

        if (grounded)
        {
            coyoteTimer = coyoteTime;
            wallCoyoteTimer = 0f;
            lastWallSideJumpedFrom = 0; // reset when grounded
        }
        else
        {
            if (onWall) wallCoyoteTimer = wallCoyoteTime;
        }

        // Read horizontal input (lock briefly after wall jump)
        float inputX = (wallJumpLockTimer > 0f) ? 0f : (moveAction ? moveAction.action.ReadValue<float>() : 0f);
        bool hasInput = Mathf.Abs(inputX) > 0.01f;

        // Horizontal accel/decel
        float targetSpeedX = inputX * moveSpeed;
        float curX = rb.linearVelocity.x;
        float accel = grounded ? groundAcceleration : airAcceleration;
        float decel = grounded ? groundDeceleration : airDeceleration;

        float nextX = hasInput
            ? Mathf.MoveTowards(curX, targetSpeedX, accel * Time.fixedDeltaTime)
            : Mathf.MoveTowards(curX, 0f, decel * Time.fixedDeltaTime);

        nextX = Mathf.Clamp(nextX, -maxHorizontalSpeed, maxHorizontalSpeed);

        Vector3 v = rb.linearVelocity;
        v.x = nextX;

        // Jump priority: ground/coyote first, else wall (with checks)
        bool canGroundJump = grounded || coyoteTimer > 0f;
        bool canWallJump   = !grounded && (onWall || wallCoyoteTimer > 0f);

        // NEW: disallow immediate re-jump on the SAME wall
        bool sameWallBlocked = (sameWallCooldownTimer > 0f) && (wallSide != 0) && (wallSide == lastWallSideJumpedFrom);

        if (jumpBufferTimer > 0f && (canGroundJump || (canWallJump && !sameWallBlocked)))
        {
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;

            if (canWallJump && wallSide != 0 && !sameWallBlocked)
            {
                // Wall jump away from wall
                v.y = wallJumpVerticalVelocity;
                v.x = wallJumpHorizontalVelocity * -wallSide; // push away
                wallJumpLockTimer = wallJumpInputLock;
                wallCoyoteTimer = 0f;

                // Mark this wall so you can't pogo it immediately
                lastWallSideJumpedFrom = wallSide;
                sameWallCooldownTimer  = sameWallJumpCooldown;
            }
            else
            {
                // Normal jump
                v.y = jumpVelocity;
            }

            grounded = false;
        }

        // Gravity & variable jump height
        float g = Physics.gravity.y; // negative
        bool rising  = v.y >  0.01f;
        bool falling = v.y < -0.01f;

        if (falling)
        {
            v += Vector3.up * (g * (fallGravityMultiplier - 1f) * Time.fixedDeltaTime);
        }
        else if (rising)
        {
            float mult = jumpHeld ? ascendGravityMultiplier : lowJumpGravityMultiplier;
            v += Vector3.up * (g * (mult - 1f) * Time.fixedDeltaTime);
        }

        // WALL SLIDE: only when airborne, on a wall, FALLING, and (optionally) pressing into the wall
        bool pressingTowardWall = (Mathf.Sign(inputX) == wallSide) && (Mathf.Abs(inputX) > 0.01f);
        bool allowSlide = onWall
                          && (!slideOnlyWhenFalling || falling)   // if true, must be falling
                          && (!requireIntoWallInputToSlide || pressingTowardWall);

        if (!grounded && allowSlide)
        {
            if (slideOnlyWhenFalling && !falling)
            {
                // Do nothing (no slide while rising)
            }
            else if (Mathf.Abs(v.y) > wallSlideSpeed)
            {
                v.y = -wallSlideSpeed;
            }
        }

        // Gentle ground stick to avoid micro bumps
        if (grounded && !wasGrounded && v.y > 0f) v.y = 0f;
        if (grounded && v.y <= 0f)
        {
            v += Vector3.down * groundStickForce * Time.fixedDeltaTime;
        }

        rb.linearVelocity = v;
        wasGrounded = grounded;
    }

    bool IsGrounded()
    {
        Bounds b = box.bounds;
        Vector3 halfExtents = b.extents * 0.98f;
        float castDistance = groundCheckSkin;
        Vector3 origin = b.center + Vector3.up * 0.01f;

        return Physics.BoxCast(
            origin,
            halfExtents,
            Vector3.down,
            out _,
            Quaternion.identity,
            castDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
    }

    void SenseWalls(out bool left, out bool right)
    {
        Bounds b = box.bounds;
        Vector3 halfExtents = b.extents * 0.9f;
        Vector3 origin = b.center;
        halfExtents.y *= 0.8f;

        left = Physics.BoxCast(
            origin,
            halfExtents,
            Vector3.left,
            out _,
            Quaternion.identity,
            wallCheckDistance,
            wallMask,
            QueryTriggerInteraction.Ignore
        );

        right = Physics.BoxCast(
            origin,
            halfExtents,
            Vector3.right,
            out _,
            Quaternion.identity,
            wallCheckDistance,
            wallMask,
            QueryTriggerInteraction.Ignore
        );
    }

    void OnDrawGizmosSelected()
    {
        if (box == null) return;

        if (drawGroundCheckGizmo)
        {
            Gizmos.color = Color.cyan;
            Bounds b = box.bounds;
            Vector3 origin = b.center + Vector3.up * 0.01f;
            Vector3 size = b.size * 0.98f;
            Gizmos.DrawWireCube(origin + Vector3.down * groundCheckSkin, size);
        }

        if (drawWallCheckGizmo)
        {
            Gizmos.color = Color.yellow;
            Bounds b = box.bounds;
            Vector3 origin = b.center;
            Vector3 size = b.size * 0.9f;
            size.y *= 0.8f;

            Gizmos.DrawWireCube(origin + Vector3.left  * (wallCheckDistance), size);
            Gizmos.DrawWireCube(origin + Vector3.right * (wallCheckDistance), size);
        }
    }
}
