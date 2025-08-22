using UnityEngine;

public abstract class Spell
{
    public class SpellData
    {
        public float cooldown = 2f;
        public float lastCastTime;
        public bool autoCast = true;
        public Transform firePoint;
        public float range = 10f;
        public GameObject prefab;
        public SpellData(float cooldown, bool autoCast, Transform firePoint ,float range, GameObject prefab)
        {
            this.cooldown = cooldown;
            this.autoCast = autoCast;
            this.firePoint = firePoint;
            this.range = range;
            this.prefab = prefab;
        }
    }
    protected SpellData data;
    public Spell(float cooldown, bool autoCast, Transform firePoint, float range, GameObject prefab)
    {
        data = new SpellData(cooldown, autoCast, firePoint, range, prefab);
    }

    public virtual void OnAdd(Entity owner) { }
    public virtual void OnRemove(Entity owner) { }

    public virtual void UpdateSpell(Entity owner)
    {
        if (this.data.autoCast && Time.time >= this.data.lastCastTime + this.data.cooldown)
        {
            if (owner.NetEntity != null)
            {
                owner.NetEntity.CastSpellServerRpc(GetType().Name);
            }
            else
            {
                ExecuteServer(owner);
            }
            this.data.lastCastTime = Time.time;
        }
    }

    public void TryCast(Entity owner, NetworkEntity netEntity)
    {
        if (Time.time >= this.data.lastCastTime + this.data.cooldown)
        {
            if (netEntity != null)
                netEntity.CastSpellServerRpc(GetType().Name);
            else
                ExecuteServer(owner);

            this.data.lastCastTime = Time.time;
        }
    }

    public abstract void ExecuteServer(Entity owner);
}
