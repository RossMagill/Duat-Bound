using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(BoxCollider))]
public class StackController : MonoBehaviour, IControllable
{
    // public static StackController Instance { get; private set; }

    [SerializeField]
    private List<GameObject> stack = new();

    private BoxCollider box;
    private IMovement playerMovement;
    private PlayerFocusManager playerFocusManager;

    private void Awake()
    {
        box = GetComponent<BoxCollider>();
        playerMovement = GetComponent<IMovement>();
        playerFocusManager = FindAnyObjectByType<PlayerFocusManager>();
    }

    void Start()
    {
        Debug.Log($"Initial stack count: {stack.Count}");
        CalculateCollider();
    }

    public void RejoinStack(GameObject robot)
    {
        RobotController robotController = robot.GetComponent<RobotController>();
        string robotType = robotController.robotType;
        Debug.Log($"Robot of type **{robotType}** is rejoining the stack.");
        Rigidbody rb = GetRobotRigidbody(robot);
        float previousStackHeight = GetStackHeight();
        float offset = stack[0].transform.localScale.y / 2f;

        rb.isKinematic = true;
        rb.useGravity = false;
        BoxCollider boxCollider = robot.GetComponent<BoxCollider>();
        boxCollider.enabled = false;
        stack.Add(robot);
        robot.transform.SetParent(this.transform);
        playerFocusManager.DeregisterControllable(robot, this.gameObject);

        // Logic for positioning the robot in the stack
        if (robotType == "Flyer" || robotType == "Jumper")
        {
            robot.transform.localPosition = new Vector3(0, previousStackHeight + offset, 0);
        }
        else
        {
            robot.transform.localPosition = new Vector3(0, previousStackHeight, 0);
        }
        Debug.Log($"Robot **{robot}** rejoined the stack. Current stack count: {stack.Count}");
        CalculateCollider();
    }

    public void HandleStackInputLogic()
    {
        // Try to stack

        PopStack();
    }

    public void PopStack()
    {
        if (stack.Count > 1)
        {
            GameObject poppedRobot = stack[^1];
            playerFocusManager.RegisterControllable(poppedRobot);
            IMovement poppedMovement = poppedRobot.GetComponent<IMovement>();
            BoxCollider poppedBox = poppedRobot.GetComponent<BoxCollider>();
            poppedRobot.transform.SetParent(null);
            poppedBox.enabled = true;
            Rigidbody rb = GetRobotRigidbody(poppedRobot);
            rb.isKinematic = false;
            rb.useGravity = true;
            playerMovement.DisableMovement();
            poppedMovement?.EnableMovement();
            stack.RemoveAt(stack.Count - 1);
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
        float height = GetStackHeight();
        float offset = stack[0].transform.localScale.y / 2f;
        box.center = new Vector3(0f, (height / 2f) - offset, 0);
        box.size = new Vector3(2f, height, 0.2f);
    }

    private float GetStackHeight()
    {
            float height = 0;
            foreach (GameObject robot in stack)
            {
                height += (float)robot.transform.localScale.y;
            }
            return height;
    }

    void IControllable.ActivateControl()
    {
        playerMovement.EnableMovement();
        Debug.Log("StackController ActivateControl called.");
    }

    void IControllable.DeactivateControl()
    {
        playerMovement.DisableMovement();
        Debug.Log("StackController DeactivateControl called.");
    }
}