using Mirror;
using UnityEngine;

public class SlashSpell : Spell
{
    public SlashSpell() { }

    public override void ExecuteServer(NetworkEntity owner)
    {
        if (owner == null || data == null || data.prefab == null)
            return;

        GameObject obj = GameObject.Instantiate(
            data.prefab,
            owner.transform.position,
            owner.transform.rotation
        );

        SlashBehaviour slash = obj.GetComponent<SlashBehaviour>();
        if (slash != null)
        {
            slash.Initialize(owner, data.damage, data.duration, data.range);
        }

        NetworkServer.Spawn(obj);
    }
}