using System.Collections;
using AuthMessages;
using kcp2k;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientSideInstanceManager : MonoBehaviour
{
    public static ClientSideInstanceManager Instance { get; private set; }

    [Header("Loading")]
    [SerializeField] private string loadingSceneName = "Loading";
    [SerializeField] private float minimumLoadingTime = 1.0f;

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

        Debug.Log($"[ClientSideInstanceManager] StartClient -> {ip}:{port}");

        manager.StartClient();

        yield return StartCoroutine(SendHubAuthWhenConnected());

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

        AsyncOperation op = SceneManager.LoadSceneAsync(loadingSceneName, LoadSceneMode.Single);

        while (!op.isDone)
            yield return null;

        Debug.Log("[ClientSideInstanceManager] Loading scene loaded");
    }

    private IEnumerator SendHubAuthWhenConnected()
    {
        float timeout = 10f;
        float timer = 0f;

        while (!NetworkClient.isConnected)
        {
            timer += Time.deltaTime;

            if (timer >= timeout)
            {
                Debug.LogError("[ClientSideInstanceManager] Timeout while waiting for connection");
                yield break;
            }

            yield return null;
        }

        if (string.IsNullOrEmpty(ClientAccountAPI.CurrentUsername))
        {
            Debug.LogWarning("[ClientSideInstanceManager] Cannot send HubAuthMessage, CurrentUsername is empty");
            yield break;
        }

        NetworkClient.Send(new HubAuthMessage
        {
            username = ClientAccountAPI.CurrentUsername
        });

        Debug.Log($"[ClientSideInstanceManager] Sent HubAuthMessage as {ClientAccountAPI.CurrentUsername}");
    }
}