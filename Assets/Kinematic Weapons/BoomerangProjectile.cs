using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(LineRenderer))]
public class BoomerangProjectile : MonoBehaviour
{
    public float minimumDistance = 4f;
    public float maximumDistance = 12f;
    public float minimumFlightTime = 1f;
    public float maximumFlightTime = 2f;
    public float chargeTime = 1.5f;
    public float curveAmount = 3f;
    public float arcHeight = 2f;
    public float spinSpeed = 720f;
    public float damage = 20f;
    public Vector3 spinAxis = Vector3.up;
    public int pathPoints = 40;

    public float ChargeAmount { get; private set; }
    public bool IsReady { get; private set; }
    public float CurveAngle { get; private set; }

    private Rigidbody rb;
    private LineRenderer lineRenderer;
    private Transform returnPoint;
    private Collider[] boomerangColliders;
    private MeshRenderer[] meshRenderers;
    private Collider[] ownerColliders;
    private Vector3 startPosition;
    private Vector3 launchDirection;
    private float flightTime;
    private float flightProgress;
    private bool isFlying;
    private bool isPocketed;
    private Enemy lastEnemyHit;
    private float castRadius = 0.1f;
    [SerializeField] private float lockFlightSpeed = 25f;
    private float launchedCurveAngle;
    private readonly List<Enemy> flightTargets = new List<Enemy>();
    private readonly HashSet<Enemy> hitEnemies = new HashSet<Enemy>();
    private int targetIndex;
    private bool lockFlight;
    private float lockFlightTimer;

    public void AdjustCurve(float amount)
    {
        CurveAngle = Mathf.Clamp(CurveAngle + amount, -90f, 90f);
    }

    void Awake()
    {
        Initialize();
    }

    void Initialize()
    {
        if (rb != null)
            return;

        rb = GetComponent<Rigidbody>();
        lineRenderer = GetComponent<LineRenderer>();
        boomerangColliders = GetComponentsInChildren<Collider>();
        meshRenderers = GetComponentsInChildren<MeshRenderer>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        lineRenderer.useWorldSpace = true;
        lineRenderer.widthMultiplier = 0.03f;
        lineRenderer.startColor = Color.yellow;
        lineRenderer.endColor = Color.yellow;
        lineRenderer.positionCount = 0;
        lineRenderer.enabled = false;

        if (lineRenderer.sharedMaterial == null)
        {
            Shader lineShader = Shader.Find("Universal Render Pipeline/Unlit");

            if (lineShader == null)
                lineShader = Shader.Find("Sprites/Default");

            if (lineShader != null)
                lineRenderer.material = new Material(lineShader);
        }
    }

    void FixedUpdate()
    {
        if (!isFlying)
        {
            if (IsReady && returnPoint != null)
            {
                rb.MovePosition(returnPoint.position);
                rb.MoveRotation(returnPoint.rotation);
            }

            return;
        }

        if (lockFlight)
        {
            FlyToTargets();
            return;
        }
        flightProgress += Time.fixedDeltaTime / flightTime;

        float progress = Mathf.Clamp01(flightProgress);
        Vector3 targetPosition = returnPoint != null ? returnPoint.position : startPosition;
        Vector3 nextPosition = GetFlightPosition(progress, startPosition, launchDirection, ChargeAmount, targetPosition);

        Vector3 movement = nextPosition - rb.position;

        if (movement.sqrMagnitude > 0f && FindHit(movement, out RaycastHit hit))
        {
            Enemy enemy = hit.collider.GetComponentInParent<Enemy>();

            if (enemy != null)
            {
                if (hitEnemies.Add(enemy))
                {
                    enemy.TakeDamage(damage);
                    lastEnemyHit = enemy;
                }
            }
            else
            {
                Defence barrier = hit.collider.GetComponentInParent<Defence>();
                if (barrier != null)
                    barrier.TakeDamage(damage);
                FinishFlight();
                return;
            }
        }

        rb.MovePosition(nextPosition);

        Vector3 axis = spinAxis.sqrMagnitude > 0f ? spinAxis.normalized : Vector3.up;
        Quaternion spin = Quaternion.AngleAxis(spinSpeed * Time.fixedDeltaTime, axis);
        rb.MoveRotation(rb.rotation * spin);

        if (flightProgress >= 1f)
            FinishFlight();
    }

    public void SetOwner(Transform catchPoint, Collider[] playerColliders)
    {
        Initialize();
        returnPoint = catchPoint;
        ownerColliders = playerColliders;
        isFlying = false;
        IsReady = true;
        ChargeAmount = 0f;
        rb.isKinematic = true;
        rb.useGravity = false;
        SetColliders(false);
        SetVisible(true);
        rb.position = returnPoint.position;
        rb.rotation = returnPoint.rotation;
        HidePath();
    }

    public void SetPocketed(bool pocketed)
    {
        isPocketed = pocketed;

        if (!isFlying)
            SetVisible(!isPocketed);
    }

    public void BeginCharge()
    {
        ChargeAmount = 0f;
    }

    public void CancelCharge()
    {
        ChargeAmount = 0f;
        HidePath();
    }

    public void Charge(float deltaTime)
    {
        ChargeAmount += deltaTime / Mathf.Max(0.01f, chargeTime);
        ChargeAmount = Mathf.Clamp01(ChargeAmount);
    }

    public void ShowPath(Vector3 pathStart, Vector3 direction, List<Enemy> targets = null)
    {
        if (targets != null && targets.Count > 0)
        {
            lineRenderer.enabled = true;
            lineRenderer.positionCount = targets.Count + 2;
            lineRenderer.SetPosition(0, pathStart);
            for (int i = 0; i < targets.Count; i++)
                lineRenderer.SetPosition(i + 1, targets[i] != null ? targets[i].transform.position : pathStart);
            lineRenderer.SetPosition(targets.Count + 1, pathStart);
            return;
        }
        int pointCount = Mathf.Max(2, pathPoints);
        Vector3 normalizedDirection = direction.normalized;

        lineRenderer.positionCount = pointCount;
        lineRenderer.enabled = true;

        for (int i = 0; i < pointCount; i++)
        {
            float progress = (float)i / (pointCount - 1);
            Vector3 point = GetFlightPosition(progress, pathStart, normalizedDirection, ChargeAmount, pathStart);
            lineRenderer.SetPosition(i, point);
        }
    }

    public void HidePath()
    {
        lineRenderer.enabled = false;
        lineRenderer.positionCount = 0;
    }

    public void Launch(Vector3 direction, float chargeAmount, List<Enemy> targets = null)
    {
        if (!IsReady || isPocketed)
            return;

        GameAudio.PlayThrow();

        startPosition = rb.position;
        launchDirection = direction.normalized;
        ChargeAmount = Mathf.Clamp01(chargeAmount);
        flightTime = Mathf.Lerp(minimumFlightTime, maximumFlightTime, ChargeAmount);
        flightProgress = 0f;
        isFlying = true;
        IsReady = false;
        lastEnemyHit = null;
        hitEnemies.Clear();
        launchedCurveAngle = CurveAngle;
        flightTargets.Clear();
        if (targets != null)
        {
            foreach (Enemy enemy in targets)
                if (enemy != null && enemy.HealthPercent > 0f && !flightTargets.Contains(enemy) && flightTargets.Count < 4)
                    flightTargets.Add(enemy);
        }
        targetIndex = 0;
        lockFlight = flightTargets.Count > 0;
        lockFlightTimer = 0f;

        HidePath();
        SetVisible(true);
        SetColliders(true);

        rb.isKinematic = true;
        rb.useGravity = false;

        foreach (Collider boomerangCollider in boomerangColliders)
        {
            foreach (Collider ownerCollider in ownerColliders)
                Physics.IgnoreCollision(boomerangCollider, ownerCollider, true);
        }
    }

    Vector3 GetFlightPosition(float progress, Vector3 origin, Vector3 direction, float chargeAmount, Vector3 targetPosition)
    {
        float distance = Mathf.Lerp(minimumDistance, maximumDistance, chargeAmount);
        float outwardDistance = Mathf.Sin(progress * Mathf.PI);
        float sidewaysDistance = Mathf.Sin(progress * Mathf.PI * 2f);

        Vector3 sideDirection = Vector3.Cross(Vector3.up, direction).normalized;
        if (sideDirection.sqrMagnitude < 0.01f)
            sideDirection = Vector3.right;
        Quaternion curveRotation = Quaternion.AngleAxis(isFlying ? launchedCurveAngle : CurveAngle, direction);
        Vector3 arcDirection = curveRotation * Vector3.Cross(direction, sideDirection).normalized;
        sideDirection = curveRotation * sideDirection;

        Vector3 position = origin;
        position += direction * distance * outwardDistance;
        position += arcDirection * arcHeight * outwardDistance;
        position += sideDirection * curveAmount * sidewaysDistance;

        if (progress > 0.5f)
        {
            float returnProgress = (progress - 0.5f) * 2f;
            position += (targetPosition - origin) * returnProgress;
        }

        return position;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isFlying)
            return;

        Enemy enemy = collision.collider.GetComponentInParent<Enemy>();

        if (enemy != null && hitEnemies.Add(enemy))
        {
            enemy.TakeDamage(damage);
            lastEnemyHit = enemy;
        }
    }

    bool FindHit(Vector3 movement, out RaycastHit closestHit)
    {
        RaycastHit[] hits = Physics.SphereCastAll(rb.position, castRadius, movement.normalized, movement.magnitude, ~0, QueryTriggerInteraction.Ignore);
        float closestDistance = float.MaxValue;
        bool foundHit = false;
        closestHit = new RaycastHit();

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform))
                continue;
            Enemy enemy = hit.collider.GetComponentInParent<Enemy>();
            if (enemy != null && hitEnemies.Contains(enemy))
                continue;

            bool belongsToOwner = false;

            foreach (Collider ownerCollider in ownerColliders)
            {
                if (hit.collider == ownerCollider)
                {
                    belongsToOwner = true;
                    break;
                }
            }

            if (!belongsToOwner && hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestHit = hit;
                foundHit = true;
            }
        }

        return foundHit;
    }

    void FinishFlight()
    {
        lockFlight = false;
        flightTargets.Clear();
        isFlying = false;
        IsReady = true;
        ChargeAmount = 0f;

        rb.isKinematic = true;
        rb.useGravity = false;
        SetColliders(false);
        SetVisible(!isPocketed);

        if (returnPoint != null)
            rb.position = returnPoint.position;
    }

    private void FlyToTargets()
    {
        lockFlightTimer += Time.fixedDeltaTime;
        while (targetIndex < flightTargets.Count &&
               (flightTargets[targetIndex] == null || flightTargets[targetIndex].HealthPercent <= 0f || hitEnemies.Contains(flightTargets[targetIndex])))
            targetIndex++;
        if (lockFlightTimer > 8f)
            targetIndex = flightTargets.Count;
        bool returning = targetIndex >= flightTargets.Count;
        Vector3 target = returning ? (returnPoint != null ? returnPoint.position : startPosition) : flightTargets[targetIndex].transform.position;
        Vector3 next = Vector3.MoveTowards(rb.position, target, lockFlightSpeed * Time.fixedDeltaTime);
        Vector3 movement = next - rb.position;
        if (!returning && movement.sqrMagnitude > 0f && FindHit(movement, out RaycastHit hit))
        {
            Enemy enemy = hit.collider.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                if (hitEnemies.Add(enemy))
                    enemy.TakeDamage(damage);
            }
            else
            {
                Defence barrier = hit.collider.GetComponentInParent<Defence>();
                if (barrier != null)
                    barrier.TakeDamage(damage);
                targetIndex = flightTargets.Count;
                next = rb.position;
            }
        }
        rb.MovePosition(next);
        rb.MoveRotation(rb.rotation * Quaternion.AngleAxis(spinSpeed * Time.fixedDeltaTime, Vector3.up));
        if (Vector3.Distance(next, target) < 0.1f)
        {
            if (returning)
                FinishFlight();
            else
                targetIndex++;
        }
    }

    void SetColliders(bool value)
    {
        foreach (Collider boomerangCollider in boomerangColliders)
            boomerangCollider.enabled = value;
    }

    void SetVisible(bool value)
    {
        foreach (MeshRenderer meshRenderer in meshRenderers)
            meshRenderer.enabled = value;
    }
}
