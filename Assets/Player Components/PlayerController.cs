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
    public float maxStamina = 100f;
    public float chargeStaminaPerSecond = 30f;
    public float staminaRecoveryPerSecond = 20f;

    public float HealthPercent => currentHealth / maxHealth;
    public float StaminaPercent => currentStamina / maxStamina;
    public bool IsAlive => currentHealth > 0f;
    public bool CanPickup { get; private set; }
    public string ButtonPrompt => interactAction.action.GetBindingDisplayString();

    private Rigidbody rb;
    private CapsuleCollider playerCollider;
    private Rigidbody heldObject;
    private Collider[] heldColliders;
    private Vector2 moveInput;
    private InputAction jumpAction;
    private InputAction crouchAction;
    private InputAction sprintAction;
    private float standingHeight;
    private Vector3 standingCenter;
    private float currentHealth;
    private float currentStamina;
    private bool jumpRequested;
    private bool isCrouching;
    private bool isChargingBoomerang;
    private float slowMultiplier = 1f;
    private float slowTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();

        jumpAction = moveAction.action.actionMap.FindAction("Jump");
        crouchAction = moveAction.action.actionMap.FindAction("Crouch");
        sprintAction = moveAction.action.actionMap.FindAction("Sprint");

        standingHeight = playerCollider.height;
        standingCenter = playerCollider.center;
        currentHealth = maxHealth;
        currentStamina = maxStamina;

        if (boomerang != null)
            boomerang.SetOwner(interactPoint, rb.GetComponentsInChildren<Collider>());
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        interactAction.action.Enable();
        jumpAction.Enable();
        crouchAction.Enable();
        sprintAction.Enable();

        if (throwAction != null)
            throwAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        interactAction.action.Disable();
        jumpAction.Disable();
        crouchAction.Disable();
        sprintAction.Disable();

        if (throwAction != null)
            throwAction.action.Disable();
    }
    void Update()
    {
        slowTimer -= Time.deltaTime;

        if (slowTimer <= 0f)
            slowMultiplier = 1f;

        moveInput = moveAction.action.ReadValue<Vector2>();

        SetCrouching(crouchAction.IsPressed());

        if (jumpAction.WasPressedThisFrame() && IsGrounded())
            jumpRequested = true;

        bool usedStamina = false;

        if (interactAction.action.WasPressedThisFrame())
        {
            if (heldObject == null)
                Pickup();
            else
                Drop();
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
                isChargingBoomerang = true;
            }

            if (isChargingBoomerang && throwAction.action.IsPressed())
            {
                float chargeDelta = Time.deltaTime;

                if (chargeStaminaPerSecond > 0f)
                    chargeDelta = Mathf.Min(chargeDelta, currentStamina / chargeStaminaPerSecond);

                if (chargeDelta > 0f)
                {
                    boomerang.Charge(chargeDelta);
                    currentStamina -= chargeStaminaPerSecond * chargeDelta;
                    currentStamina = Mathf.Max(0f, currentStamina);
                    usedStamina = true;
                }

                boomerang.ShowPath(interactPoint.position, cameraTransform.forward);
            }

            if (isChargingBoomerang && throwAction.action.WasReleasedThisFrame())
            {
                boomerang.Launch(cameraTransform.forward, boomerang.ChargeAmount);
                isChargingBoomerang = false;
            }
        }

        if (!usedStamina)
            currentStamina = Mathf.MoveTowards(currentStamina, maxStamina, staminaRecoveryPerSecond * Time.deltaTime);

        CanPickup = heldObject == null && FindPickup() != null;
    }

    void FixedUpdate()
    {
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

        if (isCrouching)
            currentSpeed = crouchSpeed;
        else if (sprintAction.IsPressed())
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
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, interactRange))
        {
            Rigidbody objectRb = hit.collider.attachedRigidbody;

            if (objectRb != null && objectRb != rb && objectRb.GetComponent<BoomerangProjectile>() == null && objectRb.GetComponent<EnemyProjectile>() == null)

                return objectRb;
        }
        return null;
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
