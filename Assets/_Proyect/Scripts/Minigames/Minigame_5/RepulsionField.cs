using UnityEngine;

public class RepulsionField : MonoBehaviour
{
    [Header("FX")]
    public ParticleSystem fx;

    [Header("Editor")]
    [SerializeField] private bool showRadiusGizmo = true;
    [SerializeField] private Color radiusGizmoColor = new Color(0.2f, 0.9f, 1f, 0.9f);

    [Header("Force")]
    public float radius = 5f;
    public float force = 10f;
    public float duration = 0.5f;

    private ParticleSystem.Particle[] particles;

    void Start()
    {
        Activate();
    }

    public void Activate()
    {
        fx.Play();
        StartCoroutine(RepelRoutine());
    }

    System.Collections.IEnumerator RepelRoutine()
    {
        float time = 0f;

        while (time < duration)
        {
            ApplyRepulsion();
            time += Time.deltaTime;
            yield return null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showRadiusGizmo) return;
        Gizmos.color = radiusGizmoColor;
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    void ApplyRepulsion()
    {
        if (particles == null || particles.Length < fx.main.maxParticles)
            particles = new ParticleSystem.Particle[fx.main.maxParticles];

        int count = fx.GetParticles(particles);

        Vector3 center = transform.position;

        for (int i = 0; i < count; i++)
        {
            Vector3 dir = particles[i].position - center;
            float dist = dir.magnitude;

            if (dist < radius)
            {
                float falloff = 1f - (dist / radius);

                particles[i].velocity += dir.normalized * force * falloff;
            }

           
            if (dist > radius)
            {
                particles[i].remainingLifetime = 0;
            }
        }

        fx.SetParticles(particles, count);
    }
}
