using Mirror;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
public class PlayerMovement : NetworkBehaviour
{
    private NavMeshAgent agent;
    private PlayerInputActions inputActions;
    private Camera mainCamera;
    private GameObject interactableTarget;
    private bool isHoldingClick = false;

    public bool InputBlocked { get; private set; }

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.MoveClick.started += OnMoveClickStarted;
        inputActions.Player.MoveClick.canceled += OnMoveClickCanceled;
    }

    private void OnDisable()
    {
        inputActions.Player.MoveClick.started -= OnMoveClickStarted;
        inputActions.Player.MoveClick.canceled -= OnMoveClickCanceled;
        inputActions.Disable();
    }

    private void Start()
    {
        if (!isLocalPlayer)
            return;

        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
            agent.updateRotation = false;

        mainCamera = Camera.main;
    }

    public void SetInputBlocked(bool blocked)
    {
        InputBlocked = blocked;

        if (blocked)
        {
            isHoldingClick = false;
            interactableTarget = null;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
        }
    }

    private void OnMoveClickStarted(InputAction.CallbackContext ctx)
    {
        if (!isLocalPlayer)
            return;

        if (InputBlocked)
            return;

        isHoldingClick = true;
    }

    private void OnMoveClickCanceled(InputAction.CallbackContext ctx)
    {
        if (!isLocalPlayer)
            return;

        isHoldingClick = false;
    }

    private void Update()
    {
        if (!isLocalPlayer)
            return;

        if (InputBlocked)
            return;

        if (agent == null)
            return;

        InteractTarget();
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer)
            return;

        if (InputBlocked)
            return;

        if (agent == null)
            return;

        if (isHoldingClick)
            MoveToCursor();
    }

    private void MoveToCursor()
    {
        // Block clicks when mouse is over UI
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        Camera cam = GetCamera();

        if (cam == null)
            return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (ClickInteractable(hit))
                return;

            agent.SetDestination(hit.point);

            Vector3 direction = hit.point - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(direction.normalized);
        }
    }

    private bool ClickInteractable(RaycastHit hit)
    {
        if (hit.collider.TryGetComponent<IInteractable>(out IInteractable interactable))
        {
            interactableTarget = hit.collider.gameObject;

            if (agent != null && agent.isOnNavMesh)
                agent.SetDestination(interactableTarget.transform.position);

            return true;
        }

        interactableTarget = null;
        return false;
    }

    private void InteractTarget()
    {
        if (!interactableTarget)
            return;

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

    private Camera GetCamera()
    {
        if (mainCamera != null)
            return mainCamera;

        if (CameraFollow.LocalCamera != null)
            mainCamera = CameraFollow.LocalCamera;
        else
            mainCamera = Camera.main;

        return mainCamera;
    }

    private void OnDrawGizmos()
    {
        if (agent != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(agent.destination, 0.2f);
        }
    }
}