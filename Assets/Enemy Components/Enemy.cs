using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float maxHealth = 50f;
    public float damage = 10f;

    private float currentHealth;
    private WoodlandMotion motion;

    public float HealthPercent => currentHealth / maxHealth;

    void Awake()
    {
        currentHealth = maxHealth;
        motion = GetComponentInChildren<WoodlandMotion>();
    }

    public void ShowAttack()
    {
        if (motion != null)
            motion.Attack();
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
