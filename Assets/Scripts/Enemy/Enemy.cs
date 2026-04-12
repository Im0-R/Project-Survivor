using UnityEngine;
using UnityEngine.AI;
using Mirror;

public class Enemy : NetworkEntity
{
    [Header("Combat")]
    [SerializeField] public float attackRange = 2f;
    [SerializeField] public float attackDamage = 10f;

    NavMeshAgent agent;

    public HitboxHitHumanoidMonster hitboxHit;
    public HumanoidAnimator humanoidAnimator;
    public IEnemyState currentState;

    public Transform firePoint;

    // ======================
    // SERVER INIT
    // ======================
    public override void OnStartServer()
    {
        base.OnStartServer();

        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError($"[Enemy] NavMeshAgent missing on {name}");
            return;
        }

        OnDeath -= OnDeathEffects;
        OnDeath += OnDeathEffects;
    }
    public override void OnStopServer()
    {
        if (!isServer)
        {
            Debug.LogWarning($"[Enemy] ❌  OnStopServer called on CLIENT {name}");
            return;
        }


        OnDeath -= OnDeathEffects;

        base.OnStopServer();
    }

    // ======================
    // AI TICK
    // ======================
    public void Tick(float dt)
    {
        if (!isServer) return;

        if (agent == null)
        {
            Debug.LogError($"[Enemy] ❌ Tick called but agent NULL on {name}");
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning($"[Enemy] ⚠️ {name} NOT on NavMesh | pos={transform.position}");
            return;
        }

        currentState?.Update(this);
        agent.speed = StatComp.stats[StatId.MoveSpeedMult];
    }

    // ======================
    // STATE MACHINE
    // ======================
    public void ChangeState(IEnemyState newState)
    {

        currentState?.Exit(this);
        currentState = newState;
        currentState?.Enter(this);
    }


    // ======================
    // NAV
    // ======================
    public NavMeshAgent GetAgent() => agent;

    // ======================
    // PLAYER DETECTION
    // ======================
    public Transform GetClosestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        float closest = Mathf.Infinity;
        Transform best = null;

        foreach (GameObject p in players)
        {
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < closest)
            {
                closest = d;
                best = p.transform;
            }
        }

        //if (best != null)
        //    Debug.Log($"[Enemy] 🎯 Closest player = {best.name} dist={closest:F2}");

        return best;
    }

    // ======================
    // DEATH
    // ======================
    public void OnDeathEffects()
    {
        Debug.Log($"[Enemy] OnDeathEffects {name}");
        GiveExpToPlayers();
    }

    public void GiveExpToPlayers()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        //Debug.Log($"[Enemy] XP given to {players.Length} players");

        foreach (GameObject p in players)
        {
            PlayerEntity playerEntity = p.GetComponent<PlayerEntity>();
            if (playerEntity != null)
                playerEntity.GainExperience(StatComp.stats[StatId.ExperienceGiven]);
        }
    }

    // ======================
    // ATTACK
    // ======================
    public void CanDealMeleeDamage()
    {
        //Debug.Log($"[Enemy] ⚔️ CanDealMeleeDamage {name}");
        hitboxHit.EnableHitbox();
    }

    public void Attack()
    {
        //Debug.Log($"[Enemy] ⚔️ Attack {name}");
        hitboxHit.EnableHitbox();
    }

    public void DisactiveAttack()
    {
        //Debug.Log($"[Enemy] 🛑 End Attack {name}");
        hitboxHit.DisableHitbox();
        ChangeState(new EnemyChaseState());
    }

    public void StopMoving()
    {
        //Debug.Log($"[Enemy] 🛑 StopMoving {name}");
        agent.isStopped = true;
    }

    // ======================
    // COLLISION
    // ======================
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            //Debug.Log($"[Enemy] 💥 Collision with Player {collision.gameObject.name}");
        }
    }
    protected override void Die()
    {
        if (!isServer) return;
#if UNITY_SERVER

        int seed = (int)(Time.time * 1000f);
        Debug.Log($"[Enemy] GenerateDrop {name}");

        LootManager.Instance.GenerateDrop(1, seed, transform.position);
#endif
        EnemyPool.Instance?.DespawnEnemy(gameObject);

    }
    // ======================
    // CLEANUP
    // ======================
    private void OnDisable()
    {
        if (isServer)
            //Debug.Log($"[Enemy] ❌ Disabled {name}");

            if (isServer)
                EnemyManager.Instance?.UnregisterEnemy(this);
    }

    // ======================
    // UTIL
    // ======================
    public void ResetState()
    {
        ChangeState(new EnemyIdleState());
    }
    public void SleepState()
    {
        ChangeState(new EnemySleepState());
    }
    [ClientRpc]
    public void RpcSetActive(bool active)
    {
        gameObject.SetActive(active);
    }
    [Server]
public void ResetForSpawn()
{
    if (agent == null)
        agent = GetComponent<NavMeshAgent>();

    InitStatsFromSO();

    if (hitboxHit != null)
        hitboxHit.DisableHitbox();

    if (agent != null)
    {
        agent.isStopped = false;
        agent.ResetPath();
    }

    ResetState();
}

[Server]
public void ResetForDespawn()
{
    if (hitboxHit != null)
        hitboxHit.DisableHitbox();

    if (agent != null)
    {
        agent.isStopped = true;
        agent.ResetPath();
    }

    currentState = null;
}
}
