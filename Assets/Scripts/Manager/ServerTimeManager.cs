using Mirror;
using UnityEngine;

public class ServerTimeManager : NetworkBehaviour
{

    public static ServerTimeManager instance;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ======================
    // COMMANDS (Client -> Server)
    // ======================

    [Command]
    public void CmdPauseGame()
    {
        RpcSetTimeScale(0f);
    }

    [Command]
    public void CmdResumeGame()
    {
        RpcSetTimeScale(1f);
    }

    // ======================
    // RPCs (Server -> Clients)
    // ======================

    [ClientRpc]
    void RpcSetTimeScale(float scale)
    {
        Time.timeScale = scale;
    }
}
