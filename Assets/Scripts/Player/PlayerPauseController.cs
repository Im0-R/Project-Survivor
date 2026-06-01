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
        if (Local == this)
            Local = null;
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
        ServerTimeManager manager = ServerTimeManager.instance;

        if (manager == null)
            manager = FindFirstObjectByType<ServerTimeManager>();

        if (manager == null)
        {
            Debug.LogWarning("[PlayerPauseController] ServerTimeManager not found.");
            return;
        }

        ServerTimeManager.instance = manager;
        manager.PauseGame();

        TargetSetCooldownPaused(connectionToClient, true);

        Debug.Log("[Server] Global pause requested");
    }

    [Command]
    private void CmdRequestResume()
    {
        ServerTimeManager manager = ServerTimeManager.instance;

        if (manager == null)
            manager = FindFirstObjectByType<ServerTimeManager>();

        if (manager == null)
        {
            Debug.LogWarning("[PlayerPauseController] ServerTimeManager not found.");
            return;
        }

        ServerTimeManager.instance = manager;
        manager.ResumeGame();

        TargetSetCooldownPaused(connectionToClient, false);

        Debug.Log("[Server] Global resume requested");
    }

    [TargetRpc]
    private void TargetSetCooldownPaused(NetworkConnectionToClient target, bool paused)
    {
        if (SpellsSlotsUI.Instance != null)
            SpellsSlotsUI.Instance.SetCooldownPaused(paused);
    }
}