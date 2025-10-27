using Mirror;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    private NavMeshAgent agent;
    private PlayerInputActions inputActions;
    private Camera mainCamera;

    void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.MoveClick.performed += OnMoveClick;
    }

    void OnDisable()
    {
        inputActions.Player.MoveClick.performed -= OnMoveClick;
        inputActions.Disable();
    }

    void Start()
    {
        if (!isLocalPlayer) return;

        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;  // disable automatic rotation
        mainCamera = Camera.main;
    }

    private void OnMoveClick(InputAction.CallbackContext context)
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
        if (GetComponent<Collider>().TryGetComponent<IInteractable>(out IInteractable interactable))
        {
            if (Vector3.Distance(transform.position, hit.point) <= 1f)
            {
                interactable.OnInteract();
                return true;
            }
        }
        return false;
    }
}
