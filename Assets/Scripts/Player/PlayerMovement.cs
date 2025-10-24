using Mirror;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    private NavMeshAgent agent;
    private PlayerInputActions inputActions;
    private Camera mainCamera;
    private GameObject interactableTarget;
    private bool isHoldingClick = false;

    void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.MoveClick.started += ctx => isHoldingClick = true;
        inputActions.Player.MoveClick.canceled += ctx => isHoldingClick = false;
    }

    void OnDisable()
    {
        inputActions.Player.MoveClick.started -= ctx => isHoldingClick = true;
        inputActions.Player.MoveClick.canceled -= ctx => isHoldingClick = false;
        inputActions.Disable();
    }

    void Start()
    {
        if (!isLocalPlayer) return;

        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;  // disable automatic rotation
        mainCamera = Camera.main;
    }
    private void Update()
    {
        if (!isLocalPlayer) return;


        InteractTarget();
    }
    private void FixedUpdate()
    {
        if (!isLocalPlayer) return;
        if (isHoldingClick)
        {
            MoveToCursor();
        }
    }
    private void MoveToCursor()
    {
        if (!isLocalPlayer) return;

        // Get the mouse position in world space
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Check if the hit point is an interactable object
            if (ClickInteractable(hit)) return;

            agent.SetDestination(hit.point);

            Vector3 direction = (hit.point - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }

    private bool ClickInteractable(RaycastHit hit)
    {
        if (hit.collider.TryGetComponent<IInteractable>(out IInteractable interactable))
        {
            interactableTarget = hit.collider.gameObject;
            agent.SetDestination(interactableTarget.transform.position);
        }
        return false;
    }
    private void OnDrawGizmos()
    {
        if (agent != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(agent.destination, 0.2f);
        }
    }
    private void InteractTarget()
    {
        if (!interactableTarget) return;

        float distance = Vector3.Distance(transform.position, interactableTarget.transform.position);
        if (distance < 3f)
        {
            if (interactableTarget.TryGetComponent<IInteractable>(out IInteractable interactable))
            {
                interactable.OnInteract();
                interactableTarget = null;
            }
        }
    }
}
