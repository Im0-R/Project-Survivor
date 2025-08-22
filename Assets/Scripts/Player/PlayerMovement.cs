using UnityEngine;
using Unity.Netcode;
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
        if (!IsOwner)
        {
            GetComponent<Renderer>().material.color = Color.red;
            return;
        }

        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;  // désactive la rotation automatique
        mainCamera = Camera.main;
    }

    private void OnMoveClick(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        // Récupère la position de la souris
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            agent.SetDestination(hit.point);

            // Optionnel : tourner le joueur vers la destination
            Vector3 direction = (hit.point - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }
}
