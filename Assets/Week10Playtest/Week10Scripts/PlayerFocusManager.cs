using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerFocusManager : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> controllables = new();

    [Header("Inputs")]
    [SerializeField] private InputActionReference switchFocusAction;
    [SerializeField] private InputActionReference stackInteractionAction;

    private GameObject currentFocus = null;
    private int currentFocusIndex = 0;

    void Start()
    {
        if (controllables.Count > 0)
        {
            currentFocus = controllables[0];
        }
        else
        {
            Debug.LogWarning("No controllable objects registered.");
        }
    }

    void OnEnable()
    {
        switchFocusAction?.action.Enable();
        stackInteractionAction?.action.Enable();
    }

    void OnDisable()
    {
        switchFocusAction?.action.Disable(); 
        stackInteractionAction?.action.Disable();
    }

    public void RegisterControllable(GameObject newTarget)
    {
        if (!controllables.Contains(newTarget))
        {
            controllables.Add(newTarget);
            if (currentFocus == null)
            {
                SetFocus(newTarget);
            }
        }
    }

    // public void DeregisterControllable(GameObject target)
    // {
    //     if (controllables.Contains(target))
    //     {
    //         controllables.Remove(target);
    //         if (currentFocus == target)
    //         {
    //             currentFocus = controllables.Count > 0 ? controllables[0] : null;
    //         }
    //     }
    // }

    public void SetFocus(GameObject newFocus)
    {
        if (controllables.Contains(newFocus))
        {
            currentFocus = newFocus;
        }
        else
        {
            Debug.LogWarning("Attempted to set focus to an unregistered controllable.");
        }
    }   

    private void HandleSharedAction()
    {
        if (currentFocus == null)
        {
            Debug.LogWarning("No current focus to handle shared action.");
            return;
        }

        RobotController robotController = currentFocus.GetComponent<RobotController>();

        if (robotController != null && !currentFocus.GetComponent<StackController>())
        {
            robotController.TryRejoin();
        }

        StackController stackController = currentFocus.GetComponent<StackController>();

        if (stackController != null)
        {
            stackController.HandleStackInputLogic();
        }
    }

    void SwitchFocus()
    {
        // TODO
    }

    void Update()
    {
        if (stackInteractionAction != null && stackInteractionAction.action.WasPressedThisFrame())
        {
            HandleSharedAction();
        }

        if (switchFocusAction != null && switchFocusAction.action.WasPressedThisFrame())
        {
            SwitchFocus();
        }
    }
}
