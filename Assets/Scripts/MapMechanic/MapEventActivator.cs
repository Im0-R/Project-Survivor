using Mirror;
using UnityEngine;

public class MapEventActivator : NetworkBehaviour, IInteractable
{
    [SerializeField] private EnemySpawner enemySpawner;

    [SyncVar] private bool alreadyActivated;

    public void OnInteract()
    {
        if (!NetworkClient.active)
            return;

        CmdActivateEvent();
    }

    [Command(requiresAuthority = false)]
    private void CmdActivateEvent(NetworkConnectionToClient sender = null)
    {
        if (alreadyActivated)
            return;

        if (enemySpawner == null)
            enemySpawner = FindFirstObjectByType<EnemySpawner>();

        if (enemySpawner == null)
        {
            Debug.LogError("[MapEventActivator] No EnemySpawner found.");
            return;
        }

        int difficulty = 1;

        if (InstanceState.Instance != null)
            difficulty = InstanceState.Instance.difficulty;

        alreadyActivated = true;

        Debug.Log($"[MapEventActivator] Event activated | difficulty={difficulty}");

        enemySpawner.StartEvent(difficulty);
    }
}