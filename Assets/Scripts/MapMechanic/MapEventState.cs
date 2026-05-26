using Mirror;
using UnityEngine;

public class MapEventState : NetworkBehaviour
{
    public static MapEventState Instance { get; private set; }

    [SyncVar] public bool eventRunning;
    [SyncVar] public float remainingTime;
    [SyncVar] public int difficulty = 1;

    private void Awake()
    {
        Instance = this;
    }

    [Server]
    public void StartEvent(float duration, int newDifficulty)
    {
        eventRunning = true;
        remainingTime = duration;
        difficulty = Mathf.Max(1, newDifficulty);
    }

    [Server]
    public void SetRemainingTime(float time)
    {
        remainingTime = Mathf.Max(0f, time);
    }

    [Server]
    public void EndEvent()
    {
        eventRunning = false;
        remainingTime = 0f;
    }
}