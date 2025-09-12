using System;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class PlayerEntity : NetworkEntity
{
    public Transform firePoint;
    private PlayerUI playerUI;   

    protected override void Awake()
    {
        base.Awake();
        AddSpell(SpellsManager.Instance.GetRandomSpell());
        GetComponent<NavMeshAgent>().speed = movementSpeedMultiplier.Value;
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
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        playerUI = FindAnyObjectByType<PlayerUI>(FindObjectsInactive.Include);
        playerUI.SetPlayer(this);
    }
}
