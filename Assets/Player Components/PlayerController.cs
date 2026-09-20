using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public Transform cameraTransform;
    public Transform interactPoint;

    public InputActionReference moveAction;
    public InputActionReference interactAction;
    public InputActionReference throwAction;
    public BoomerangProjectile boomerang;


    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float crouchSpeed = 2.5f;
    public float jumpForce = 6f;
    public float crouchHeight = 1f;
    public float interactRange = 2f;
    public float throwForce = 10f;
    public float maxHealth = 100f;
    public float turnSpeed = 160f;
    public float quickThrowCharge = 0.45f;

    public float HealthPercent => currentHealth / maxHealth;
    public bool IsAlive => currentHealth > 0f;
    public bool CanPickup { get; private set; }
    public bool IsGliding { get; private set; }
    public bool IsPrecisionAiming { get; private set; }
    public Defence TargetDefence { get; private set; }
    public string ButtonPrompt => Application.isMobilePlatform ? "INTERACT" : interactAction.action.GetBindingDisplayString();
    public Vector2 AimInput => moveInput;

    private Rigidbody rb;
    private CapsuleCollider playerCollider;
    private Rigidbody heldObject;
    private Collider[] heldColliders;
    private Vector2 moveInput;
    private InputAction jumpAction;
    private InputAction crouchAction;
    private InputAction aimAction;
    private InputAction sprintAction;
    private float standingHeight;
    private Vector3 standingCenter;
    private float currentHealth;
    private bool jumpRequested;
    private bool isCrouching;
    private bool isChargingBoomerang;
    private float slowMultiplier = 1f;
    private float slowTimer;
    private GliderLaunchPad launchPad;
    private AcornWallet wallet;
    private WoodlandMotion motion;
    private PlayerPowerups powerups;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        powerups = GetComponent<PlayerPowerups>();
        launchPad = FindAnyObjectByType<GliderLaunchPad>();
        wallet = GetComponent<AcornWallet>();
        motion = GetComponentInChildren<WoodlandMotion>();
        playerCollider = GetComponent<CapsuleCollider>();

        jumpAction = moveAction.action.actionMap.FindAction("Jump");
        crouchAction = moveAction.action.actionMap.FindAction("Crouch");
        aimAction = moveAction.action.actionMap.FindAction("Aim");
        sprintAction = moveAction.action.actionMap.FindAction("Sprint");

        standingHeight = playerCollider.height;
        standingCenter = playerCollider.center;
        currentHealth = maxHealth;

        if (boomerang != null)
            boomerang.SetOwner(interactPoint, rb.GetComponentsInChildren<Collider>());
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        interactAction.action.Enable();
        jumpAction.Enable();
        crouchAction?.Enable();
        aimAction?.Enable();
        sprintAction.Enable();

        if (throwAction != null)
            throwAction.action.Enable();
    }

    private void OnDisable()
    {
        isChargingBoomerang = false;
        moveInput = Vector2.zero;
        jumpRequested = false;
        if (boomerang != null)
            boomerang.CancelCharge();
        if (powerups != null)
            powerups.ClearTargets();
        moveAction.action.Disable();
        interactAction.action.Disable();
        jumpAction.Disable();
        crouchAction?.Disable();
        aimAction?.Disable();
        sprintAction.Disable();
        IsPrecisionAiming = false;

        if (throwAction != null)
            throwAction.action.Disable();
    }
    void Update()
    {
        if (IsGliding)
            return;

        slowTimer -= Time.deltaTime;

        if (slowTimer <= 0f)
            slowMultiplier = 1f;

        moveInput = moveAction.action.ReadValue<Vector2>();

        if (aimAction != null && aimAction.WasPressedThisFrame())
            SetPrecisionAiming(!IsPrecisionAiming);

        SetCrouching(!IsPrecisionAiming && crouchAction != null && crouchAction.IsPressed());

        if (!IsPrecisionAiming && jumpAction.WasPressedThisFrame() && IsGrounded())
            jumpRequested = true;

        TargetDefence = null;

        if (!IsPrecisionAiming && Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit interactionHit, interactRange, ~0, QueryTriggerInteraction.Collide))
        {
            Defence barrier = interactionHit.collider.GetComponentInParent<Defence>();

            if (barrier != null && Vector3.Distance(transform.position, interactionHit.collider.ClosestPoint(transform.position)) <= 3.5f)
                TargetDefence = barrier;
        }

        if (!IsPrecisionAiming && interactAction.action.WasPressedThisFrame())
        {
            if (TargetDefence != null && TargetDefence.NeedsRepair)
                TargetDefence.TryRepair(wallet);
            else if (heldObject != null)
                Drop();
            else if (launchPad != null && launchPad.TryUse(this))
                return;
            else
                Pickup();
        }

        if (throwAction != null && heldObject != null)
        {
            if (!IsPrecisionAiming && throwAction.action.WasPressedThisFrame())
                Throw();
        }
        else if (throwAction != null && boomerang != null)
        {
            if (!IsPrecisionAiming)
            {
                if (throwAction.action.WasPressedThisFrame() && boomerang.IsReady)
                    QuickThrow();
            }
            else
            {
                if (throwAction.action.WasPressedThisFrame() && boomerang.IsReady)
                {
                    boomerang.BeginCharge();
                    isChargingBoomerang = true;
                }

                if (isChargingBoomerang && throwAction.action.IsPressed())
                {
                    boomerang.Charge(Time.deltaTime);
                    boomerang.ShowPrecisionPath(interactPoint.position, cameraTransform.forward);
                }

                if (isChargingBoomerang && throwAction.action.WasReleasedThisFrame())
                {
                    boomerang.LaunchPrecision(cameraTransform.forward, boomerang.ChargeAmount);

                    if (motion != null)
                        motion.Attack();

                    isChargingBoomerang = false;
                    SetPrecisionAiming(false);
                }
            }
        }

        CanPickup = !IsPrecisionAiming && heldObject == null && FindPickup() != null;
    }

    void FixedUpdate()
    {
        if (IsGliding)
            return;

        if (IsPrecisionAiming)
        {
            Quaternion aimRotation = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);
            rb.MoveRotation(aimRotation);

            Vector3 stoppedVelocity = rb.linearVelocity;
            stoppedVelocity.x = 0f;
            stoppedVelocity.z = 0f;
            rb.linearVelocity = stoppedVelocity;
        }
        else
        {
            float rotationAmount = moveInput.x * turnSpeed * Time.fixedDeltaTime;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, rotationAmount, 0f));

            float currentSpeed = moveSpeed;

            if (isCrouching)
                currentSpeed = crouchSpeed;
            else if (powerups != null && powerups.CanSprint && sprintAction.IsPressed())
                currentSpeed = sprintSpeed;

            currentSpeed *= slowMultiplier;

            Vector3 velocity = transform.forward * moveInput.y * currentSpeed;
            velocity.y = rb.linearVelocity.y;
            rb.linearVelocity = velocity;
        }

        if (jumpRequested)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            jumpRequested = false;
        }

        if (heldObject != null)
        {
            heldObject.MovePosition(interactPoint.position);
            heldObject.MoveRotation(interactPoint.rotation);
        }
    }

    void QuickThrow()
    {
        if (powerups != null)
        {
            powerups.ClearTargets();
            powerups.UpdateTargets(cameraTransform, 1f);
        }

        boomerang.Launch(cameraTransform.forward, quickThrowCharge, powerups != null ? powerups.Targets : null);

        if (powerups != null)
            powerups.ClearTargets();

        if (motion != null)
            motion.Attack();
    }

    public void SetPrecisionAiming(bool aiming)
    {
        if (aiming && (IsGliding || heldObject != null || boomerang == null || !boomerang.IsReady))
            return;

        IsPrecisionAiming = aiming;
        jumpRequested = false;

        if (aiming)
        {
            SetCrouching(false);

            if (powerups != null)
                powerups.ClearTargets();
        }
        else if (isChargingBoomerang && boomerang != null && boomerang.IsReady)
        {
            boomerang.CancelCharge();
            isChargingBoomerang = false;
        }
    }

    void SetCrouching(bool crouching)
    {
        if (isCrouching == crouching)
            return;

        isCrouching = crouching;

        if (isCrouching)
        {
            float newHeight = Mathf.Max(crouchHeight, playerCollider.radius * 2f);
            float heightDifference = standingHeight - newHeight;

            playerCollider.height = newHeight;
            playerCollider.center = standingCenter - Vector3.up * heightDifference * 0.5f;
        }
        else
        {
            playerCollider.height = standingHeight;
            playerCollider.center = standingCenter;
        }
    }

    bool IsGrounded()
    {
        Vector3 rayStart = transform.TransformPoint(playerCollider.center);
        float rayDistance = playerCollider.height * 0.5f + 0.15f;

        return Physics.Raycast(rayStart, Vector3.down, rayDistance, ~0, QueryTriggerInteraction.Ignore);
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        GameAudio.PlayHit();
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    public void SetSlowMultiplier(float amount)
    {
        slowMultiplier = amount;
        slowTimer = 0.2f;
    }

    Rigidbody FindPickup()
    {
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, interactRange, ~0, QueryTriggerInteraction.Ignore))
        {
            Rigidbody objectRb = hit.collider.attachedRigidbody;

            if (objectRb != null && objectRb != rb && objectRb.GetComponent<GliderController>() == null && objectRb.GetComponent<BoomerangProjectile>() == null && objectRb.GetComponent<EnemyProjectile>() == null)

                return objectRb;
        }
        return null;
    }

    public void SetGliding(bool gliding)
    {
        SetPrecisionAiming(false);

        if (gliding && heldObject != null)
            Drop();

        IsGliding = gliding;
        TargetDefence = null;
        CanPickup = false;
        jumpRequested = false;
        isChargingBoomerang = false;
        if (powerups != null)
            powerups.ClearTargets();

        if (boomerang != null)
        {
            boomerang.CancelCharge();
            boomerang.SetPocketed(gliding);
        }
    }

    void Pickup()
    {
        SetPrecisionAiming(false);

        Rigidbody objectRb = FindPickup();

        if (objectRb == null)
            return;

        heldObject = objectRb;
        heldColliders = heldObject.GetComponentsInChildren<Collider>();

        if (!heldObject.isKinematic)
        {
            heldObject.linearVelocity = Vector3.zero;
            heldObject.angularVelocity = Vector3.zero;
        }


        foreach (Collider objectCollider in heldColliders)
            objectCollider.enabled = false;

        heldObject.isKinematic = true;
        heldObject.useGravity = false;
        heldObject.position = interactPoint.position;
        heldObject.rotation = interactPoint.rotation;

        isChargingBoomerang = false;

        if (boomerang != null)
        {
            boomerang.CancelCharge();
            boomerang.SetPocketed(true);
        }
    }

    void Drop()
    {
        heldObject.isKinematic = false;
        heldObject.useGravity = true;

        foreach (Collider objectCollider in heldColliders)
            objectCollider.enabled = true;

        heldObject = null;
        heldColliders = null;

        if (boomerang != null)
            boomerang.SetPocketed(false);
    }

    void Throw()
    {
        if (motion != null)
            motion.Attack();
        Rigidbody thrownObject = heldObject;
        AcornProjectile acornProjectile = thrownObject.GetComponent<AcornProjectile>();

        Drop();

        if (acornProjectile != null)
            acornProjectile.Launch(cameraTransform.forward);
        else
        {
            thrownObject.AddForce(cameraTransform.forward * throwForce, ForceMode.Impulse);
            GameAudio.PlayThrow();
        }
    }
}
