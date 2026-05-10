using Mirror;
using UnityEngine;

public class InstanceState : NetworkBehaviour
{
    public static InstanceState Instance { get; private set; }

    [SyncVar(hook = nameof(OnMapDataChanged))]
    public string mapId;

    [SyncVar(hook = nameof(OnSeedChanged))]
    public int seed;

    private bool clientMapGenerated;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        TryGenerateClientMap();
    }

    private void OnMapDataChanged(string oldValue, string newValue)
    {
        TryGenerateClientMap();
    }

    private void OnSeedChanged(int oldValue, int newValue)
    {
        TryGenerateClientMap();
    }

    private void TryGenerateClientMap()
    {
        if (isServer)
            return;

        if (clientMapGenerated)
            return;

        if (string.IsNullOrWhiteSpace(mapId))
            return;

        MapGenerator generator = FindFirstObjectByType<MapGenerator>();

        if (generator == null)
        {
            Debug.LogError("[InstanceState] MapGenerator not found on client");
            return;
        }

        clientMapGenerated = true;

        Debug.Log($"[InstanceState] Client generating map | mapId={mapId} | seed={seed}");

        generator.StartCoroutine(generator.Generate(mapId, seed));
    }

    [Server]
    public void SetMap(string newMapId, int newSeed)
    {
        if (string.IsNullOrWhiteSpace(newMapId))
        {
            Debug.LogError("[InstanceState] SetMap called with empty mapId");
            return;
        }

        seed = newSeed;
        mapId = newMapId;

        Debug.Log($"[InstanceState] Server map set | mapId={mapId} | seed={seed}");
    }
}