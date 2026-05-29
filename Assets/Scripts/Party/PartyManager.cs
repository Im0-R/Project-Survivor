using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PartyManager : NetworkBehaviour
{
    public static PartyManager Instance;

    [Header("Instance")]
    [SerializeField] private string publicServerIp = "72.60.212.58";

    private void Awake()
    {
        Instance = this;
    }

    [Server]
    public void InvitePlayer(PlayerEntity inviter, uint targetNetId)
    {
        if (inviter == null)
            return;

        if (!NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetIdentity))
            return;

        PlayerEntity target = targetIdentity.GetComponent<PlayerEntity>();

        if (target == null)
            return;

        string inviterName = inviter.connectionToClient.authenticationData as string;
        string targetName = target.connectionToClient.authenticationData as string;

        if (string.IsNullOrWhiteSpace(inviterName) || string.IsNullOrWhiteSpace(targetName))
            return;

        DatabaseManager.CreateParty(inviterName, targetName);

        RefreshPartyUI(inviterName);
        RefreshPartyUI(targetName);

        Debug.Log($"[Party] Created/updated party: {inviterName} + {targetName}");
    }

    [Server]
    public void TeleportToPartyMember(PlayerEntity requester, uint targetNetId)
    {
        if (requester == null)
            return;

        if (!NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetIdentity))
            return;

        PlayerEntity target = targetIdentity.GetComponent<PlayerEntity>();

        if (target == null)
            return;

        string targetName = target.connectionToClient.authenticationData as string;

        TeleportToPartyMemberByName(requester, targetName);
    }

    [Server]
    public void TeleportToPartyMemberByName(PlayerEntity requester, string memberName)
    {
        if (requester == null)
            return;

        string requesterName = requester.connectionToClient.authenticationData as string;

        if (string.IsNullOrWhiteSpace(requesterName) || string.IsNullOrWhiteSpace(memberName))
        {
            Debug.LogWarning("[PartyTP] requesterName or memberName missing.");
            return;
        }

        if (requesterName == memberName)
        {
            Debug.LogWarning("[PartyTP] Player tried to teleport to self.");
            return;
        }

        if (!DatabaseManager.AreInSameParty(requesterName, memberName))
        {
            Debug.LogWarning($"[PartyTP] {requesterName} and {memberName} are not in same party.");
            return;
        }

        PlayerLocationData targetLocation = DatabaseManager.GetPlayerLocation(memberName);

        if (targetLocation == null)
        {
            Debug.LogWarning($"[PartyTP] No DB location found for {memberName}.");
            return;
        }

        int currentPort = GetCurrentServerPort();
        string currentScene = SceneManager.GetActiveScene().name;

        Debug.Log(
            $"[PartyTP] {requesterName} wants to join {memberName}. " +
            $"Target={targetLocation.CurrentScene}:{targetLocation.CurrentPort}, Current={currentScene}:{currentPort}"
        );

        if (targetLocation.CurrentPort == currentPort && targetLocation.CurrentScene == currentScene)
        {
            TeleportNearMemberInCurrentInstance(requester, memberName);
            return;
        }

        requester.TargetSwitchToPartyMemberInstance(
            requester.connectionToClient,
            publicServerIp,
            targetLocation.CurrentPort,
            targetLocation.CurrentScene,
            memberName
        );
    }

    [Server]
    public void CompletePartyTeleport(PlayerEntity requester, string memberName)
    {
        if (requester == null)
            return;

        string requesterName = requester.connectionToClient.authenticationData as string;

        if (!DatabaseManager.AreInSameParty(requesterName, memberName))
        {
            Debug.LogWarning($"[PartyTP] Complete failed, {requesterName} and {memberName} are not in same party.");
            return;
        }

        TeleportNearMemberInCurrentInstance(requester, memberName);
    }

    [Server]
    private void TeleportNearMemberInCurrentInstance(PlayerEntity requester, string memberName)
    {
        PlayerEntity target = FindPlayerByUsername(memberName);

        if (target == null)
        {
            Debug.LogWarning($"[PartyTP] Target not found in this instance: {memberName}");
            return;
        }

        if (target == requester)
        {
            Debug.LogWarning("[PartyTP] Target is requester.");
            return;
        }

        requester.transform.position = target.transform.position + target.transform.right * 1.5f;

        Debug.Log($"[PartyTP] {GetUsername(requester)} teleported near {memberName}");
    }

    [Server]
    private PlayerEntity FindPlayerByUsername(string username)
    {
        foreach (PlayerEntity player in FindObjectsByType<PlayerEntity>(FindObjectsSortMode.None))
        {
            if (player == null || player.connectionToClient == null)
                continue;

            string playerUsername = player.connectionToClient.authenticationData as string;

            if (playerUsername == username)
                return player;
        }

        return null;
    }

    [Server]
    public static void UpdateLocationFor(PlayerEntity player, int port)
    {
        if (player == null || player.connectionToClient == null)
            return;

        string username = player.connectionToClient.authenticationData as string;

        if (string.IsNullOrWhiteSpace(username))
            return;

        string sceneName = SceneManager.GetActiveScene().name;

        DatabaseManager.UpdatePlayerLocation(username, port, sceneName);

        if (Instance != null)
            Instance.RefreshPartyUI(username);
    }

    [Server]
    public void RefreshPartyUI(string username)
    {
        string[] members = DatabaseManager.GetPartyMembers(username);

        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn == null || conn.identity == null)
                continue;

            string connUsername = conn.authenticationData as string;

            if (connUsername == username)
            {
                PlayerEntity player = conn.identity.GetComponent<PlayerEntity>();

                if (player != null)
                    player.TargetReceivePartyMembers(conn, members);
            }
        }
    }

    [Server]
    public void RefreshPartyUIFor(PlayerEntity player)
    {
        if (player == null || player.connectionToClient == null)
            return;

        string username = player.connectionToClient.authenticationData as string;

        if (string.IsNullOrWhiteSpace(username))
            return;

        RefreshPartyUI(username);
    }

    [Server]
    private int GetCurrentServerPort()
    {
        kcp2k.KcpTransport kcp = Transport.active as kcp2k.KcpTransport;

        if (kcp == null)
            kcp = FindFirstObjectByType<kcp2k.KcpTransport>();

        if (kcp == null)
        {
            Debug.LogWarning("[PartyTP] Cannot find KcpTransport, current port unknown.");
            return -1;
        }

        return kcp.Port;
    }

    private string GetUsername(PlayerEntity player)
    {
        if (player == null || player.connectionToClient == null)
            return "Unknown";

        return player.connectionToClient.authenticationData as string;
    }
}