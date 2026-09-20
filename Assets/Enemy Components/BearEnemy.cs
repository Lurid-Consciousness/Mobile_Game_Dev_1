using UnityEngine;

public class BearEnemy : MonoBehaviour
{
    public TreeObjective tree;
    public float moveSpeed = 2f;
    public float attackDistance = 2f;
    public float attackDelay = 1.5f;
    public float aggroDistance = 5f;
    public float aggroTime = 5f;
    public float honeyRange = 10f;
    public float honeyPrepareTime = 1.5f;
    public float honeyLockTime = 0.5f;
    public float honeyDelay = 6f;

    private Enemy enemy;
    private PlayerController player;
    private Renderer enemyRenderer;
    private float attackTimer;
    private float aggroTimer;
    private float honeyTimer;
    private float prepareTimer;
    private bool preparingHoney;
    private HoneyPuddle honeyWarning;

    void Start()
    {
        enemy = GetComponent<Enemy>();
        player = FindAnyObjectByType<PlayerController>();

        if (tree == null)
            tree = FindAnyObjectByType<TreeObjective>();

        enemyRenderer = GetComponent<Renderer>();

        if (enemyRenderer != null)
            enemyRenderer.material.color = new Color(0.4f, 0.2f, 0.05f);

    }

    void Update()
    {
        if (tree == null || player == null || !tree.IsAlive)
            return;

        attackTimer -= Time.deltaTime;
        aggroTimer -= Time.deltaTime;
        honeyTimer -= Time.deltaTime;

        Vector3 playerPosition = FlatPosition(player.transform.position);
        Vector3 treePosition = FlatPosition(tree.transform.position);
        float playerDistance = Vector3.Distance(transform.position, playerPosition);

        if (playerDistance <= aggroDistance)
            Aggro();

        if (!preparingHoney && honeyTimer <= 0f && playerDistance <= honeyRange)
        {
            preparingHoney = true;
            prepareTimer = honeyPrepareTime;
            CreateHoneyWarning();

            if (enemyRenderer != null)
                enemyRenderer.material.color = Color.yellow;
        }

        if (preparingHoney)
        {
            prepareTimer -= Time.deltaTime;

            if (honeyWarning != null && prepareTimer > honeyLockTime)
                honeyWarning.transform.position = GetHoneyPosition(player.transform.position);

            if (prepareTimer <= 0f)
            {
                if (honeyWarning != null)
                    honeyWarning.Activate();

                honeyWarning = null;
                preparingHoney = false;
                honeyTimer = honeyDelay;

                if (enemyRenderer != null)
                    enemyRenderer.material.color = new Color(0.4f, 0.2f, 0.05f);
            }
        }

        bool targetsPlayer = aggroTimer > 0f;
        Vector3 targetPosition = targetsPlayer ? playerPosition : treePosition;
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
                if (targetsPlayer)
                    player.TakeDamage(enemy.damage);
                else
                    tree.TakeDamage(enemy.damage);

                enemy.ShowAttack();

                attackTimer = attackDelay;
            }
        }
    }

    public void Aggro()
    {
        aggroTimer = aggroTime;
    }

    void CreateHoneyWarning()
    {
        GameObject honey = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        honey.name = "Honey Warning";
        honey.transform.position = GetHoneyPosition(player.transform.position);
        honey.transform.localScale = new Vector3(2f, 0.05f, 2f);
        Collider honeyCollider = honey.GetComponent<Collider>();
        honeyCollider.isTrigger = true;
        honeyWarning = honey.AddComponent<HoneyPuddle>();
        honeyWarning.ShowWarning();
    }

    Vector3 GetHoneyPosition(Vector3 position)
    {
        return new Vector3(position.x, 0.1f, position.z);
    }

    void OnDestroy()
    {
        if (honeyWarning != null)
            Destroy(honeyWarning.gameObject);
    }

    void MoveTo(Vector3 targetPosition)
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        transform.LookAt(targetPosition);
    }

    Vector3 FlatPosition(Vector3 position)
    {
        position.y = transform.position.y;
        return position;
    }
}
