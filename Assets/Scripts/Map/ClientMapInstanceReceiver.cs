
using System.Collections;
using Mirror;
using UnityEngine;


public struct MapInstanceInfoMessage : NetworkMessage
{
    public string mapId;
    public int seed;
}
public class ClientMapInstanceReceiver : MonoBehaviour
{
    [SerializeField] private MapGenerator mapGenerator;

    private bool generated;

    private void OnEnable()
    {
#if !UNITY_SERVER
        NetworkClient.RegisterHandler<MapInstanceInfoMessage>(OnMapInstanceInfo, false);
#endif
    }

    private void OnDisable()
    {
#if !UNITY_SERVER
        if (NetworkClient.active)
            NetworkClient.UnregisterHandler<MapInstanceInfoMessage>();
#endif
    }

    private void OnMapInstanceInfo(MapInstanceInfoMessage msg)
    {
        if (generated)
            return;

        generated = true;

        if (mapGenerator == null)
            mapGenerator = FindFirstObjectByType<MapGenerator>();

        if (mapGenerator == null)
        {
            Debug.LogError("[ClientMapInstanceReceiver] MapGenerator not found");
            return;
        }

        Debug.Log($"[ClientMapInstanceReceiver] Received map info | mapId={msg.mapId} | seed={msg.seed}");

        StartCoroutine(mapGenerator.Generate(msg.mapId, msg.seed));
    }
}