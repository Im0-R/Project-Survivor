using UnityEngine;

[System.Serializable]
public abstract class Spell
{
    public Spell() { }
    public Spell(SpellData spellData)
    {
        this.data = spellData;
    }
    [System.Serializable]
    public class SpellData
    {
        public SpellTypeReference spellType;
        public Sprite UISprite;
        public string spellName;
        public GameObject prefab;
        public float damage;
        public float speed;
        public float range;
        public float duration;
        public int manaCost;
        public float cooldown = 2f;
        public float lastCastTime;
        public bool autoCast = true;
        public Transform firePoint;
        public NetworkEntity owner;
        public int maxLevel = 3;
        public int currentLevel = 1;
        public string description;

        public SpellData Clone()
        {
            return (SpellData)this.MemberwiseClone();
        }
    }
    protected SpellData data;

    public void Init(SpellData spellData)
    {
        this.data = spellData;
    }
    public virtual void OnAdd(NetworkEntity owner) { }
    public virtual void OnRemove(NetworkEntity owner) { }
    public virtual void UpdateSpell(NetworkEntity owner)
    {
        if (this.data.autoCast && Time.time >= this.data.lastCastTime + this.data.cooldown)
        {
            if (owner != null)
            {
                owner.CastSpellServerRpc(GetType().Name);
            }
            else
            {
                ExecuteServer(owner);
            }
            this.data.lastCastTime = Time.time;
        }
    }

    public void TryCast(NetworkEntity netEntity)
    {
        if (Time.time >= this.data.lastCastTime + this.data.cooldown)
        {
            if (netEntity != null)
                netEntity.CastSpellServerRpc(GetType().Name);
            else
                ExecuteServer(netEntity);

            this.data.lastCastTime = Time.time;
        }
    }
    public SpellData GetData() { return data; }
    public abstract void ExecuteServer(NetworkEntity owner);

    public void LevelUp()
    {
        if (data.currentLevel < data.maxLevel)
        {
            data.currentLevel++;
            data.damage *= 1.2f;
            data.cooldown = Mathf.Max(0.5f, data.cooldown / -0.8f);
        }
    }

    public void Upgrade()
    {
        data.currentLevel++;
    }

    public bool IsMaxLevel()
    {
        return data.currentLevel >= data.maxLevel;
    }
}
