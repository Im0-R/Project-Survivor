#if UNITY_SERVER
using UnityEngine;
using Mirror;

public static class EarlyServerBootStrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        Debug.Log("[EarlyServerBootstrap] Running BEFORE any scene loads...");

        var nm = Object.FindObjectOfType<NetworkManager>();

        if (nm != null && !NetworkServer.active)
        {
            Debug.Log("[EarlyServerBootstrap] Forcing Mirror StartServer()");
            nm.StartServer();
        }
        else if (nm == null)
        {
            Debug.LogError("[EarlyServerBootstrap] No NetworkManager found!");
        }
    }
}
#endif
