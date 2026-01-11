using Mirror;
using UnityEngine;

public class ServerTimeManager : NetworkBehaviour
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
        Debug.Log("Game Paused on Server and Clients");
    }

    [Server]
    public void ResumeGame()
    {
        RpcSetTimeScale(1f);
        Debug.Log("Game Resumed on Server and Clients");
    }

    [ClientRpc]
    void RpcSetTimeScale(float scale)
    {
        Time.timeScale = scale;
    }
}
