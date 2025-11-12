using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class FlyerMovement : MonoBehaviour, IMovement
{
    [Header("Input (New Input System)")]
    [Tooltip("Vector2. X = left/right, Y = up/down.")]
    [SerializeField] private InputActionReference move2D;
    [SerializeField] private InputActionReference boostAction;     // held
    [SerializeField] private InputActionReference brakeAction;     // held (air-brake)
    [SerializeField] private InputActionReference hoverHoldAction; // toggle altitude hold (press)
    [SerializeField] private InputActionReference resetAction;     // press
    [SerializeField] private InputActionReference changeScene;     // press

    [Header("Speed & Acceleration")]
    public float maxSpeedX = 18f;
    public float maxSpeedY = 14f;
    public float acceleration = 60f;    // toward target vel
    public float deceleration = 70f;    // when no input on that axis

    [Header("Boost")]
    public float boostMultiplier = 1.6f;
    public float boostAccelMultiplier = 1.4f;
    public float staminaMax = 3.0f;     // seconds of full boost
    public float staminaRegenPerSec = 0.9f;

    [Header("Air Control / Feel")]
    public float airBrakeStrength = 10f;     // additive decel when brake is held
    public float gravityDown = 6f;           // small gravity to make it feel weighty
    public float neutralLift = 0.0f;         // constant upward counter to gravity (0 = none)
    public float verticalDeadzone = 0.05f;   // ignore tiny Y input
    public float horizontalDeadzone = 0.05f; // ignore tiny X input

    [Header("Altitude Limits (optional)")]
    public bool clampAltitude = false;
    public float minAltitude = -10f;
    public float maxAltitude = 100f;

    [Header("Altitude Hold")]
    public bool allowAltitudeHold = true;
    public float holdStrength = 20f;     // how aggressively we correct toward hold altitude
    public float holdDamp = 8f;          // velocity damping during hold

    [Header("Slice Lock (2.5D)")]
    public float sliceZ = 0f;

    [Header("Bank/Tilt Visuals (optional)")]
    public Transform visual;            // child to tilt
    public float bankMaxDegrees = 20f;  // tilt around Z with horizontal speed
    public float bankSmoothing = 8f;

    private Rigidbody rb;
    private BoxCollider box;
    private float stamina;
    private bool hoverHoldEnabled = false;
    private float heldAltitude = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        box = GetComponent<BoxCollider>();

        rb.useGravity = false; // we handle our own lightweight gravity
        rb.constraints = RigidbodyConstraints.FreezePositionZ |
                         RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationY |
                         RigidbodyConstraints.FreezeRotationZ;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        stamina = staminaMax;
    }

    void OnEnable()
    {
        move2D?.action.Enable();
        boostAction?.action.Enable();
        brakeAction?.action.Enable();
        hoverHoldAction?.action.Enable();
        resetAction?.action.Enable();
        changeScene?.action.Enable();
    }

    void OnDisable()
    {
        move2D?.action.Disable();
        boostAction?.action.Disable();
        brakeAction?.action.Disable();
        hoverHoldAction?.action.Disable();
        resetAction?.action.Disable();
        changeScene?.action.Disable();
    }

    void Update()
    {
        // Reset scene
        if (resetAction != null && resetAction.action.WasPressedThisFrame())
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // Cycle scenes the same way as your old script
        if (changeScene != null && changeScene.action.WasPressedThisFrame())
        {
            int curr = SceneManager.GetActiveScene().buildIndex;
            if      (curr == 0) SceneManager.LoadScene("Week8B");
            else if (curr == 1) SceneManager.LoadScene("Week8C");
            else if (curr == 2) SceneManager.LoadScene("Week8A");
        }

        // Toggle altitude hold
        if (allowAltitudeHold && hoverHoldAction != null && hoverHoldAction.action.WasPressedThisFrame())
        {
            hoverHoldEnabled = !hoverHoldEnabled;
            heldAltitude = rb.position.y;
        }
    }

    void FixedUpdate()
    {
        // Lock to Z slice
        var p = rb.position;
        if (Mathf.Abs(p.z - sliceZ) > 0.0001f)
            rb.position = new Vector3(p.x, p.y, sliceZ);

        // Read input
        Vector2 inVec = move2D ? move2D.action.ReadValue<Vector2>() : Vector2.zero;
        float inputX = Mathf.Abs(inVec.x) < horizontalDeadzone ? 0f : inVec.x;
        float inputY = Mathf.Abs(inVec.y) < verticalDeadzone   ? 0f : inVec.y;

        // Boost state & stamina
        bool boosting = boostAction && boostAction.action.IsPressed() && stamina > 0.05f;
        float speedMult   = boosting ? boostMultiplier       : 1f;
        float accelMult   = boosting ? boostAccelMultiplier  : 1f;

        // Target velocities
        float targetVX = inputX * (maxSpeedX * speedMult);
        float targetVY = inputY * (maxSpeedY * speedMult);

        // Current velocity
        Vector3 v = rb.linearVelocity;

        // Horizontal accel/decel
        float ax = (Mathf.Abs(inputX) > 0.001f) ? (acceleration * accelMult) : (deceleration + (brakeAction && brakeAction.action.IsPressed() ? airBrakeStrength : 0f));
        v.x = Mathf.MoveTowards(v.x, targetVX, ax * Time.fixedDeltaTime);

        // Vertical accel/decel
        float ay = (Mathf.Abs(inputY) > 0.001f) ? (acceleration * accelMult) : (deceleration + (brakeAction && brakeAction.action.IsPressed() ? airBrakeStrength : 0f));
        v.y = Mathf.MoveTowards(v.y, targetVY, ay * Time.fixedDeltaTime);

        // Lightweight gravity feel (only when not strongly pushing up OR in hold mode we replace it)
        if (!hoverHoldEnabled)
        {
            float netGravity = -gravityDown + neutralLift;
            v.y += netGravity * Time.fixedDeltaTime;
        }
        else
        {
            // Altitude hold: PD-style correction toward heldAltitude
            float err = heldAltitude - rb.position.y;
            float correction = err * holdStrength - v.y * holdDamp;
            v.y += correction * Time.fixedDeltaTime;

            // If user gives significant vertical input, shift the hold target
            if (Mathf.Abs(inputY) > 0.2f)
                heldAltitude = rb.position.y;
        }

        // Clamp speeds
        v.x = Mathf.Clamp(v.x, -maxSpeedX * speedMult, maxSpeedX * speedMult);
        v.y = Mathf.Clamp(v.y, -maxSpeedY * speedMult, maxSpeedY * speedMult);

        // Apply altitude clamps (optional)
        if (clampAltitude)
        {
            float nextY = rb.position.y + v.y * Time.fixedDeltaTime;
            if (nextY < minAltitude && v.y < 0f) v.y = 0f;
            if (nextY > maxAltitude && v.y > 0f) v.y = 0f;
        }

        rb.linearVelocity = v;

        // Update stamina
        if (boosting)
            stamina = Mathf.Max(0f, stamina - Time.fixedDeltaTime);
        else
            stamina = Mathf.Min(staminaMax, stamina + staminaRegenPerSec * Time.fixedDeltaTime);

        // Visual banking (optional)
        if (visual)
        {
            float bankT = Mathf.InverseLerp(0f, maxSpeedX, Mathf.Abs(rb.linearVelocity.x));
            float targetBank = -Mathf.Sign(rb.linearVelocity.x) * bankMaxDegrees * bankT; // negative for rightward bank
            Quaternion targetRot = Quaternion.Euler(0f, 0f, targetBank);
            visual.localRotation = Quaternion.Slerp(visual.localRotation, targetRot, bankSmoothing * Time.fixedDeltaTime);
        }
    }

    // Expose stamina (0..1) for UI bars if you want
    public float Stamina01 => Mathf.Approximately(staminaMax, 0f) ? 0f : Mathf.Clamp01(stamina / staminaMax);

    public void EnableMovement() { enabled = true; }
    public void DisableMovement() { enabled = false; }
}
