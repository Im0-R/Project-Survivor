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
}