using Mirror;
using UnityEngine;

public class ServerTimeManager : NetworkBehaviour
{
    public static ServerTimeManager instance;
    public static event System.Action<bool> OnPauseChanged;

    [SyncVar]
    public bool isPaused;

    public static bool IsPaused
    {
        get
        {
            return instance != null && instance.isPaused;
        }
    }

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    public override void OnStartServer()
    {
        instance = this;
        Debug.Log($"[ServerTimeManager] OnStartServer netId={netId} scene={gameObject.scene.name}");
    }

    public override void OnStopServer()
    {
        if (instance == this)
            instance = null;
    }

    [Server]
    public void PauseGame()
    {
        if (isPaused)
            return;

        isPaused = true;

        foreach (var agent in FindObjectsByType<UnityEngine.AI.NavMeshAgent>(
                     FindObjectsInactive.Exclude,
                     FindObjectsSortMode.None))
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        RpcSetPaused(true);

        Debug.Log("[ServerTimeManager] PauseGame");
    }

    [Server]
    public void ResumeGame()
    {
        if (!isPaused)
            return;

        isPaused = false;

        foreach (var agent in FindObjectsByType<UnityEngine.AI.NavMeshAgent>(
                     FindObjectsInactive.Exclude,
                     FindObjectsSortMode.None))
        {
            agent.isStopped = false;
        }

        RpcSetPaused(false);

        Debug.Log("[ServerTimeManager] ResumeGame");
    }

    [ClientRpc]
    private void RpcSetPaused(bool paused)
    {
        OnPauseChanged?.Invoke(paused);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        OnPauseChanged?.Invoke(isPaused);
    }
}