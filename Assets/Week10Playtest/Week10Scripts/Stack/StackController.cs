// using System;
// using UnityEngine;
// using UnityEngine.InputSystem;
// using System.Collections.Generic;

// [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
// public class StackController : MonoBehaviour
// {
// 	[Serializable]
// 	public class RobotSlot
// 	{
// 		public string id;                 // "Flyer", "Jumper", "Runner"
// 		public GameObject robotObject;    // the child prefab in the stack
// 	}
	
// 	[Header("Stack")]
// 	[SerializeField] private List<string> stack = new() { "Flyer", "Jumper", "Runner" };

// 	[Header("Robots in Scene (match ids above, same order)")]
// 	[SerializeField] private List<RobotSlot> robots = new();
	
// 	[Header("Input")]
// 	[SerializeField] private InputActionReference popAction;

// 	private BoxCollider box;

// 	private void Awake()
// 	{
// 		box = GetComponent<BoxCollider>();
// 	}

// 	void OnEnable()
// 	{
// 		popAction?.action.Enable();
// 	}
	
//     void OnDisable()
//     {
//         popAction?.action.Disable();
//     }

// 	// Start is called once before the first execution of Update after the MonoBehaviour is created
// 	void Start()
// 	{
// 		SetColliderHeight();
// 	}

// 	// Sets the collider height based on the current stack
// 	void SetColliderHeight()
// 	{
// 		CalculateColliderThree();
// 		CalculateColliderTwo();
// 		CalculateColliderOne();
// 	}

// 	void CalculateCollider()
// 	{
//     // Placeholder for future implementation
    
//   }

// 	// Temporary: calculates the collider for a stack of three
// 	private void CalculateColliderThree()
// 	{
// 		if (stack.Count == 3)
// 		{
// 			box.center = new Vector3(0f, 2f, 0f);
// 			box.size = new Vector3(2f, 4.714541f, 0.2f);
// 		}
// 	}

// 	// Temporary: calculates the collider for a stack of two
// 	private void CalculateColliderTwo()
// 	{
// 		if (stack.Count == 2 && stack.Contains("Jumper") && stack.Contains("Runner"))
// 		{
// 			box.center = new Vector3(0f, 1f, 0f);
// 			box.size = new Vector3(2f, 3f, 0.2f);
// 		} else if (stack.Count == 2 && stack.Contains("Flyer") && stack.Contains("Runner"))
// 		{
// 			box.center = new Vector3(0f, 0.8f, 0f); //
// 			box.size = new Vector3(2f, 2.6f, 0.2f);
// 		} else if (stack.Count == 2 && stack.Contains("Flyer") && stack.Contains("Jumper"))
// 		{
// 			box.center = new Vector3(0f, 2.34f, 0f);
// 			box.size = new Vector3(2f, 3.75f, 0.2f);
// 		}
// 	}

// 	// Temporary: calculates the collider for a stack of one
// 	private void CalculateColliderOne()
// 	{
// 		if (stack.Count == 1 && stack[0] == "Runner")
// 		{
// 			box.center = new Vector3(0f, 0f, 0f);
// 			box.size = new Vector3(2f, 1f, 0.2f);
// 		}
// 		else if (stack.Count == 1 && stack[0] == "Jumper")
// 		{
// 			box.center = new Vector3(0f, 0f, 0f);
// 			box.size = new Vector3(1f, 1f, 0.2f);
// 		} else if (stack.Count == 1 && stack[0] == "Flyer")
// 		{
// 			box.center = new Vector3(0f, 0.1449941f, 0f);
// 			box.size = new Vector3(2f, 0.8673214f, 0.2f);
// 		}
// 	}

// 	// Update is called once per frame
// 	void Update()
// 	{
// 		if (popAction != null && popAction.action.WasPressedThisFrame())
// 		{
// 			OnPop();
// 		}
// 	}

// 	void OnPop()
// 	{
// 		if (stack.Count > 1)
// 		{
// 			stack.RemoveAt(0);
// 			SetColliderHeight();
// 		}
// 	}
// }

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider))]
public class StackController : MonoBehaviour
{
    public static StackController Instance { get; private set; }

    [SerializeField]
    private List<GameObject> stack = new();

    [Header("Inputs")]
    [SerializeField] private InputActionReference popAction;

    private BoxCollider box;
    private GameObject activeRobotObject = null;

    private void Awake()
    {
        box = GetComponent<BoxCollider>();
    }

    void OnEnable()
    {
        popAction?.action.Enable();
    }

    void OnDisable()
    {
        popAction?.action.Disable();
    }

    void Start()
    {
        Debug.Log($"Initial stack count: {stack.Count}");
        CalculateCollider();
    }

    public void RejoinStack(GameObject robot)
    {
        stack.Add(activeRobotObject);
        Debug.Log($"Robot **{robot}** rejoined the stack. Current stack count: {stack.Count}");
        CalculateCollider();
    }

    public void PopStack()
    {
        if (stack.Count > 1)
        {
            GameObject poppedRobot = stack[^1]; 
            stack.RemoveAt(stack.Count - 1);
            Debug.Log($"Robot **{poppedRobot}** popped off the stack.");
            CalculateCollider();
        }
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
            Debug.Log($"Adding {robot.transform.localScale.y} to collider height from robot {robot.name}");
        }
        float offset = stack[0].transform.localScale.y / 2f;
        box.center = new Vector3(0f, (height / 2f) - offset, 0);
        box.size = new Vector3(2f, height, 0.2f);
    }

    void Update()
    {
        if (popAction != null && popAction.action.WasPressedThisFrame())
        {
            if (activeRobotObject != null)
            {
                RejoinStack(activeRobotObject);
                activeRobotObject = null;
            }
            else
            {
                PopStack();
            }
        }
    }
}