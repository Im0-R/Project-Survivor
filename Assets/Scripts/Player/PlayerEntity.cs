using UnityEngine;

public class PlayerEntity : NetworkEntity
{
    public Transform firePoint;

    protected override void Awake()
    {
        base.Awake();

        // Fireball manuel
        entity.AddSpell(new FireballSpell(SpellsManager.Instance.fireballPrefab, firePoint, 1.5f, 1.5f, autoCast: false));
    }

    protected override void Update()
    {
        base.Update();

        if (!IsOwner) return;

        if (Input.GetMouseButtonDown(0))
        {
            var fireball = entity.GetSpell<FireballSpell>();
            fireball?.TryCast(entity, this);
        }
    }
}
