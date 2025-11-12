using UnityEngine;
using UnityEngine.InputSystem;

public class RobotController : MonoBehaviour, IControllable
{
    [Header("Robot Type")]
    public string robotType;

    private IMovement playerMovement;
    private PlayerFocusManager playerFocusManager;

    private void Awake()
    {
        playerMovement = GetComponent<IMovement>();
        playerFocusManager = FindAnyObjectByType<PlayerFocusManager>();
    }

    public void TryRejoin()
    {
        float searchDistance = 1f;
        float halfWidth = transform.localScale.x / 2f;
        Vector3 centreOrigin = transform.position;
        Vector3 rightOrigin = transform.position + transform.right * halfWidth;
        Vector3 leftOrigin = transform.position - transform.right * halfWidth;

        RaycastHit centreHit, rightHit, leftHit, finalHit;

        bool centreHitSuccess = Physics.Raycast(centreOrigin, Vector3.down, out centreHit, searchDistance);
        bool rightHitSuccess = Physics.Raycast(rightOrigin, Vector3.down, out rightHit, searchDistance);
        bool leftHitSuccess = Physics.Raycast(leftOrigin, Vector3.down, out leftHit, searchDistance);
        Debug.Log($"Raycast results - Centre: {centreHitSuccess}, Right: {rightHitSuccess}, Left: {leftHitSuccess}");

        bool foundTarget;

        if (centreHitSuccess)
        {
            finalHit = centreHit;
            foundTarget = true;
        }
        else if (rightHitSuccess)
        {
            finalHit = rightHit;
            foundTarget = true;
        }
        else if (leftHitSuccess)
        {
            finalHit = leftHit;
            foundTarget = true;
        }
        else
        {
            return;
        }

        Debug.Log($"Raycasting object: {this.gameObject.name}");

        if (foundTarget)
        {
            StackController targetStack = finalHit.collider.GetComponentInParent<StackController>();
            Debug.Log($"Found stack to rejoin: {targetStack?.gameObject.name}");
            if (targetStack != null)
            {   
                targetStack.RejoinStack(this.gameObject);
            }
        }
    }
    
    void IControllable.ActivateControl()
    {
        //this.enabled = true;
        playerMovement.EnableMovement();
        Debug.Log($"Robot of type **{robotType}** control activated.");
    }

    void IControllable.DeactivateControl()
    {
        playerMovement.DisableMovement();
        //this.enabled = false;
    }
}