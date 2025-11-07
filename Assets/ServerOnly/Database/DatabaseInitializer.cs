#if UNITY_SERVER
using Mirror;
using UnityEngine;

public class DatabaseInitializer : MonoBehaviour
{
    void Start()
    {
        if (NetworkServer.active)
        {
            if (DatabaseManager.isInitialized()) return;
            DatabaseManager.Initialize();
            Debug.Log("[Server] Database initialized");
        }
    }

    void OnApplicationQuit()
    {
        if (NetworkServer.active)
            DatabaseManager.Close();
    }
}
#endif