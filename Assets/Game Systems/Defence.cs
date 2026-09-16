using UnityEngine;

public class Defence : MonoBehaviour
{


    private float maxHealth = 300f;
    private float currentHealth;

    public float WallHealth => currentHealth / maxHealth;
    public bool IsBroken => currentHealth <= 0f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = maxHealth;

    }


    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }
}
