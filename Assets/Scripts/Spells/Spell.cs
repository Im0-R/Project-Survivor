using Mirror;
using UnityEngine;

[System.Serializable]
public abstract class Spell
{
    public Spell() { }

    public Spell(SpellData spellData)
    {
        data = spellData;
    }

    [System.Serializable]
    public class SpellData
    {
        [Header("Identity")]
        public SpellTypeReference spellType;
        public Sprite UISprite;
        public string spellName;
        public string description;

        [Header("Arcana Tags")]
        public SpellTag[] tags;

        [Header("Prefab")]
        public GameObject prefab;

        [Header("Cast Mode")]
        public SpellCastMode castMode = SpellCastMode.Projectile;

        [Header("Visual")]
        public SpellVisualId visualId = SpellVisualId.None;

        [Header("Base Stats")]
        public float damage;
        public float speed;
        public float range;
        public float duration;
        public int manaCost;
        public float cooldown = 2f;

        [Header("Runtime")]
        public float lastCastTime;
        public bool autoCast = true;
        public Transform firePoint;
        public NetworkEntity owner;

        [Header("Arcana Level")]
        public int maxLevel = 20;
        public int currentLevel = 1;

        [Header("Projectile")]
        public ProjectileMotionType motionType = ProjectileMotionType.Straight;
        public float projectileRadius = 0.35f;
        public float projectileLifetime = 2f;
        public int maxHits = 1;
        public float arcHeight = 3f;

        [Header("Projectile Modifiers")]
        public int projectileCount = 1;
        public float projectileSpreadAngle = 12f;
        public int pierceCount = 0;

        [Header("Runes")]
        public string[] runeIds;

        public SpellData Clone()
        {
            SpellData clone = (SpellData)MemberwiseClone();

            if (tags != null)
                clone.tags = (SpellTag[])tags.Clone();

            if (runeIds != null)
                clone.runeIds = (string[])runeIds.Clone();

            clone.lastCastTime = 0f;
            clone.owner = null;
            clone.firePoint = null;

            return clone;
        }
    }

    protected SpellData data;

    public void Init(SpellData spellData)
    {
        data = spellData;
    }

    public virtual void OnAdd(NetworkEntity owner) { }

    public virtual void OnRemove(NetworkEntity owner) { }

    public virtual void UpdateSpell(NetworkEntity owner)
    {
        if (data == null) return;

        float cooldown = GetFinalCooldown(owner);

        if (data.autoCast && Time.time >= data.lastCastTime + cooldown)
        {
            ExecuteServer(owner);
            data.lastCastTime = Time.time;
        }
    }

    public void TryCast(NetworkEntity netEntity)
    {
        if (netEntity == null) return;
        if (!netEntity.isServer) return;
        if (data == null) return;

        float cooldown = GetFinalCooldown(netEntity);

        if (Time.time >= data.lastCastTime + cooldown)
        {
            ExecuteServer(netEntity);
            data.lastCastTime = Time.time;
        }
    }

    protected float GetFinalCooldown(NetworkEntity owner)
    {
        if (data == null)
            return 1f;

        float cooldown = data.cooldown;

        if (owner is PlayerEntity player)
        {
            float globalReduction = player.GetCurrentStat(StatId.CooldownReduction);
            float spellReduction = player.GetSpellModifier(data.spellName, "CooldownReduction");

            float totalReduction = Mathf.Clamp(globalReduction + spellReduction, 0f, 80f);
            cooldown *= 1f - totalReduction / 100f;
        }

        return Mathf.Max(0.1f, cooldown);
    }

    public SpellData GetData()
    {
        return data;
    }

    public abstract void ExecuteServer(NetworkEntity owner);

    public virtual void ExecuteClient(NetworkEntity owner) { }

    public void LevelUp()
    {
        if (data == null) return;

        if (data.currentLevel < data.maxLevel)
        {
            data.currentLevel++;
            data.damage *= 1.12f;
            data.cooldown = Mathf.Max(0.35f, data.cooldown * 0.97f);
        }
    }

    public bool IsMaxLevel()
    {
        return data != null && data.currentLevel >= data.maxLevel;
    }
    protected bool HasTag(SpellTag tag)
    {
        if (data == null || data.tags == null)
            return false;

        for (int i = 0; i < data.tags.Length; i++)
        {
            if (data.tags[i] == tag)
                return true;
        }

        return false;
    }

    protected float GetFinalDamage(NetworkEntity owner)
    {
        if (data == null)
            return 0f;

        float damage = data.damage;

        if (owner is PlayerEntity player)
        {
            // Bonus global spell
            damage += player.GetCurrentStat(StatId.SpellDamage);

            // Bonus par élément
            if (HasTag(SpellTag.Fire))
                damage += player.GetCurrentStat(StatId.FireDamage);

            if (HasTag(SpellTag.Cold))
                damage += player.GetCurrentStat(StatId.ColdDamage);

            if (HasTag(SpellTag.Lightning))
                damage += player.GetCurrentStat(StatId.LightningDamage);

            if (HasTag(SpellTag.Chaos))
                damage += player.GetCurrentStat(StatId.ChaosDamage);

            // Bonus spécifique à ce spell
            damage += player.GetSpellModifier(data.spellName, "Damage");

            // Multiplicateur global
            float damageMult = player.GetCurrentStat(StatId.DamageMult);
            damage *= 1f + damageMult / 100f;
        }

        return Mathf.Max(0f, damage);
    }

    protected float GetFinalProjectileSpeed(NetworkEntity owner)
    {
        if (data == null)
            return 0f;

        float speed = data.speed;

        if (owner is PlayerEntity player)
        {
            if (HasTag(SpellTag.Projectile))
            {
                speed += player.GetCurrentStat(StatId.ProjectileSpeed);
                speed += player.GetSpellModifier(data.spellName, "ProjectileSpeed");
            }
        }

        return Mathf.Max(0f, speed);
    }

    protected int GetFinalProjectileCount(NetworkEntity owner)
    {
        if (data == null)
            return 1;

        int count = data.projectileCount;

        if (owner is PlayerEntity player && HasTag(SpellTag.Projectile))
            count += Mathf.RoundToInt(player.GetSpellModifier(data.spellName, "ProjectileCount"));

        return Mathf.Max(1, count);
    }

    protected int GetFinalPierce(NetworkEntity owner)
    {
        if (data == null)
            return 0;

        int pierce = data.pierceCount;

        if (owner is PlayerEntity player && HasTag(SpellTag.Projectile))
            pierce += Mathf.RoundToInt(player.GetSpellModifier(data.spellName, "Pierce"));

        return Mathf.Max(0, pierce);
    }
}