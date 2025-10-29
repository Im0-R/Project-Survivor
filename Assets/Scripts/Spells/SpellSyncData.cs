using Mirror;

public struct SpellSyncData
{
    public string spellName;
    public string spellTypeID;
    public string description;
    public int manaCost;
    public float cooldown;
    public float damage;
    public float range;
    public float speed;
    public int currentLevel;
    public int maxLevel;

    public SpellSyncData(
        string spellName,
        string spellTypeID,
        string description,
        int manaCost,
        float cooldown,
        float damage,
        float range,
        float speed,
        int currentLevel,
        int maxLevel)
    {
        this.spellName = spellName;
        this.spellTypeID = spellTypeID;
        this.description = description;
        this.manaCost = manaCost;
        this.cooldown = cooldown;
        this.damage = damage;
        this.range = range;
        this.speed = speed;
        this.currentLevel = currentLevel;
        this.maxLevel = maxLevel;
    }
}
