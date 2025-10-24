using UnityEngine;
using UnityEngine.AI;

public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent agent;
    private NetworkEntity netEntity;
    void Awake()
    {
        animator = GetComponentInChildren<Animator>(); // si l'Animator est sur un enfant
        if (animator == null)
        {
            Debug.Log("Animator component not found.");
        }
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = true; // désactive la rotation automatique
        netEntity = GetComponent<NetworkEntity>();
    }

    void Update()
    {
        // Magnitude de la vitesse pour animer
        float speed = agent.velocity.magnitude;
        float health = netEntity.currentHealth;

        animator.SetFloat("Speed", speed);
        animator.SetFloat("Health", health);
    }
}
