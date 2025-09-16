using System;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class PlayerEntity : NetworkEntity
{
    public Transform firePoint;

    protected override void Awake()
    {
        base.Awake();
        AddSpell(SpellsManager.Instance.GetRandomSpell());

        if (!IsOwner) return;
    }

    protected override void Update()
    {
        base.Update();

        if (!IsOwner) return;

        if (Input.GetMouseButtonDown(0))
        {
            var fireball = GetSpell<FireballSpell>();
            fireball?.TryCast(this);
        }

        GetComponent<NavMeshAgent>().speed = movementSpeedMultiplier.Value;
    }

    public override void OnNetworkSpawn()
    {
        InitFromSO();

        if (!IsOwner) return;

        CameraFollow.Instance.SetTarget(transform);
    }
}
