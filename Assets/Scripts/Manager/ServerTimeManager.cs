using Mirror;
using UnityEngine;

public class ServerTimeManager : NetworkBehaviour
{
    public static ServerTimeManager instance;

    [SyncVar] private bool isPaused;

    public override void OnStartServer()
    {
        instance = this;
    }

    public override void OnStopServer()
    {
        if (instance == this) instance = null;
    }

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
        Time.timeScale = paused ? 0f : 1f;
        Debug.Log($"[Client] Pause={paused} timeScale={Time.timeScale}");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Time.timeScale = isPaused ? 0f : 1f;
    }
}
