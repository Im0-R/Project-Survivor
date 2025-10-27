using kcp2k;
using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientSideInstanceManager : MonoBehaviour
{
    public static ClientSideInstanceManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }
    public void SwitchToInstance(string ip, ushort port)
    {
        StartCoroutine(SwitchRoutine(ip, port));
    }

    private IEnumerator SwitchRoutine(string ip, ushort port)
    {
        Debug.Log("Preparing to switch instance...");

        // Deconnection if connected
        if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient();
            yield return new WaitForSeconds(0.5f);
        }

        // Get KcpTransport
        KcpTransport kcp = Transport.active as KcpTransport;
        if (kcp == null)
        {
            Debug.LogError("KcpTransport not found or inactive!");
            yield break;
        }

        // Transport configuration
        kcp.Port = port;
        NetworkManager.singleton.networkAddress = ip;

        Debug.Log($"Connecting to new instance at {ip}:{port}");

        // Loading new scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MapScene");
        while (!asyncLoad.isDone)
            yield return null;

        // Stating client
        NetworkManager.singleton.StartClient();

        Debug.Log("Switched to new instance successfully!");


    }
}
