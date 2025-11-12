using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider))]
public class StackController : MonoBehaviour, IControllable
{
    // public static StackController Instance { get; private set; }

    [SerializeField]
    private List<GameObject> stack = new();

    private BoxCollider box;
    private PlayerMovement playerMovement;
    private GameObject activeRobotObject = null;
    private PlayerFocusManager playerFocusManager;

    private void Awake()
    {
        box = GetComponent<BoxCollider>();
        playerMovement = GetComponent<PlayerMovement>();
        playerFocusManager = FindAnyObjectByType<PlayerFocusManager>();
    }

    void Start()
    {
        Debug.Log($"Initial stack count: {stack.Count}");
        CalculateCollider();
    }

    public void RejoinStack(GameObject robot)
    {
        stack.Add(activeRobotObject);
        activeRobotObject.transform.SetParent(this.transform);
        Debug.Log($"Robot **{robot}** rejoined the stack. Current stack count: {stack.Count}");
        CalculateCollider();
    }

    public void HandleStackInputLogic()
    {
        // if blah blah

        PopStack();
    }

    public void PopStack()
    {
        if (stack.Count > 1)
        {
            GameObject poppedRobot = stack[^1];
            playerFocusManager.RegisterControllable(poppedRobot);
            PlayerMovement poppedPlayerMovement = poppedRobot.GetComponent<PlayerMovement>();
            BoxCollider poppedBox = poppedRobot.GetComponent<BoxCollider>();
            poppedRobot.transform.SetParent(null);
            poppedBox.enabled = true;
            Rigidbody rb = GetRobotRigidbody(poppedRobot);
            rb.isKinematic = false;
            rb.useGravity = true;
            playerMovement.enabled = false;
            poppedPlayerMovement.enabled = true;
            stack.RemoveAt(stack.Count - 1);
            // Debug.Log($"Robot **{poppedRobot}** popped off the stack.");
            CalculateCollider();
        }
    }

    private Rigidbody GetRobotRigidbody(GameObject robotObject)
    {
        return robotObject.GetComponent<Rigidbody>();
    }

    public int GetStackCount()
    {
        return stack.Count;
    }

    private void CalculateCollider()
    {
        float height = 0;
        foreach (GameObject robot in stack)
        {
            height += (float)robot.transform.localScale.y;
            // Debug.Log($"Adding {robot.transform.localScale.y} to collider height from robot {robot.name}");
        }
        float offset = stack[0].transform.localScale.y / 2f;
        box.center = new Vector3(0f, (height / 2f) - offset, 0);
        box.size = new Vector3(2f, height, 0.2f);
    }

    void IControllable.ActivateControl()
    {
        playerMovement.enabled = true;
        Debug.Log("StackController ActivateControl called.");
    }

    void IControllable.DeactivateControl()
    {
        playerMovement.enabled = false;
        Debug.Log("StackController DeactivateControl called.");
    }
}