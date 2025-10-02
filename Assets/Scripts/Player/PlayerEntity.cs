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
        //if (!IsOwner) return;
        base.Awake();
        OnLevelUp += UIManager.Instance.ShowSpellsRewardUI;
        Debug.Log("PlayerEntity Awake completed");
    }

    protected override void Update()
    {
        base.Update();

        if (!IsOwner) return;

        GetComponent<NavMeshAgent>().speed = movementSpeedMultiplier.Value;
    }

    public override void OnNetworkSpawn()
    {
        InitFromSO();

        if (!IsOwner) return;

        CameraFollow.Instance.SetTarget(transform);
    }
}
