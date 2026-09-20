using UnityEngine;

public class Defence : MonoBehaviour
{


    [SerializeField] private float maxHealth = 300f;
    [SerializeField] private int repairCost = 20;
    [SerializeField] private GameObject intactVisuals;
    private float currentHealth;
    private Collider wallCollider;

    public float WallHealth => currentHealth / maxHealth;
    public bool IsBroken => currentHealth <= 0f;
    public bool NeedsRepair => currentHealth < maxHealth;
    public int RepairCost => repairCost;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        currentHealth = maxHealth;
        wallCollider = GetComponent<Collider>();
    }


    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        UpdateBarrier();
    }

    public bool TryRepair(AcornWallet wallet)
    {
        if (!NeedsRepair || wallet == null)
            return false;

        if (IsBroken)
        {
            BoxCollider box = (BoxCollider)wallCollider;
            Vector3 halfSize = Vector3.Scale(box.size, transform.lossyScale) * 0.45f;
            foreach (Collider occupant in Physics.OverlapBox(transform.TransformPoint(box.center), halfSize, transform.rotation, ~0, QueryTriggerInteraction.Ignore))
            {
                if (occupant.GetComponentInParent<Enemy>() != null || occupant.GetComponentInParent<PlayerController>() != null)
                    return false;
            }
        }

        if (!wallet.TrySpend(repairCost))
            return false;

        currentHealth = maxHealth;
        UpdateBarrier();
        GameAudio.PlayButton();
        return true;
    }

    private void UpdateBarrier()
    {
        wallCollider.isTrigger = IsBroken;
        if (intactVisuals != null)
            intactVisuals.SetActive(!IsBroken);
    }

    public static Defence FindBlocking(Vector3 position, Vector3 target, float distance)
    {
        Vector3 direction = target - position;
        if (direction.sqrMagnitude < 0.01f)
            return null;

        RaycastHit[] hits = Physics.SphereCastAll(position, 0.45f, direction.normalized,
            Mathf.Min(distance, direction.magnitude), ~0, QueryTriggerInteraction.Ignore);
        Defence closest = null;
        float closestDistance = float.MaxValue;
        foreach (RaycastHit hit in hits)
        {
            Defence barrier = hit.collider.GetComponentInParent<Defence>();
            if (barrier != null && !barrier.IsBroken && hit.distance < closestDistance)
            {
                closest = barrier;
                closestDistance = hit.distance;
            }
        }
        return closest;
    }
}
