using Mirror;
using UnityEngine;

public class ServerTimeManager : NetworkBehaviour
{
    public static ServerTimeManager instance;
    public static event System.Action<bool> OnPauseChanged;

    [SyncVar] public bool isPaused;

    public override void OnStartServer()
    {
        instance = this;
        Debug.Log($"[ServerTimeManager] OnStartServer netId={netId} scene={gameObject.scene.name}");
    }

    private void Awake()
    {
#if UNITY_SERVER
    // utile si timing bizarre, mais reste server-only
    if (instance == null) instance = this;
#endif
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

        // Freeze agents côté serveur
        foreach (var agent in FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        RpcSetPaused(true);
        Debug.Log("[Server] PauseGame");
    }

    [Server]
    public void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;

        foreach (var agent in FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            agent.isStopped = false;
        }

        RpcSetPaused(false);
        Debug.Log("[Server] ResumeGame");
    }

    [ClientRpc]
    private void RpcSetPaused(bool paused)
    {
        Time.timeScale = paused ? 0f : 1f;
        OnPauseChanged?.Invoke(paused);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Time.timeScale = isPaused ? 0f : 1f;
    }
}
