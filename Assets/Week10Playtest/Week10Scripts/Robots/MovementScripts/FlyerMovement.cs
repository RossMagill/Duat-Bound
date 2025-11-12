using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class FlyerMovement : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;

    [Header("Movement")]
    public float moveSpeed = 8f;
    public float jumpForce = 7f;
    public LayerMask groundMask;

    Rigidbody rb;
    bool controlEnabled;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Start disabled if you're spawning it inside the stack
        SetControlEnabled(true);
        rb.isKinematic = true;                        // while in the stack
        GetComponent<Collider>().enabled = false;     // while in the stack
        gameObject.layer = LayerMask.NameToLayer("IgnorePlayer"); // optional
    }

    public void SetControlEnabled(bool enabled)
    {
        controlEnabled = enabled;

        if (enabled)
        {
            moveAction?.action.Enable();
            jumpAction?.action.Enable();
            rb.isKinematic = false;
            GetComponent<Collider>().enabled = true;
            gameObject.layer = LayerMask.NameToLayer("Player"); // optional
        }
        else
        {
            moveAction?.action.Disable();
            jumpAction?.action.Disable();
            rb.isKinematic = true;
            GetComponent<Collider>().enabled = false;
            gameObject.layer = LayerMask.NameToLayer("IgnorePlayer");
        }
    }

    void Update()
    {
        if (!controlEnabled) return;

        Vector2 move = moveAction.action.ReadValue<Vector2>();
        Vector3 v = rb.linearVelocity;
        v.x = move.x * moveSpeed;
        rb.linearVelocity = v;

        if (jumpAction.action.WasPressedThisFrame() && IsGrounded())
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
    }

    bool IsGrounded()
    {
        // your ground check here
        return true;
    }   
}
