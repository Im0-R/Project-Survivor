using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using Mirror;

public class Enemy : EnemyEntity
{
    [SerializeField] public float attackRange = 2f;
    [SerializeField] public float attackDamage = 10f;
    NavMeshAgent agent;

    public HitboxHitHumanoidMonster hitboxHit;
    public HumanoidAnimator humanoidAnimator;
    IEnemyState currentState;
    public override void OnStartServer()
    {
        //Since the enemy is controlled by the server, we only run this code on the server
        if (!isServer) return;

        InitStatsFromSO();
        agent = GetComponent<NavMeshAgent>();

        // Initialize State = Chase
        ChangeState(new EnemyChaseState());
        OnDeath += OnDeathEffects;
    }
    public void Tick(float dt)
    {
        if (!isServer) return;
        currentState?.Update(this);
        agent.speed = movementSpeedMultiplier;
    }

    public void ChangeState(IEnemyState newState)
    {
        currentState?.Exit(this);
        currentState = newState;
        currentState?.Enter(this);
    }

    public NavMeshAgent GetAgent() => agent;

    public Transform GetClosestPlayer()
    {
        var players = GameObject.FindGameObjectsWithTag("Player");
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

    [Command]
    public void CmdTakeDamage(int dmg)
    {
        if (!isServer) return;

        currentHealth -= dmg;
        if (currentHealth <= 0)
        {
            ChangeState(new EnemyDeadState());
        }
    }
    public void OnDeathEffects()
    {
        GiveExpToPlayers();
    }
    public void GiveExpToPlayers()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in players)
        {
            PlayerEntity playerEntity = p.GetComponent<PlayerEntity>();
            if (playerEntity != null)
            {
                playerEntity.GainExperience(experienceGiven);
            }
        }
    }
    public void CanDealMeleeDamage()
    {
        hitboxHit.EnableHitbox();
    }
    public void Attack()
    {
        hitboxHit.EnableHitbox();
    }
    public void DisactiveAttack()
    {
        //switch the enemy to Chase state after attacking
        hitboxHit.DisableHitbox();
        ChangeState(new EnemyChaseState());
    }

    public void StopMoving()
    {
        agent.isStopped = true;
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Here you can call a method on the player to deal damage
            PlayerEntity player = collision.gameObject.GetComponent<PlayerEntity>();
            if (player != null)
            {
                Debug.Log("Player found, dealing damage.");
            }
        }
    }
    private void OnDisable()
    {
        if (isServer) EnemyManager.Instance?.UnregisterEnemy(this);
    }
    public void ResetState()
    {
        ChangeState(new EnemyIdleState());
    }
    public void SleepState()
    {
        ChangeState(new EnemySleepState());
    }
}