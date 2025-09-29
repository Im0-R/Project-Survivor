using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class GameLobbyManager : MonoBehaviour
{
    public static GameLobbyManager Instance;
    private static bool _isAuthenticating = false;
    private Lobby currentLobby;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {

        await InitServicesAsync();
    }
    private async Task InitServicesAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();

            // Évite le double sign-in
            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.LogWarning("Déjà connecté à UGS.");
                return;
            }

            if (_isAuthenticating)
            {
                Debug.LogWarning("Authentification déjà en cours...");
                return;
            }

            _isAuthenticating = true;

            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            Debug.Log($"Connecté: {AuthenticationService.Instance.PlayerId}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Erreur d'authentification: {ex}");
        }
        finally
        {
            _isAuthenticating = false;
        }
    }
    // HOST
    public async Task<string> CreateGameAsync(int maxPlayers = 4)
    {
        try
        {
            int maxConnections = Mathf.Max(1, maxPlayers - 1);

            // 1) allocation Relay (host)
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // 2) config transport => on passe les données brutes (compatible avec la plupart des versions)
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            var relayServer = allocation.RelayServer; // ip/port
            transport.SetRelayServerData(
                relayServer.IpV4,
                (ushort)relayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                null, // hostConnectionData (host n'en a pas ici)
                false // isSecure (set true si tu utilises dtls/wss ; adapter si besoin)
            );

            // 3) Create Lobby et stocker le relayJoinCode dans les data (pour que les clients le récupèrent)
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    { "relayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
                },
                Player = new Player(id: AuthenticationService.Instance.PlayerId)
            };

            currentLobby = await LobbyService.Instance.CreateLobbyAsync("MyLobby", maxPlayers, options);
            Debug.Log($"Lobby créé : {currentLobby.LobbyCode}  (relay join code: {relayJoinCode})");

            // 4) start host
            NetworkManager.Singleton.StartHost();

            return currentLobby.LobbyCode;
        }
        catch (Exception ex)
        {
            Debug.LogError($"CreateGameAsync failed: {ex}");
            throw;
        }
    }

    // JOIN
    public async Task JoinGameAsync(string lobbyCode)
    {
        try
        {
            // 1) join lobby
            currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
            Debug.Log($"Rejoint le lobby {lobbyCode}");

            // 2) récupérer le relay join code stocké par l'host dans le lobby
            string relayJoinCode = currentLobby.Data["relayJoinCode"].Value;

            // 3) join allocation Relay (client)
            JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

            // 4) config transport (client)
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            var relayServer = joinAlloc.RelayServer;
            transport.SetRelayServerData(
                relayServer.IpV4,
                (ushort)relayServer.Port,
                joinAlloc.AllocationIdBytes,
                joinAlloc.Key,
                joinAlloc.ConnectionData,
                joinAlloc.HostConnectionData, // client fournit hostConnectionData
                false
            );

            // 5) start client
            NetworkManager.Singleton.StartClient();
        }
        catch (Exception ex)
        {
            Debug.LogError($"JoinGameAsync failed: {ex}");
            throw;
        }
    }


    public string GetCode()
    {
        if (currentLobby == null) return "No Lobby";
        return currentLobby.LobbyCode;
    }
}
