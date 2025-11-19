using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

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

    // ======================
    // SERVER
    // ======================
    public override void OnStartServer()
    {
        base.OnStartServer();
        InitStatsFromSO();

        // OnLevelUp += UIManager.Instance.ShowSpellsRewardUI;
    }

    // ======================
    // CLIENT
    // ======================
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        Debug.Log("[Client] Local player spawned ? Loading PlayerUI scene...");

        // 1) Charger PlayerUI (client-only)
        SceneManager.LoadSceneAsync("PlayerUI", LoadSceneMode.Additive)
            .completed += op => { StartCoroutine(LinkUIWhenReady()); };
    }

    private IEnumerator LinkUIWhenReady()
    {
        // 2) Attendre CameraFollow et PlayerUI
        yield return new WaitUntil(() => CameraFollow.Instance != null
                                     && PlayerUI.Instance != null);

        Debug.Log("[Client] PlayerUI & CameraFollow ready ? linking...");

        // 3) Set caméra
        CameraFollow.Instance.SetTarget(transform);

        // 4) Link PlayerUI
        PlayerUI.Instance.SetPlayer(this);

        Debug.Log("[Client] UI + Camera linked successfully.");
    }
}
