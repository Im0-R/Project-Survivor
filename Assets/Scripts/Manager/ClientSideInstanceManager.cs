using System.Collections;
using kcp2k;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientSideInstanceManager : MonoBehaviour
{
    public static ClientSideInstanceManager Instance { get; private set; }

    [Header("Loading")]
    [SerializeField] private string loadingSceneName = "Loading";
    [SerializeField] private float minimumLoadingTime = 1f;

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

        Debug.Log("[ClientSideInstanceManager] Awake");
    }

    public void SwitchToInstance(ushort port, string ip, string sceneName)
    {
        Debug.Log($"[ClientSideInstanceManager] SwitchToInstance called | ip={ip} | port={port} | scene={sceneName}");

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

        NetworkManager manager = NetworkManager.singleton;

        if (manager == null)
        {
            Debug.LogError("[ClientSideInstanceManager] NetworkManager.singleton is null");
            isSwitching = false;
            yield break;
        }

        float startTime = Time.time;

        string oldOfflineScene = manager.offlineScene;
        manager.offlineScene = "";

        yield return LoadLoadingScene();

        if (NetworkClient.isConnected || NetworkClient.isConnecting)
        {
            Debug.Log("[ClientSideInstanceManager] Stop current client");

            manager.StopClient();

            while (NetworkClient.isConnected || NetworkClient.isConnecting)
                yield return null;

            Debug.Log("[ClientSideInstanceManager] Current client stopped");
        }

        // Petit délai pour laisser Mirror nettoyer proprement
        yield return new WaitForSeconds(0.25f);

        KcpTransport kcp = manager.transport as KcpTransport;

        if (kcp == null)
            kcp = manager.GetComponent<KcpTransport>();

        if (kcp == null)
        {
            Debug.LogError("[ClientSideInstanceManager] KcpTransport not found");

            manager.offlineScene = oldOfflineScene;
            isSwitching = false;
            yield break;
        }

        manager.networkAddress = ip;
        kcp.Port = port;

        // IMPORTANT
        // Le prochain serveur est un serveur gameplay
        // donc OnClientConnect devra faire Ready + AddPlayer
        ClientAccountAPI.ConnectingToHub = true;

        Debug.Log($"[ClientSideInstanceManager] StartClient -> {ip}:{port}");

        manager.StartClient();

        while (Time.time - startTime < minimumLoadingTime)
            yield return null;

        manager.offlineScene = oldOfflineScene;
        isSwitching = false;
    }

    private IEnumerator LoadLoadingScene()
    {
        if (SceneManager.GetActiveScene().name == loadingSceneName)
            yield break;

        Debug.Log($"[ClientSideInstanceManager] Loading transition scene -> {loadingSceneName}");

        AsyncOperation op =
            SceneManager.LoadSceneAsync(loadingSceneName, LoadSceneMode.Single);

        while (!op.isDone)
            yield return null;

        Debug.Log("[ClientSideInstanceManager] Loading scene loaded");
    }
}