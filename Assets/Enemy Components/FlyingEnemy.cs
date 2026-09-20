using UnityEngine;

public class FlyingEnemy : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float height = 6f;
    public float orbitRadius = 6f;
    public float orbitSpeed = 0.5f;
    public float projectileSpeed = 8f;
    public float attackDelay = 2f;

    private Enemy enemy;
    private PlayerController player;
    private float attackTimer;
    private float orbitOffset;

    void Start()
    {
        enemy = GetComponent<Enemy>();
        player = FindAnyObjectByType<PlayerController>();
        orbitOffset = Random.Range(0f, Mathf.PI * 2f);

        Renderer enemyRenderer = GetComponent<Renderer>();

        if (enemyRenderer != null)
            enemyRenderer.material.color = Color.magenta;
    }

    void Update()
    {
        if (player == null || !player.IsAlive)
            return;

        float angle = Time.time * orbitSpeed + orbitOffset;
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * orbitRadius;
        Vector3 targetPosition = player.transform.position + offset + Vector3.up * height;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        Vector3 lookPosition = player.transform.position;
        lookPosition.y = transform.position.y;
        transform.LookAt(lookPosition);

        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            Shoot();
            attackTimer = attackDelay;
        }
    }

    void Shoot()
    {
        enemy.ShowAttack();
        int choice = Random.Range(0, 3);
        PrimitiveType shape = choice == 0 ? PrimitiveType.Sphere : choice == 1 ? PrimitiveType.Cube : PrimitiveType.Capsule;
        GameObject projectileObject = GameObject.CreatePrimitive(shape);
        projectileObject.name = choice == 0 ? "Enemy Acorn" : choice == 1 ? "Enemy Garbage" : "Enemy Spike";
        projectileObject.transform.position = transform.position;
        projectileObject.transform.localScale = Vector3.one * 0.3f;

        Rigidbody projectileRb = projectileObject.AddComponent<Rigidbody>();
        projectileRb.useGravity = false;
        projectileRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        EnemyProjectile projectile = projectileObject.AddComponent<EnemyProjectile>();
        Vector3 direction = (player.transform.position + Vector3.up - transform.position).normalized;
        projectile.Launch(direction, projectileSpeed, enemy.damage);

        Collider projectileCollider = projectileObject.GetComponent<Collider>();
        Collider enemyCollider = GetComponent<Collider>();

        if (projectileCollider != null && enemyCollider != null)
            Physics.IgnoreCollision(projectileCollider, enemyCollider);
    }
}
