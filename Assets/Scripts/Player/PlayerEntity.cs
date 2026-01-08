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
        SceneManager.LoadSceneAsync("PlayerUI", LoadSceneMode.Additive);
    }


    //private IEnumerator LinkUIWhenReady()
    //{
    //    yield return null;

    //    PlayerUI ui = null;
    //    while (ui == null)
    //    {   
    //        ui = PlayerUI.Instance;
    //        yield return null;
    //    }

    //    CameraFollow cam = null;
    //    while (cam == null)
    //    {
    //        cam = CameraFollow.Instance;
    //        yield return null;
    //    }

    //    Debug.Log("[Client] Linking UI + camera");

    //    cam.SetTarget(transform);
    //    entityName = GameUILoader.Instance.playerName;
    //    ui.SetPlayer(this);

    //    Debug.Log("[Client] UI + Camera linked successfully.");
    //}

}
