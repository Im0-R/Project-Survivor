using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;

public class Enemy : EnemyEntity
{
    [SerializeField] public float attackRange = 2f;
    [SerializeField] public float attackDamage = 10f;
    NavMeshAgent agent;

    public HitboxHitHumanoidMonster hitboxHit;
    public HumanoidAnimator humanoidAnimator;
    IEnemyState currentState;
    public override void OnNetworkSpawn()
    {
        //Since the enemy is controlled by the server, we only run this code on the server
        if (!IsServer) return;

        InitFromSO();
        agent = GetComponent<NavMeshAgent>();

        // Premier état = Chase
        ChangeState(new EnemyChaseState());
        OnDeath += OnDeathEffects;
    }

    protected override void Update()
    {
        // 1) D’abord faire tourner les spells via la base
        base.Update();

        // 2) Puis l’IA uniquement serveur
        if (!IsServer) return;

        currentState?.Update(this);
        agent.speed = movementSpeedMultiplier.Value;
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

    [ServerRpc]
    public void TakeDamageServerRpc(int dmg)
    {
        if (!IsServer) return;

        currentHealth.Value -= dmg;
        if (currentHealth.Value <= 0)
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
                playerEntity.GainExperience(experienceGiven.Value);
            }
        }
    }
    public void CanDealMeleeDamage()
    {
        hitboxHit.EnableHitbox();
    }
    public void Attack()
    {
        GetComponent<HitboxHitHumanoidMonster>().EnableHitbox();
    }
    public void DisactiveAttack()
    {
        GetComponent<HitboxHitHumanoidMonster>().DisableHitbox();
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
}