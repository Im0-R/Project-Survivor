using Mirror;
using UnityEngine;

public class PlayerPauseController : NetworkBehaviour
{
    public static PlayerPauseController Local;

    public override void OnStartLocalPlayer()
    {
        Local = this;
    }

    public override void OnStopLocalPlayer()
    {
        if (Local == this) Local = null;
    }

    public void RequestPause()
    {
        if (!isLocalPlayer) return;
        CmdRequestPause();
        Debug.Log("[Client] Pause requested");
    }

    public void RequestResume()
    {
        if (!isLocalPlayer) return;
        CmdRequestResume();
        Debug.Log("[Client] Resume requested");
    }

    [Command]
    private void CmdRequestPause()
    {
        if (ServerTimeManager.instance == null)
        {
            var found = FindFirstObjectByType<ServerTimeManager>();
            Debug.LogWarning($"[Server] ServerTimeManager.instance NULL. FoundInScene={(found != null)}");
            if (found != null) ServerTimeManager.instance = found;
            else return;
        }

        ServerTimeManager.instance.PauseGame();
    }

    [Command]
    private void CmdRequestResume()
    {
        if (ServerTimeManager.instance == null)
        {
            var found = FindFirstObjectByType<ServerTimeManager>();
            Debug.LogWarning($"[Server] ServerTimeManager.instance NULL. FoundInScene={(found != null)}");
            if (found != null) ServerTimeManager.instance = found;
            else return;
        }

        ServerTimeManager.instance.ResumeGame();
    }
    private void OnDisable()
    {
        Debug.LogWarning($"[PPC] OnDisable name={name} netId={netId} scene={gameObject.scene.name}\n{System.Environment.StackTrace}");
    }

    private void OnDestroy()
    {
        Debug.LogWarning($"[PPC] OnDestroy name={name} netId={netId} scene={gameObject.scene.name}\n{System.Environment.StackTrace}");
    }

}
