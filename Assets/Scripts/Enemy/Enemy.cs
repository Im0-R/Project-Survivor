using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;

public class Enemy : EnemyEntity
{
    [SerializeField] int maxHealth = 100;
    int currentHealth;

    [SerializeField] public float attackRange = 2f;
    [SerializeField] public float attackDamage = 10f;

    [SerializeField] public float movementSpeed = 1f;
    NavMeshAgent agent;
    Transform targetPlayer;

    IEnemyState currentState;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        agent = GetComponent<NavMeshAgent>();
        agent.speed = movementSpeed;

        currentHealth = maxHealth;

        // Premier état = Chase
        ChangeState(new EnemyChaseState());
    }

    protected override void Update()
    {
        // 1) D’abord faire tourner les spells via la base
        base.Update();

        // 2) Puis l’IA uniquement serveur
        if (!IsServer) return;
        currentState?.Update(this);
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

        currentHealth -= dmg;
        if (currentHealth <= 0)
        {
            ChangeState(new EnemyDeadState());
        }
    }
}
