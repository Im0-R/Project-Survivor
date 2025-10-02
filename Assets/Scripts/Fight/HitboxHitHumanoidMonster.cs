using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class HitboxHitHumanoidMonster : MonoBehaviour
{

    public Collider hitbox;
    private void Start()
    {
        hitbox = GetComponent<Collider>();
        if (hitbox == null)
        {
            Debug.LogError("No collider found on HitboxHitHumanoidMonster");
        }
        hitbox.isTrigger = true;
        hitbox.enabled = false; // Désactiver le hitbox au départ
    }
    public void EnableHitbox()
    {
        hitbox.isTrigger = true;
    }

    public void DisableHitbox()
    {
        hitbox.isTrigger = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        //if the hitbox is enabled and the other collider is an enemy, deal damage then disable the hitbox
        if (hitbox.enabled && other.CompareTag("Player"))
        {
            Debug.Log("Enemy touché : " + other.name);
            hitbox.isTrigger = false;
        }
    }
}
