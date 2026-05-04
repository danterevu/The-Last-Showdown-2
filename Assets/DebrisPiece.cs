using UnityEngine;


public class DebrisPiece : MonoBehaviour
{
    [SerializeField] private float lifetime = 2f;

    private SpriteRenderer sr;
    private float timer;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        timer = lifetime;
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        // fade out en el ultimo segundo
        float alpha = Mathf.Clamp01(timer);
        if (sr != null)
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);

        if (timer <= 0f)
            Destroy(gameObject);
    }

    // llamado desde Explodable para sincronizar el lifetime
    public void SetLifetime(float value)
    {
        lifetime = value;
        timer = value;
    }
}