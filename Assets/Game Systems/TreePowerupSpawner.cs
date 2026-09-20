using UnityEngine;

public class TreePowerupSpawner : MonoBehaviour
{
    [SerializeField] private TreePowerup sprintPrefab;
    [SerializeField] private TreePowerup lockPrefab;
    [SerializeField] private float minimumDelay = 12f;
    [SerializeField] private float maximumDelay = 20f;
    [SerializeField] private float radius = 4f;

    private TreeObjective tree;
    private float timer = 5f;

    void Awake()
    {
        tree = GetComponent<TreeObjective>();
    }

    void Update()
    {
        if (tree == null || !tree.IsAlive)
            return;
        timer -= Time.deltaTime;
        if (timer > 0f)
            return;
        timer = Random.Range(minimumDelay, maximumDelay);
        if (FindObjectsByType<TreePowerup>(FindObjectsSortMode.None).Length >= 2)
            return;
        Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(2.5f, radius);
        Vector3 origin = transform.position + new Vector3(circle.x, 20f, circle.y);
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit ground, 50f, ~0, QueryTriggerInteraction.Ignore))
            return;
        if (ground.collider.GetComponentInParent<Defence>() != null || ground.collider.GetComponentInParent<TreeObjective>() != null)
            return;
        TreePowerup prefab = Random.value < 0.5f ? sprintPrefab : lockPrefab;
        if (prefab != null)
            Instantiate(prefab, ground.point + Vector3.up * 0.6f, Quaternion.identity);
    }
}
