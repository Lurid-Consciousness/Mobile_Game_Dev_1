using UnityEngine;

public class RaccoonEnemy : MonoBehaviour
{
    public TreeObjective tree;
    public float moveSpeed = 3f;
    public float attackDistance = 1.5f;
    public float attackDelay = 1f;
    public float playerFightDistance = 2f;
    public float riseDistance = 1.5f;
    public float riseSpeed = 2f;

    private Enemy enemy;
    private PlayerController player;
    private Vector3 surfacePosition;
    private float attackTimer;
    private float slowMultiplier = 1f;
    private float slowTimer;
    private bool isRising = true;

    void Start()
    {
        enemy = GetComponent<Enemy>();
        player = FindAnyObjectByType<PlayerController>();

        if (tree == null)
            tree = FindAnyObjectByType<TreeObjective>();

        surfacePosition = transform.position;
        transform.position -= Vector3.up * riseDistance;

        Renderer enemyRenderer = GetComponent<Renderer>();

        if (enemyRenderer != null)
            enemyRenderer.material.color = Color.gray;
    }

    void Update()
    {
        if (tree == null || player == null || !tree.IsAlive)
            return;

        if (isRising)
        {
            transform.position = Vector3.MoveTowards(transform.position, surfacePosition, riseSpeed * Time.deltaTime);

            if (transform.position == surfacePosition)
                isRising = false;

            return;
        }

        attackTimer -= Time.deltaTime;
        slowTimer -= Time.deltaTime;

        if (slowTimer <= 0f)
            slowMultiplier = 1f;

        Vector3 treePosition = FlatPosition(tree.transform.position);
        Vector3 playerPosition = FlatPosition(player.transform.position);
        Vector3 treeDirection = (treePosition - transform.position).normalized;
        Vector3 playerDirection = (playerPosition - transform.position).normalized;
        float playerDistance = Vector3.Distance(transform.position, playerPosition);
        bool playerIsBlocking = playerDistance <= playerFightDistance && Vector3.Dot(treeDirection, playerDirection) > 0.5f;
        Vector3 targetPosition = playerIsBlocking ? playerPosition : treePosition;
        float targetDistance = Vector3.Distance(transform.position, targetPosition);

        Defence barrier = Defence.FindBlocking(transform.position, targetPosition, attackDistance + moveSpeed * Time.deltaTime);
        if (barrier != null)
        {
            transform.LookAt(FlatPosition(barrier.transform.position));
            if (attackTimer <= 0f)
            {
                barrier.TakeDamage(enemy.damage);
                enemy.ShowAttack();
                attackTimer = attackDelay;
            }
            return;
        }

        if (targetDistance > attackDistance)
        {
            MoveTo(targetPosition);
        }
        else
        {
            if (attackTimer <= 0f)
            {
                if (playerIsBlocking)
                    player.TakeDamage(enemy.damage);
                else
                    tree.TakeDamage(enemy.damage);

                enemy.ShowAttack();

                attackTimer = attackDelay;
            }
        }
    }

    public void SetSlowMultiplier(float amount)
    {
        slowMultiplier = amount;
        slowTimer = 0.2f;
    }

    void MoveTo(Vector3 targetPosition)
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * slowMultiplier * Time.deltaTime);
        transform.LookAt(targetPosition);
    }

    Vector3 FlatPosition(Vector3 position)
    {
        position.y = transform.position.y;
        return position;
    }
}
