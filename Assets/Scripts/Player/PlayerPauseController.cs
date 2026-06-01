using Mirror;
using UnityEngine;

public class PlayerPauseController : NetworkBehaviour
{
    public static PlayerPauseController Local;

    private NetworkEntity networkEntity;

    private void Awake()
    {
        networkEntity = GetComponent<NetworkEntity>();
    }

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
        if (networkEntity == null)
            networkEntity = GetComponent<NetworkEntity>();

        if (networkEntity != null)
            networkEntity.DisableSpells();

        RpcPauseCooldownUI();

        Debug.Log($"[Server] Spells disabled for {name}");
    }

    [Command]
    private void CmdRequestResume()
    {
        if (networkEntity == null)
            networkEntity = GetComponent<NetworkEntity>();

        if (networkEntity != null)
            networkEntity.EnableSpells();

        RpcResumeCooldownUI();

        Debug.Log($"[Server] Spells enabled for {name}");
    }

    [TargetRpc]
    private void RpcPauseCooldownUI()
    {
        if (SpellsSlotsUI.Instance != null)
            SpellsSlotsUI.Instance.SetCooldownPaused(true);
    }

    [TargetRpc]
    private void RpcResumeCooldownUI()
    {
        if (SpellsSlotsUI.Instance != null)
            SpellsSlotsUI.Instance.SetCooldownPaused(false);
    }
}