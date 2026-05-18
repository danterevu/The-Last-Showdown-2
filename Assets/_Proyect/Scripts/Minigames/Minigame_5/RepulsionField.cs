using UnityEngine;

public class RepulsionField : MonoBehaviour
{
    [Header("FX")]
    public ParticleSystem fx;

    [Header("Radius Visual")]
    [SerializeField] private bool showRadius = true;
    [SerializeField] private LineRenderer circleRenderer;
    [SerializeField] private int circleSegments = 64;
    [SerializeField] private float circleLineWidth = 0.05f;

    [Header("Force")]
    public float radius = 5f;
    public float force = 10f;
    public float duration = 0.5f;

    private ParticleSystem.Particle[] particles;

    private void Awake()
    {
        if (circleRenderer == null)
            circleRenderer = GetComponent<LineRenderer>();
    }

    void Start()
    {
        Activate();
    }

    public void Activate()
    {
        fx.Play();
        SetupCircle();
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

        if (circleRenderer != null)
            circleRenderer.enabled = false;
    }

    private void SetupCircle()
    {
        if (!showRadius || circleRenderer == null) return;

        int segments = Mathf.Max(8, circleSegments);
        circleRenderer.useWorldSpace = false;
        circleRenderer.loop = true;
        circleRenderer.positionCount = segments;
        circleRenderer.startWidth = circleLineWidth;
        circleRenderer.endWidth = circleLineWidth;
        circleRenderer.enabled = true;

        float step = (Mathf.PI * 2f) / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = step * i;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            circleRenderer.SetPosition(i, new Vector3(x, y, 0f));
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            SetupCircle();
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

        // Destruir proyectiles dentro del radio de repulsión
        DestroyProjectilesInRadius(center);
    }

    private void DestroyProjectilesInRadius(Vector3 center)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);
        
        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;

            Projectile projectile = hit.GetComponent<Projectile>();
            if (projectile != null)
            {
                Destroy(projectile.gameObject);
                continue;
            }

            SlowGrandeProjectile slowGrande = hit.GetComponent<SlowGrandeProjectile>();
            if (slowGrande != null)
            {
                Destroy(slowGrande.gameObject);
                continue;
            }

            HomingMissile homingMissile = hit.GetComponent<HomingMissile>();
            if (homingMissile != null)
            {
                Destroy(homingMissile.gameObject);
            }
        }
    }
}
