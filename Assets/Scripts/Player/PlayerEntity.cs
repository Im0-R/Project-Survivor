using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class PlayerEntity : NetworkEntity
{
    public Transform firePoint;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Update()
    {
        if (!isServer) return;
        base.Update();
        GetComponent<NavMeshAgent>().speed = movementSpeedMultiplier;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        InitFromSO();
        OnLevelUp += UIManager.Instance.ShowSpellsRewardUI;
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        // 1) Set camera (only on the local client)
        if (CameraFollow.Instance != null)
            CameraFollow.Instance.SetTarget(transform);
        else
            Debug.LogWarning("[PlayerEntity] CameraFollow.Instance is null in OnStartLocalPlayer.");

        // 2) Try to link UI right away, or we wait a bit
        if (PlayerUI.Instance != null)
        {
            PlayerUI.Instance.SetPlayer(this);
            Debug.Log("[PlayerEntity] PlayerUI linked immediately.");
        }
        else
        {
            Debug.Log("[PlayerEntity] PlayerUI not ready yet, starting waiter coroutine.");
            StartCoroutine(WaitForUIAndSet());
        }
    }

    private IEnumerator WaitForUIAndSet()
    {
        float timeout = 5f;
        float t = 0f;

        while (PlayerUI.Instance == null && t < timeout)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (PlayerUI.Instance != null)
        {
            PlayerUI.Instance.SetPlayer(this);
            Debug.Log("[PlayerEntity] PlayerUI linked after wait.");
        }
        else
        {
            Debug.LogWarning("[PlayerEntity] PlayerUI still null after waiting. Vérifie que PlayerUI est présent dans la scène et que son Awake a été appelé.");
        }
    }
}
