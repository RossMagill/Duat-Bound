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
            // if (currentFocus == null)
            // {
            //     SetFocus(newTarget);
            // }

            SetFocus(newTarget);
        }
    }

    public void DeregisterControllable(GameObject target, GameObject newFocus)
    {
        if (!controllables.Contains(target))
        {
            return;
        }

        controllables.Remove(target);

        Debug.Log($"new focus index: {controllables.IndexOf(newFocus)}");
        SetFocus(newFocus);
    }

    public void SetFocus(GameObject newFocus)
    {
        if (currentFocus != null)
        {
            IControllable previousControllable = currentFocus.GetComponent<IControllable>();
            previousControllable?.DeactivateControl();
        }

        currentFocus = newFocus;
        currentFocusIndex = controllables.IndexOf(newFocus);

        if (currentFocus != null)
        {
            IControllable newControllable = currentFocus.GetComponent<IControllable>();
            newControllable?.ActivateControl();
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
            GameObject previousFocus = currentFocus;
            robotController.TryRejoin();

            if (previousFocus != currentFocus)
            {
                return;
            }
        }

        StackController stackController = currentFocus.GetComponent<StackController>();

        if (stackController != null)
        {
            stackController.HandleStackInputLogic();
        }
    }

    void Update()
    {
        if (stackInteractionAction != null && stackInteractionAction.action.WasPressedThisFrame())
        {
            HandleSharedAction();
        }

        if (switchFocusAction != null && switchFocusAction.action.WasPressedThisFrame())
        {
            if (controllables.Count <= 1) return;
            currentFocusIndex = (currentFocusIndex + 1) % controllables.Count;
            if (controllables[currentFocusIndex] != null)
            {
                SetFocus(controllables[currentFocusIndex]);
            }
        }
        
        // Debug.Log($"Current Focus: {currentFocus}, Index: {currentFocusIndex}");
    }
}
