using Mirror;
using UnityEngine;

public class FireballSpell : Spell
{
    //viable Constructor for Activator
    public FireballSpell() { }

    public override void ExecuteServer(NetworkEntity owner)
    {
        Transform target = null;

        var netOwner = owner; // (PlayerEntity or EnemyEntity)

        //Find the right target according to the side (Player or Enemy)
        if (netOwner is PlayerEntity)
            target = TargetHelper.FindClosestTarget(owner.transform.position, "Enemy", data.range);
        else if (netOwner is EnemyEntity)
            target = TargetHelper.FindClosestTarget(owner.transform.position, "Player", data.range);

        if (target == null) return;

        //Instantiate the projectile
        var obj = GameObject.Instantiate(data.prefab, owner.transform.position, Quaternion.identity);
        var proj = obj.GetComponent<Projectile>();

        proj?.Initialize(netOwner, target, data.damage, data.speed, data.currentLevel);   // Initialize the projectile with damage , speed and scale

        //Network spawn
        NetworkServer.Spawn(obj);
        Debug.Log($"{netOwner.entityName} cast FireballSpell towards {target.name}");
    }
}
