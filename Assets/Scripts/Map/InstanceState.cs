using Mirror;
using UnityEngine;

public class InstanceState : NetworkBehaviour
{
    public static InstanceState Instance { get; private set; }

    [SyncVar(hook = nameof(OnMapDataChanged))]
    public string mapId;

    [SyncVar(hook = nameof(OnSeedChanged))]
    public int seed;

    [SyncVar(hook = nameof(OnDifficultyChanged))]
    public int difficulty = 1;

    private bool clientMapGenerated;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        Debug.Log(
            $"[InstanceState] OnStartClient | netId={netId} | " +
            $"mapId={mapId} | seed={seed} | difficulty={difficulty} | isServer={isServer}"
        );

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

    private void OnDifficultyChanged(int oldValue, int newValue)
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
        {
            Debug.LogWarning("[InstanceState] mapId empty on client");
            return;
        }

        MapGenerator generator = FindFirstObjectByType<MapGenerator>();

        if (generator == null)
        {
            Debug.LogError("[InstanceState] MapGenerator NOT FOUND on client");
            return;
        }

        clientMapGenerated = true;

        Debug.Log(
            $"[InstanceState] Client generating map | mapId={mapId} | " +
            $"seed={seed} | difficulty={difficulty}"
        );

        generator.StartCoroutine(generator.Generate(mapId, seed, difficulty));
    }

    [Server]
    public void SetMap(string newMapId, int newSeed, int newDifficulty)
    {
        if (string.IsNullOrWhiteSpace(newMapId))
        {
            Debug.LogError("[InstanceState] SetMap called with empty mapId");
            return;
        }

        mapId = newMapId;
        seed = newSeed;
        difficulty = Mathf.Clamp(newDifficulty, 1, 10);

        Debug.Log(
            $"[InstanceState] Server map set | mapId={mapId} | " +
            $"seed={seed} | difficulty={difficulty}"
        );
    }
}