using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleOnDeathEffects : MonoBehaviour
{
    public bool shouldParticlesDetach = true;
    // detach particles of the parent so they don't get destroyed with the parent
    public void OnObjectDestroy()
    {
        if (!shouldParticlesDetach) return;
        var particles = GetComponents<ParticleSystem>();
        foreach (var particle in particles)
        {
            particle.transform.SetParent(null);
            Destroy(particle.gameObject, particle.main.duration + particle.main.startLifetime.constantMax);
        }
    }
}
