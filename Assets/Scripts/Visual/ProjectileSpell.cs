public class ProjectileSpell : Spell
{
    public ProjectileSpell() { }

    public override void ExecuteServer(NetworkEntity owner)
    {
        if (owner == null) return;
        if (data == null) return;

        SpellCasterServer caster = owner.GetComponent<SpellCasterServer>();
        if (caster == null) return;

        caster.Cast(data, owner);
    }
}