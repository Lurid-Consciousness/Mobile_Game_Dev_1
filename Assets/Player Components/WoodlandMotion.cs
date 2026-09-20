using UnityEngine;

public class WoodlandMotion : MonoBehaviour
{
    [SerializeField] private Transform[] legs = new Transform[0];
    [SerializeField] private Transform tail;
    [SerializeField] private Animator animator;
    [SerializeField] private float walkRate = 10f;

    private Vector3 lastPosition;
    private Vector3 restingPosition;
    private Quaternion restingRotation;
    private Quaternion tailRotation;
    private Quaternion[] legRotations;
    private float attackTime;
    private float walkTime;

    void OnEnable()
    {
        lastPosition = transform.parent.position;
        restingPosition = transform.localPosition;
        restingRotation = transform.localRotation;
        tailRotation = tail != null ? tail.localRotation : Quaternion.identity;
        legRotations = new Quaternion[legs.Length];
        for (int i = 0; i < legs.Length; i++)
            legRotations[i] = legs[i].localRotation;
    }

    void LateUpdate()
    {
        if (Time.deltaTime <= 0f)
            return;

        Vector3 movement = transform.parent.position - lastPosition;
        lastPosition = transform.parent.position;
        movement.y = 0f;
        float speed = movement.magnitude / Time.deltaTime;
        float amount = Mathf.Clamp01(speed / 2f);
        walkTime += Time.deltaTime * walkRate * Mathf.Clamp(speed, 0.5f, 2f);
        attackTime = Mathf.Max(0f, attackTime - Time.deltaTime);

        if (animator != null)
            animator.SetBool("Moving", speed > 0.15f);

        for (int i = 0; i < legs.Length; i++)
        {
            float swing = Mathf.Sin(walkTime + (i % 2) * Mathf.PI) * 25f * amount;
            legs[i].localRotation = legRotations[i] * Quaternion.Euler(swing, 0f, 0f);
        }

        if (tail != null)
            tail.localRotation = tailRotation * Quaternion.Euler(0f, Mathf.Sin(Time.time * 3f) * (4f + amount * 8f), 0f);

        float attack = Mathf.Sin(attackTime / 0.3f * Mathf.PI);
        transform.localPosition = restingPosition + Vector3.up * Mathf.Abs(Mathf.Sin(walkTime)) * 0.04f * amount;
        transform.localRotation = restingRotation * Quaternion.Euler(-attack * 15f, 0f, 0f);
    }

    public void Attack()
    {
        attackTime = 0.3f;
    }
}
