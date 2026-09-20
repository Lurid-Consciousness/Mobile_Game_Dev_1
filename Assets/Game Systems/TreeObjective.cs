using UnityEngine;

public class TreeObjective : MonoBehaviour
{
    public float maxHealth = 300f;
    public AcornProjectile acornTemplate;
    public float acornDropHeight = 4f;
    public float acornDropRadius = 2.5f;
    public float acornDropDelay = 4f;
    public int maxAcorns = 6;

    public float HealthPercent => currentHealth / maxHealth;
    public bool IsAlive => currentHealth > 0f;

    private float currentHealth;
    private float dropTimer;
    private Renderer treeRenderer;

    void Start()
    {
        currentHealth = maxHealth;
        dropTimer = 1f;
        treeRenderer = GetComponent<Renderer>();

        if (treeRenderer != null)
            treeRenderer.material.color = Color.green;
    }

    void Update()
    {
        if (!IsAlive || acornTemplate == null)
            return;

        dropTimer -= Time.deltaTime;

        if (dropTimer <= 0f)
        {
            dropTimer = acornDropDelay;

            AcornProjectile[] acorns = FindObjectsByType<AcornProjectile>();

            if (acorns.Length < maxAcorns)
                DropAcorn();
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        GameAudio.PlayHit();

        if (treeRenderer != null)
            treeRenderer.material.color = Color.Lerp(Color.red, Color.green, HealthPercent);
    }

    void DropAcorn()
    {
        Vector2 circle = Random.insideUnitCircle * acornDropRadius;
        Vector3 position = transform.position + new Vector3(circle.x, acornDropHeight, circle.y);
        AcornProjectile acorn = Instantiate(acornTemplate, position, Random.rotation);
        acorn.gameObject.SetActive(true);
        acorn.PreparePickup();
    }
}
