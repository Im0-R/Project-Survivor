using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class PlayerEntity : NetworkEntity
{
    public Transform firePoint;
    private NavMeshAgent agent;

    protected override void Awake()
    {
        base.Awake();
        agent = GetComponent<NavMeshAgent>();
    }

    protected override void Update()
    {
        if (!isServer) return;

        base.Update();

        if (agent != null)
            agent.speed = StatComp.Get(StatId.MoveSpeedMult);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (StatComp != null)
            StatComp.OnLevelUpServer += HandleLevelUpServer;
    }

    public override void OnStopServer()
    {
        if (StatComp != null)
            StatComp.OnLevelUpServer -= HandleLevelUpServer;

        base.OnStopServer();
    }

    [Server]
    private void HandleLevelUpServer(int newLevel)
    {
        Debug.Log($"[PlayerEntity] Level up detected server side: {newLevel}");

        if (UIManager.Instance != null)
            UIManager.Instance.ShowSpellsRewardUI();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        StartCoroutine(LoadAndBindPlayerUI());
    }

    private IEnumerator LoadAndBindPlayerUI()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync("PlayerUI", LoadSceneMode.Additive);
        while (!op.isDone)
            yield return null;

        while (PlayerUI.Instance == null)
            yield return null;

        PlayerUI.Instance.Bind(this);
    }
}