using UnityEngine;
using Unity.Netcode;

public class NetworkEntity : NetworkBehaviour
{
    protected Entity entity;

    // Vie synchronisée automatiquement
    public NetworkVariable<float> health = new NetworkVariable<float>(100f);
    public NetworkVariable<float> maxHealth = new NetworkVariable<float>(100f);
    public float HealthPercent => health.Value / maxHealth.Value;

    protected virtual void Awake()
    {
        entity = new Entity(this);
    }

    protected virtual void Start()
    {
        // Exemple : abonner un UI ou effet sur la vie
        health.OnValueChanged += (oldValue, newValue) =>
        {
            Debug.Log($"{name} health: {oldValue} → {newValue}");
        };
    }

    protected virtual void Update()
    {
        // Entités côté serveur (ennemis, objets IA non possédés)
        if (IsServer && !IsOwner)
        {
            entity.Update();
            return; // évite double appel
        }

        // Entités possédées (joueur local)
        if (IsOwner)
        {
            entity.Update();
        }
    }





    [ServerRpc(RequireOwnership = false)]
    public void CastSpellServerRpc(string spellName)
    {
        Spell spell = entity.GetSpellByName(spellName);
        spell?.ExecuteServer(entity);
        Debug.Log($"{name} cast spell {spellName}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void ApplyDamageServerRpc(float amount)
    {
        health.Value -= amount;
        if (health.Value <= 0)
            Die();
    }

    protected virtual void Die()
    {
        Debug.Log($"{name} died!");
        if (TryGetComponent<NetworkObject>(out var netObj))
            netObj.Despawn(true);
        else
            Destroy(gameObject);
    }

    public Entity GetEntity() => entity;
}
