using UnityEngine;
using UnityEngine.InputSystem;

public class RobotController : MonoBehaviour, IControllable
{
    [Header("Robot Type")]
    public string robotType = "Flyer";

    private PlayerMovement playerMovement;
    private PlayerFocusManager playerFocusManager;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerFocusManager = FindAnyObjectByType<PlayerFocusManager>();
    }
    public void TryRejoin()
    {
        Debug.Log($"Robot of type **{robotType}** is attempting to rejoin the stack.");
    }
    
    void IControllable.ActivateControl()
    {
        //this.enabled = true;
        playerMovement.enabled = true;
        Debug.Log($"Robot of type **{robotType}** control activated.");
    }

    void IControllable.DeactivateControl()
    {
        playerMovement.enabled = false;
        //this.enabled = false;
    }
}