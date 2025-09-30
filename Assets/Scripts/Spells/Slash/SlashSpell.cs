using Unity.Netcode;
using UnityEngine;

public class SlashSpell : Spell
{
    // Viable constructor for Activator
    public SlashSpell() { }

    public override void ExecuteServer(NetworkEntity owner)
    {
        if (owner == null) return;

        // Instancier le slash au niveau du joueur
        var obj = GameObject.Instantiate(data.prefab, owner.transform.position, Quaternion.identity);

        // Récupérer le script SlashBehaviour et l'initialiser (on passe le NetworkEntity owner)
        var slash = obj.GetComponent<SlashBehaviour>();
        if (slash != null)
        {
            slash.Initialize(owner, data.damage, data.duration, data.range);
        }

        // Network spawn (même pattern que pour FrostballSpell)
        obj.GetComponent<NetworkObject>()?.Spawn(true);
    }
}
