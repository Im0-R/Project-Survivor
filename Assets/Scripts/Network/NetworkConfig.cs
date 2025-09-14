using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetworkConfig : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;

    public void StartHost()
    {
        var transport = (UnityTransport)networkManager.NetworkConfig.NetworkTransport;
        transport.SetConnectionData("127.0.0.1", 7777);
        networkManager.StartHost();
    }

    public void StartClient()
    {
        var transport = (UnityTransport)networkManager.NetworkConfig.NetworkTransport;
        transport.SetConnectionData("127.0.0.1", 7777);
        networkManager.StartClient();
    }

    public void StartServer()
    {
        var transport = (UnityTransport)networkManager.NetworkConfig.NetworkTransport;
        transport.SetConnectionData("127.0.0.1", 7777);
        networkManager.StartServer();
    }
}
