using UnityEngine;
using UnityEngine.AI;
using Mirror;

public class Enemy : NetworkEntity
{
    [Header("Combat")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackDamage = 10f;

    private NavMeshAgent agent;

    public HitboxHitHumanoidMonster hitboxHit;
    public HumanoidAnimator humanoidAnimator;
    public IEnemyState currentState;
    public Transform firePoint;

    public float AttackRange => attackRange;
    public float AttackDamage => attackDamage;

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
            Debug.LogError($"[Enemy] Tick called but NavMeshAgent is null on {name}");
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning($"[Enemy] {name} is not on NavMesh | pos={transform.position}");
            return;
        }

        currentState?.Update(this);
        agent.speed = StatComp.Get(StatId.MoveSpeedMult);
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

    public void ResetState()
    {
        ChangeState(new EnemyIdleState());
    }

    public void SleepState()
    {
        ChangeState(new EnemySleepState());
    }

    // ======================
    // NAV
    // ======================
    public NavMeshAgent GetAgent() => agent;

    public void StopMoving()
    {
        if (agent != null)
            agent.isStopped = true;
    }

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

        foreach (GameObject p in players)
        {
            PlayerEntity playerEntity = p.GetComponent<PlayerEntity>();
            if (playerEntity != null)
                playerEntity.GainExperience(StatComp.Get(StatId.ExperienceGiven));
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
    // ATTACK
    // ======================
    public void CanDealMeleeDamage()
    {
        hitboxHit?.EnableHitbox();
    }

    public void Attack()
    {
        hitboxHit?.EnableHitbox();
    }

    public void DisactiveAttack()
    {
        hitboxHit?.DisableHitbox();
        ChangeState(new EnemyChaseState());
    }

    // ======================
    // COLLISION
    // ======================
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Debug.Log($"[Enemy] Collision with Player {collision.gameObject.name}");
        }
    }

    // ======================
    // CLIENT VISUAL STATE
    // ======================
    [ClientRpc]
    public void RpcSetActive(bool active)
    {
        gameObject.SetActive(active);
    }

    // ======================
    // POOL RESET
    // ======================
    [Server]
    public virtual void ResetForSpawn()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        StatComp.InitFromSO_Server();

        hitboxHit?.DisableHitbox();

        if (agent != null)
        {
            agent.isStopped = false;
            agent.ResetPath();
        }

        ResetState();
    }

    [Server]
    public virtual void ResetForDespawn()
    {
        hitboxHit?.DisableHitbox();

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        currentState = null;
    }

    // ======================
    // CLEANUP
    // ======================
    private void OnDisable()
    {
        if (!isServer) return;
        EnemyManager.Instance?.UnregisterEnemy(this);
    }
}