using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class CanvasPartyTargetUI : MonoBehaviour
{
    public static CanvasPartyTargetUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI targetNameTMP;
    [SerializeField] private Button inviteButton;
    [SerializeField] private Button teleportButton;

    private NetworkEntity currentTarget;

    private void Awake()
    {
        Instance = this;

        if (panel != null)
            panel.SetActive(false);

        if (inviteButton != null)
            inviteButton.onClick.AddListener(OnInviteClicked);

        if (teleportButton != null)
            teleportButton.onClick.AddListener(OnTeleportClicked);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            TryOpenOnTarget();
        }

        if (Input.GetMouseButtonDown(0) && panel.activeSelf)
        {
            // Option simple : clic gauche ailleurs ferme le menu
            // Tu peux améliorer plus tard avec EventSystem.current.IsPointerOverGameObject()
            Close();
        }
    }

    private void TryOpenOnTarget()
    {
        if (Camera.main == null)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hitInfo))
        {
            Close();
            return;
        }

        NetworkEntity entHit = hitInfo.collider.GetComponentInParent<NetworkEntity>();

        if (entHit == null)
        {
            Close();
            return;
        }

        // Évite de s'inviter soi-même
        if (entHit.isLocalPlayer)
        {
            Close();
            return;
        }

        Open(entHit);
    }

    private void Open(NetworkEntity target)
    {
        currentTarget = target;

        if (targetNameTMP != null)
            targetNameTMP.text = target.StatComp != null ? target.StatComp.Name : "Player";

        if (panel != null)
        {
            panel.SetActive(true);
            panel.transform.position = Input.mousePosition;
        }
    }

    public void Close()
    {
        currentTarget = null;

        if (panel != null)
            panel.SetActive(false);
    }

    private void OnInviteClicked()
    {
        if (currentTarget == null)
            return;

        PlayerEntity localPlayer = NetworkClient.localPlayer.GetComponent<PlayerEntity>();
        if (localPlayer == null)
            return;

        localPlayer.CmdInviteToParty(currentTarget.netId);

        Close();
    }

    private void OnTeleportClicked()
    {
        if (currentTarget == null)
            return;

        PlayerEntity localPlayer = NetworkClient.localPlayer.GetComponent<PlayerEntity>();
        if (localPlayer == null)
            return;

        localPlayer.CmdTeleportToPartyMember(currentTarget.netId);

        Close();
    }
}