using Unity.Netcode;
using UnityEngine;

public class FireballSpell : Spell
{
    // Constructeur vide obligatoire pour Activator
    public FireballSpell() { }

    public override void ExecuteServer(NetworkEntity owner)
    {
        Transform target = null;

        var netOwner = owner; // le NetworkEntity réel (PlayerEntity ou EnemyEntity)

        // Trouve la bonne cible selon le camp
        if (netOwner is PlayerEntity)
            target = TargetHelper.FindClosestTarget(owner.transform.position, "Enemy", data.range);
        else if (netOwner is EnemyEntity)
            target = TargetHelper.FindClosestTarget(owner.transform.position, "Player", data.range);

        if (target == null) return;

        // Instancie le projectile
        var obj = GameObject.Instantiate(data.prefab, owner.transform.position, Quaternion.identity);
        var proj = obj.GetComponent<Projectile>();

        proj?.Initialize(netOwner, target, data.damage, data.speed);   // Initialise avec les bonnes
                                                                       // (ici j’ai mis data.damage & data.speed)

        // Spawn réseau
        obj.GetComponent<NetworkObject>()?.Spawn(true);
    }
}
