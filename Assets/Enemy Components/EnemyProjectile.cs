using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public float lifetime = 5f;

    private Rigidbody rb;
    private float damage;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Launch(Vector3 direction, float speed, float damageAmount)
    {
        damage = damageAmount;
        rb.linearVelocity = direction * speed;
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        PlayerController player = collision.gameObject.GetComponent<PlayerController>();

        if (player != null)
            player.TakeDamage(damage);

        Defence barrier = collision.collider.GetComponentInParent<Defence>();
        if (barrier != null)
            barrier.TakeDamage(damage);

        Destroy(gameObject);
    }
}
