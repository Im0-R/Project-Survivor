using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;

public class CanvasPartyTargetUI : MonoBehaviour
{
    public static CanvasPartyTargetUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI targetNameTMP;
    [SerializeField] private Button inviteButton;
    [SerializeField] private Button teleportButton;
    [SerializeField] private Button tradeButton;

    [Header("Debug")]
    [SerializeField] private bool enableClientLogs = true;

    private NetworkEntity currentTarget;

    private void Awake()
    {
        Instance = this;

        if (panel != null)
            panel.SetActive(false);

        if (inviteButton != null)
        {
            inviteButton.onClick.RemoveListener(OnInviteClicked);
            inviteButton.onClick.AddListener(OnInviteClicked);
        }

        if (teleportButton != null)
        {
            teleportButton.onClick.RemoveListener(OnTeleportClicked);
            teleportButton.onClick.AddListener(OnTeleportClicked);
        }

        if (tradeButton != null)
        {
            tradeButton.onClick.RemoveListener(OnTradeClicked);
            tradeButton.onClick.AddListener(OnTradeClicked);
        }

        LogClient("Awake initialized.");
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            TryOpenOnTarget();
            return;
        }

        if (Input.GetMouseButtonDown(0) && panel != null && panel.activeSelf)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                LogClient("Left click over UI, not closing.");
                return;
            }

            LogClient("Left click outside UI, closing.");
            Close();
        }
    }

    private void TryOpenOnTarget()
    {
        if (Camera.main == null)
        {
            LogClient("Camera.main is null.");
            Close();
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hitInfo))
        {
            LogClient("Right click raycast hit nothing.");
            Close();
            return;
        }

        LogClient($"Right click hit collider={hitInfo.collider.name}.");

        NetworkEntity entHit = FindNetworkEntity(hitInfo.collider);

        if (entHit == null)
        {
            LogClient($"No NetworkEntity found from collider={hitInfo.collider.name}.");
            Close();
            return;
        }

        LogClient(
            $"NetworkEntity found: {GetEntityName(entHit)} | " +
            $"netId={entHit.netId} | isLocalPlayer={entHit.isLocalPlayer}"
        );

        if (entHit.isLocalPlayer)
        {
            LogClient("Target is local player, closing menu.");
            Close();
            return;
        }

        Open(entHit);
    }

    private void Open(NetworkEntity target)
    {
        currentTarget = target;

        if (targetNameTMP != null)
            targetNameTMP.text = GetEntityName(target);

        if (panel != null)
        {
            panel.SetActive(true);
            panel.transform.position = Input.mousePosition;
        }

        LogClient($"Opened party target menu for {GetEntityName(target)} | netId={target.netId}");
    }

    public void Close()
    {
        if (currentTarget != null)
            LogClient($"Closed party target menu. Previous target={GetEntityName(currentTarget)}");

        currentTarget = null;

        if (panel != null)
            panel.SetActive(false);
    }

    private void OnInviteClicked()
    {
        LogClient("Invite button clicked.");

        if (currentTarget == null)
        {
            LogClient("Invite clicked but currentTarget is null.");
            return;
        }

        if (NetworkClient.localPlayer == null)
        {
            LogClient("Invite clicked but NetworkClient.localPlayer is null.");
            return;
        }

        PlayerEntity localPlayer = NetworkClient.localPlayer.GetComponent<PlayerEntity>();

        if (localPlayer == null)
        {
            LogClient("Invite clicked but local player has no PlayerEntity.");
            return;
        }

        LogClient($"Sending party invite to {GetEntityName(currentTarget)} | netId={currentTarget.netId}");

        localPlayer.CmdInviteToParty(currentTarget.netId);

        Close();
    }

    private void OnTeleportClicked()
    {
        LogClient("Teleport button clicked.");

        if (currentTarget == null)
        {
            LogClient("Teleport clicked but currentTarget is null.");
            return;
        }

        if (NetworkClient.localPlayer == null)
        {
            LogClient("Teleport clicked but NetworkClient.localPlayer is null.");
            return;
        }

        PlayerEntity localPlayer = NetworkClient.localPlayer.GetComponent<PlayerEntity>();

        if (localPlayer == null)
        {
            LogClient("Teleport clicked but local player has no PlayerEntity.");
            return;
        }

        LogClient($"Sending teleport request to {GetEntityName(currentTarget)} | netId={currentTarget.netId}");

        localPlayer.CmdTeleportToPartyMember(currentTarget.netId);

        Close();
    }

    private void OnTradeClicked()
    {
        LogClient("Trade button clicked.");

        if (currentTarget == null)
        {
            LogClient("Trade clicked but currentTarget is null.");
            return;
        }

        if (NetworkClient.localPlayer == null)
        {
            LogClient("Trade clicked but NetworkClient.localPlayer is null.");
            return;
        }

        PlayerTrade localTrade = NetworkClient.localPlayer.GetComponent<PlayerTrade>();

        if (localTrade == null)
        {
            LogClient("Trade clicked but local player has no PlayerTrade.");
            return;
        }

        PlayerTrade targetTrade = currentTarget.GetComponent<PlayerTrade>();

        if (targetTrade == null)
            targetTrade = currentTarget.GetComponentInParent<PlayerTrade>();

        if (targetTrade == null)
            targetTrade = currentTarget.GetComponentInChildren<PlayerTrade>();

        if (targetTrade == null)
        {
            LogClient("Trade clicked but target has no PlayerTrade.");
            return;
        }

        LogClient(
            $"Sending trade request to {GetEntityName(currentTarget)} | " +
            $"targetTradeNetId={targetTrade.netId}"
        );

        localTrade.CmdRequestTrade(targetTrade.netId);

        Close();
    }

    private NetworkEntity FindNetworkEntity(Collider hitCollider)
    {
        if (hitCollider == null)
            return null;

        NetworkEntity entity = hitCollider.GetComponent<NetworkEntity>();
        if (entity != null)
            return entity;

        entity = hitCollider.GetComponentInParent<NetworkEntity>();
        if (entity != null)
            return entity;

        entity = hitCollider.GetComponentInChildren<NetworkEntity>();
        if (entity != null)
            return entity;

        Transform root = hitCollider.transform.root;
        if (root != null)
            return root.GetComponentInChildren<NetworkEntity>();

        return null;
    }

    private string GetEntityName(NetworkEntity entity)
    {
        if (entity == null)
            return "null";

        if (entity.StatComp != null && !string.IsNullOrWhiteSpace(entity.StatComp.Name))
            return entity.StatComp.Name;

        return entity.name;
    }

    private void LogClient(string message)
    {
        if (!enableClientLogs)
            return;

        Debug.Log($"[Client][CanvasPartyTargetUI] {message}");
    }
}