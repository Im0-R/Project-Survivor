using UnityEngine;

public class EnemyEntity : NetworkEntity
{
    public Transform firePoint;

    protected override void Awake()
    {
        base.Awake();

        // Fireball auto-cast toutes les 2s
        entity.AddSpell(new FireballSpell(SpellsManager.Instance.fireballPrefab, firePoint, 15f ,1.5f, autoCast: true));
    }
}
