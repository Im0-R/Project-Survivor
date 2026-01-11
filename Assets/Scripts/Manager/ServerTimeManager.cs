using Mirror;
using UnityEngine;

public class ServerTimeManager : NetworkBehaviour
{
    public static ServerTimeManager instance;

    [SyncVar] private bool isPaused;

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Appelé côté serveur
    [Server]
    public void PauseGame()
    {
        if (isPaused) return;
        isPaused = true;
        RpcSetPaused(true);
        Debug.Log("[Server] PauseGame");
    }

    [Server]
    public void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;
        RpcSetPaused(false);
        Debug.Log("[Server] ResumeGame");
    }

    [ClientRpc]
    private void RpcSetPaused(bool paused)
    {
        // IMPORTANT : timeScale est par client
        Time.timeScale = paused ? 0f : 1f;
        Debug.Log($"[Client] Pause={paused} timeScale={Time.timeScale}");
    }

    // Utile si un client rejoint pendant la pause (il recevra la SyncVar)
    public override void OnStartClient()
    {
        base.OnStartClient();
        Time.timeScale = isPaused ? 0f : 1f;
    }

    public bool IsPaused => isPaused;
}
