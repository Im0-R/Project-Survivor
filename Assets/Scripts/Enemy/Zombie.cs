public class Zombie : EnemyMelee
{
    public override void OnStartServer()
    {
        if (!isServer) return;
        base.OnStartServer();
        //AddSpell("Fireball");
    }
}
