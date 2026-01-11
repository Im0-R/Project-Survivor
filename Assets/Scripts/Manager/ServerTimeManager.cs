using Mirror;
using UnityEngine;

public class ServerTimeManager : NetworkEntity
{
    public static ServerTimeManager instance;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    [Server]
    public void PauseGame()
    {
        RpcSetTimeScale(0f);
    }

    [Server]
    public void ResumeGame()
    {
        RpcSetTimeScale(1f);
    }

    [ClientRpc]
    void RpcSetTimeScale(float scale)
    {
        Time.timeScale = scale;
    }
}
