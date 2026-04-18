using System.Collections;
using kcp2k;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientSideInstanceManager : MonoBehaviour
{
    public static ClientSideInstanceManager Instance { get; private set; }

    private bool isSwitching;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SwitchToInstance(ushort port, string ip, string sceneName)
    {
        if (isSwitching)
        {
            Debug.LogWarning("[ClientSideInstanceManager] Already switching");
            return;
        }

        StartCoroutine(SwitchRoutine(port, ip, sceneName));
    }

    private IEnumerator SwitchRoutine(ushort port, string ip, string sceneName)
    {
        isSwitching = true;

        Debug.Log($"[ClientSideInstanceManager] Switching to {ip}:{port}, scene={sceneName}");

        if (NetworkManager.singleton == null)
        {
            Debug.LogError("[ClientSideInstanceManager] NetworkManager.singleton is null");
            isSwitching = false;
            yield break;
        }

        // 1) Stop client proprement
        if (NetworkClient.isConnected || NetworkClient.isConnecting)
        {
            Debug.Log("[ClientSideInstanceManager] Stop current client");
            NetworkManager.singleton.StopClient();

            while (NetworkClient.isConnected || NetworkClient.isConnecting)
                yield return null;
        }

        yield return null;

        // 2) Charger la bonne scène localement AVANT de se reconnecter
        if (!string.IsNullOrWhiteSpace(sceneName) &&
            SceneManager.GetActiveScene().name != sceneName)
        {
            Debug.Log($"[ClientSideInstanceManager] Loading local scene: {sceneName}");

            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
            while (!load.isDone)
                yield return null;

            yield return null;
        }

        // 3) Config transport
        KcpTransport kcp = NetworkManager.singleton.transport as KcpTransport;
        if (kcp == null)
        {
            Debug.LogError("[ClientSideInstanceManager] KcpTransport not found");
            isSwitching = false;
            yield break;
        }

        kcp.Port = port;
        NetworkManager.singleton.networkAddress = ip;

        Debug.Log($"[ClientSideInstanceManager] Connecting to {ip}:{port}");
        NetworkManager.singleton.StartClient();

        isSwitching = false;
    }
}