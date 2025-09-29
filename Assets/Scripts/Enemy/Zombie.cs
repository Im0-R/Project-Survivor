public class Zombie : EnemyMelee
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer) return;

        AddSpell(SpellsManager.Instance.GetSpell("Fireball"));
        ChangeState(new EnemyIdleState());
    }
}
