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
    public float jumpForce = 6f;
    public float interactRange = 2f;
    public float throwForce = 10f;
    public float maxHealth = 100f;

    public float HealthPercent => currentHealth / maxHealth;
    public bool IsAlive => currentHealth > 0f;
    public bool CanPickup { get; private set; }
    public bool IsGliding { get; private set; }
    public Defence TargetDefence { get; private set; }
    public string ButtonPrompt => interactAction.action.GetBindingDisplayString();

    private Rigidbody rb;
    private CapsuleCollider playerCollider;
    private Rigidbody heldObject;
    private Collider[] heldColliders;
    private Vector2 moveInput;
    private InputAction jumpAction;
    private InputAction curveAction;
    private InputAction sprintAction;
    private float currentHealth;
    private bool jumpRequested;
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
        curveAction = moveAction.action.actionMap.FindAction("Curve");
        sprintAction = moveAction.action.actionMap.FindAction("Sprint");

        currentHealth = maxHealth;

        if (boomerang != null)
            boomerang.SetOwner(interactPoint, rb.GetComponentsInChildren<Collider>());
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        interactAction.action.Enable();
        jumpAction.Enable();
        curveAction?.Enable();
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
        curveAction?.Disable();
        sprintAction.Disable();

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

        if (boomerang != null && curveAction != null && boomerang.IsReady)
        {
            float scroll = curveAction.ReadValue<float>();
            if (Mathf.Abs(scroll) > 0.01f)
                boomerang.AdjustCurve(Mathf.Sign(scroll) * PlayerPrefs.GetFloat("CurveStep", 15f));
        }

        if (jumpAction.WasPressedThisFrame() && IsGrounded())
            jumpRequested = true;

        TargetDefence = null;
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit interactionHit, interactRange, ~0, QueryTriggerInteraction.Collide))
        {
            Defence barrier = interactionHit.collider.GetComponentInParent<Defence>();
            if (barrier != null && Vector3.Distance(transform.position, interactionHit.collider.ClosestPoint(transform.position)) <= 3.5f)
                TargetDefence = barrier;
        }

        if (interactAction.action.WasPressedThisFrame())
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
            if (throwAction.action.WasPressedThisFrame())
                Throw();
        }
        else if (throwAction != null && boomerang != null)
        {
            if (throwAction.action.WasPressedThisFrame() && boomerang.IsReady)
            {
                boomerang.BeginCharge();
                if (powerups != null)
                    powerups.ClearTargets();
                isChargingBoomerang = true;
            }

            if (isChargingBoomerang && throwAction.action.IsPressed())
            {
                boomerang.Charge(Time.deltaTime);
                if (powerups != null)
                    powerups.UpdateTargets(cameraTransform, boomerang.ChargeAmount);
                boomerang.ShowPath(interactPoint.position, cameraTransform.forward, powerups != null ? powerups.Targets : null);
            }

            if (isChargingBoomerang && throwAction.action.WasReleasedThisFrame())
            {
                boomerang.Launch(cameraTransform.forward, boomerang.ChargeAmount, powerups != null ? powerups.Targets : null);
                if (powerups != null)
                    powerups.ClearTargets();
                if (motion != null)
                    motion.Attack();
                isChargingBoomerang = false;
            }
        }

        CanPickup = heldObject == null && FindPickup() != null;
    }

    void FixedUpdate()
    {
        if (IsGliding)
            return;

        Quaternion playerRotation = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);
        rb.MoveRotation(playerRotation);

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 move = forward * moveInput.y + right * moveInput.x;
        move.Normalize();

        float currentSpeed = moveSpeed;

        if (powerups != null && powerups.CanSprint && sprintAction.IsPressed())
            currentSpeed = sprintSpeed;

        currentSpeed *= slowMultiplier;

        Vector3 velocity = move * currentSpeed;
        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;

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
