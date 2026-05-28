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

    [Header("Loot")]
    [SerializeField] private LootProfileSO lootProfile;
    [SerializeField] private int monsterLevel = 1;

    [Header("Scaling")]
    [SerializeField] private DifficultyScalingSO difficultyScaling;
    [SerializeField] private int difficultyPoints = 0;

    public float AttackRange => attackRange;
    public float AttackDamage => attackDamage;

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

    public NavMeshAgent GetAgent() => agent;

    public void StopMoving()
    {
        if (agent != null)
            agent.isStopped = true;
    }

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
        int seed = unchecked((int)(Time.time * 1000f) + GetInstanceID());

        float lootQuantityMultiplier = GetDifficultyLootQuantityMultiplier();
        float currencyQuantityMultiplier = GetDifficultyCurrencyQuantityMultiplier();
        float goldQuantityMultiplier = GetDifficultyGoldQuantityMultiplier();

        if (LootManager.Instance != null)
        {
            Debug.Log($"[Enemy] GenerateDrops {name} difficulty={difficultyPoints}");
            LootManager.Instance.GenerateDrops(
                lootProfile,
                monsterLevel,
                seed,
                transform.position,
                lootQuantityMultiplier,
                currencyQuantityMultiplier,
                goldQuantityMultiplier
            );
        }
#endif

        EnemyPool.Instance?.DespawnEnemy(gameObject);
    }

    [Server]
    private void ApplyDifficultyScaling()
    {
        if (difficultyScaling == null || StatComp == null)
            return;

        float healthMult = 1f + difficultyPoints * difficultyScaling.healthPercentPerPoint / 100f;
        float damageMult = 1f + difficultyPoints * difficultyScaling.damagePercentPerPoint / 100f;
        float speedMult = 1f + difficultyPoints * difficultyScaling.moveSpeedPercentPerPoint / 100f;
        float expMult = 1f + difficultyPoints * difficultyScaling.experiencePercentPerPoint / 100f;

        ScaleStat(StatId.MaxHealth, healthMult);
        ScaleStat(StatId.CurrentHealth, healthMult);
        ScaleStat(StatId.SpellDamage, damageMult);
        ScaleStat(StatId.MoveSpeedMult, speedMult);
        ScaleStat(StatId.ExperienceGiven, expMult);

        Debug.Log($"[Enemy] Difficulty applied: points={difficultyPoints}");
    }

    [Server]
    private void ScaleStat(StatId statId, float multiplier)
    {
        float current = StatComp.GetBaseStatServer(statId);
        StatComp.SetBaseStatServer(statId, current * multiplier);
    }
    private float GetDifficultyLootQuantityMultiplier()
    {
        if (difficultyScaling == null)
            return 1f;

        return 1f + difficultyPoints * difficultyScaling.lootQuantityPercentPerPoint / 100f;
    }

    private float GetDifficultyCurrencyQuantityMultiplier()
    {
        if (difficultyScaling == null)
            return 1f;

        return 1f + difficultyPoints * difficultyScaling.currencyQuantityPercentPerPoint / 100f;
    }

    private float GetDifficultyGoldQuantityMultiplier()
    {
        if (difficultyScaling == null)
            return 1f;

        return 1f + difficultyPoints * difficultyScaling.goldQuantityPercentPerPoint / 100f;
    }

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

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
        }
    }

    [ClientRpc]
    public void RpcSetActive(bool active)
    {
        gameObject.SetActive(active);
    }

    [Server]
    public virtual void ResetForSpawn()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        StatComp.InitFromSO_Server();
        ApplyDifficultyScaling();

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
    [Server]
    public void SetDifficultyPoints(int points)
    {
        difficultyPoints = Mathf.Max(0, points);
    }
    private void OnDisable()
    {
        if (!isServer) return;
        EnemyManager.Instance?.UnregisterEnemy(this);
    }
}