using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float maxHealth = 50f;
    public float damage = 10f;

    private float currentHealth;

    public float HealthPercent => currentHealth / maxHealth;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        GameAudio.PlayHit();

        Renderer enemyRenderer = GetComponent<Renderer>();

        if (enemyRenderer != null)
            enemyRenderer.material.color = Color.Lerp(Color.black, Color.red, HealthPercent);

        BearEnemy bearEnemy = GetComponent<BearEnemy>();

        if (bearEnemy != null)
            bearEnemy.Aggro();

        if (currentHealth <= 0f)
            Destroy(gameObject);
    }
}
