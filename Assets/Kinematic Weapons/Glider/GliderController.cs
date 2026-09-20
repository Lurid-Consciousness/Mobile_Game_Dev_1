using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GliderController : MonoBehaviour
{
    [SerializeField] private float launchSpeed = 26f;
    [SerializeField] private float flightDuration = 30f;
    [SerializeField] private float maximumClimbAngle = 25f;
    [SerializeField] private float maximumDiveAngle = 35f;
    [SerializeField] private float maximumBankAngle = 40f;
    [SerializeField] private float turnSpeed = 35f;
    [SerializeField] private float controlSmoothTime = 0.2f;
    [SerializeField] private float diveAcceleration = 12f;
    [SerializeField] private float diveDownForce = 5f;
    [SerializeField] private float climbAcceleration = 18f;
    [SerializeField] private float climbSpeedCost = 5f;
    [SerializeField] private float trimAngle = 7f;
    [SerializeField] private float liftStrength = 0.019f;
    [SerializeField] private float dragStrength = 0.0015f;
    [SerializeField] private float stallDrag = 5f;
    [SerializeField] private float stallSpeed = 9f;
    [SerializeField] private float sidewaysStability = 2.5f;
    [SerializeField] private float linearDamping = 0.015f;
    [SerializeField] private float stallAngle = 22f;
    [SerializeField] private Transform seat;

    public bool IsFlying { get; private set; }
    public float TimeRemaining => timeRemaining;
    public float Speed => rb == null ? 0f : rb.linearVelocity.magnitude;
    public float VerticalSpeed => rb == null ? 0f : rb.linearVelocity.y;
    public float Altitude => rb == null ? 0f : rb.position.y;
    public float AngleOfAttack { get; private set; }
    public bool IsStalling { get; private set; }

    private Rigidbody rb;
    private PlayerController player;
    private Rigidbody playerBody;
    private CameraController playerCamera;
    private Transform previousCameraTarget;
    private bool previousKinematic;
    private bool previousCollisions;
    private Vector3 parkedPosition;
    private Quaternion parkedRotation;
    private float timeRemaining;
    private float currentPitch;
    private float currentYaw;
    private float currentBank;
    private float pitchVelocity;
    private float bankVelocity;
    private float collisionGrace;
    private Vector2 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        parkedPosition = transform.position;
        parkedRotation = transform.rotation;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;
        rb.useGravity = true;
        rb.linearDamping = linearDamping;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    public void Launch(PlayerController pilot, Transform launchPoint)
    {
        if (IsFlying || pilot == null || launchPoint == null || pilot.IsGliding)
            return;

        player = pilot;
        playerBody = player.GetComponent<Rigidbody>();
        previousKinematic = playerBody.isKinematic;
        previousCollisions = playerBody.detectCollisions;
        player.SetGliding(true);
        playerBody.isKinematic = true;
        playerBody.detectCollisions = false;
        playerCamera = player.cameraTransform.GetComponent<CameraController>();

        if (playerCamera != null)
        {
            previousCameraTarget = playerCamera.playerTransform;
            playerCamera.playerTransform = transform;
        }

        rb.position = launchPoint.position;
        rb.rotation = launchPoint.rotation;
        currentPitch = Mathf.DeltaAngle(0f, launchPoint.eulerAngles.x);
        currentYaw = launchPoint.eulerAngles.y;
        currentBank = 0f;
        pitchVelocity = 0f;
        bankVelocity = 0f;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.None;
        rb.linearVelocity = launchPoint.forward * launchSpeed;
        rb.angularVelocity = Vector3.zero;
        timeRemaining = flightDuration;
        collisionGrace = 0.5f;
        AngleOfAttack = trimAngle;
        IsStalling = false;
        IsFlying = true;
        playerBody.position = SeatPosition();
    }

    void Update()
    {
        if (!IsFlying)
            return;

        moveInput = player.moveAction.action.ReadValue<Vector2>();
        timeRemaining -= Time.deltaTime;
        collisionGrace -= Time.deltaTime;

        if (timeRemaining <= 0f || !player.IsAlive)
            EndFlight();
    }

    void FixedUpdate()
    {
        if (!IsFlying)
            return;

        float speed = rb.linearVelocity.magnitude;
        float control = Mathf.InverseLerp(5f, 18f, speed);
        float targetPitch = moveInput.y >= 0f
            ? moveInput.y * maximumDiveAngle
            : moveInput.y * maximumClimbAngle;
        float targetBank = -moveInput.x * maximumBankAngle;
        currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref pitchVelocity, controlSmoothTime);
        currentBank = Mathf.SmoothDampAngle(currentBank, targetBank, ref bankVelocity, controlSmoothTime);
        currentYaw += moveInput.x * turnSpeed * control * Time.fixedDeltaTime;
        if (speed > 10f)
        {
            float flightPathPitch = -Mathf.Asin(Mathf.Clamp(rb.linearVelocity.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
            float highestNose = flightPathPitch - (stallAngle - trimAngle - 2f);
            float lowestNose = flightPathPitch + (stallAngle + trimAngle - 2f);
            currentPitch = Mathf.Clamp(currentPitch, highestNose, lowestNose);
        }
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, currentBank);
        rb.MoveRotation(rotation);
        rb.angularVelocity = Vector3.zero;

        if (speed > 1f)
        {
            Vector3 direction = rb.linearVelocity.normalized;
            Vector3 localVelocity = Quaternion.Inverse(rotation) * rb.linearVelocity;
            AngleOfAttack = Mathf.Atan2(-localVelocity.y, Mathf.Max(0.1f, localVelocity.z)) * Mathf.Rad2Deg + trimAngle;
            float absoluteAttack = Mathf.Abs(AngleOfAttack);
            float angleLift = 1f - Mathf.InverseLerp(stallAngle, stallAngle + 12f, absoluteAttack);
            float speedLift = Mathf.InverseLerp(stallSpeed, stallSpeed + 5f, speed);
            float liftRemaining = angleLift * speedLift;
            IsStalling = absoluteAttack >= stallAngle || speed < stallSpeed;

            Vector3 liftDirection = Vector3.ProjectOnPlane(rotation * Vector3.up, direction).normalized;
            float diveInput = Mathf.Max(0f, moveInput.y);
            float liftAcceleration = Mathf.Min(12f, speed * speed * liftStrength * liftRemaining) * (1f - diveInput * 0.8f);
            float dragAcceleration = speed * speed * dragStrength;
            if (IsStalling)
                dragAcceleration += stallDrag;
            rb.AddForce(liftDirection * liftAcceleration - direction * dragAcceleration, ForceMode.Acceleration);

            float sidewaysSpeed = localVelocity.x;
            rb.AddForce(-(rotation * Vector3.right) * sidewaysSpeed * sidewaysStability, ForceMode.Acceleration);

            if (diveInput > 0f)
            {
                Vector3 diveForce = rotation * Vector3.forward * diveAcceleration + Vector3.down * diveDownForce;
                rb.AddForce(diveForce * diveInput, ForceMode.Acceleration);
            }

            float climbInput = Mathf.Max(0f, -moveInput.y);
            if (climbInput > 0f)
            {
                float climbControl = Mathf.InverseLerp(5f, 12f, speed);
                rb.AddForce(Vector3.up * climbAcceleration * climbInput * climbControl, ForceMode.Acceleration);
                rb.AddForce(-direction * climbSpeedCost * climbInput, ForceMode.Acceleration);
            }
        }

        playerBody.MovePosition(SeatPosition());
        playerBody.MoveRotation(Quaternion.Euler(0f, currentYaw, 0f));
    }

    private Vector3 SeatPosition()
    {
        return seat != null ? seat.position : rb.position - Vector3.up;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsFlying && collisionGrace <= 0f)
            EndFlight();
    }

    private void EndFlight()
    {
        if (!IsFlying)
            return;

        IsFlying = false;
        if (player != null)
        {
            playerBody.position = rb.position + Vector3.up * 2f;
            playerBody.isKinematic = previousKinematic;
            playerBody.detectCollisions = previousCollisions;
            if (!previousKinematic)
                playerBody.linearVelocity = rb.linearVelocity;
            player.SetGliding(false);
        }

        if (playerCamera != null)
            playerCamera.playerTransform = previousCameraTarget;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.constraints = RigidbodyConstraints.FreezeAll;
        rb.position = parkedPosition;
        rb.rotation = parkedRotation;
        AngleOfAttack = 0f;
        IsStalling = false;
    }

    private void OnDisable()
    {
        EndFlight();
    }
}
