using UnityEngine;

public class AcornProjectile : MonoBehaviour
{

    public float speed = 10f;
    public float damage = 25f;

    private Rigidbody rb;
    private Vector3 direction;
    private bool isMoving;
    private float castRadius;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        castRadius = sphereCollider != null ? sphereCollider.radius * transform.lossyScale.x : 0.1f;
    }

    public void PreparePickup()
    {
        isMoving = false;
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void Launch(Vector3 launchDirection)
    {
        direction = launchDirection.normalized;
        rb.isKinematic = true;
        rb.useGravity = false;
        isMoving = true;
        GameAudio.PlayThrow();
    }

    private void FixedUpdate()
    {
        if (isMoving)
        {
            float distance = speed * Time.fixedDeltaTime;

            if (FindHit(distance, out RaycastHit hit))
            {
                HitObject(hit.collider, hit.point, hit.normal);
                return;
            }

            Vector3 nextPosition = rb.position + direction * distance;
            rb.MovePosition(nextPosition);
        }
    }

    bool FindHit(float distance, out RaycastHit closestHit)
    {
        RaycastHit[] hits = Physics.SphereCastAll(rb.position, castRadius, direction, distance, ~0, QueryTriggerInteraction.Ignore);
        float closestDistance = float.MaxValue;
        bool foundHit = false;
        closestHit = new RaycastHit();

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.attachedRigidbody == rb || hit.collider.GetComponentInParent<PlayerController>() != null)
                continue;

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestHit = hit;
                foundHit = true;
            }
        }

        return foundHit;
    }

    void OnCollisionEnter(Collision collision)
    {
        HitObject(collision.collider, collision.GetContact(0).point, collision.GetContact(0).normal);
    }

    void HitObject(Collider hitCollider, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (!isMoving)
            return;

        Enemy enemy = hitCollider.GetComponentInParent<Enemy>();

        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        isMoving = false;
        rb.position = hitPoint + hitNormal * castRadius;
        rb.isKinematic = false;
        rb.useGravity = true;
    }
}
