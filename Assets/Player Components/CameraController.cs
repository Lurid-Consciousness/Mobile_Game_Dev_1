using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public Transform playerTransform;
    public InputActionReference lookAction;

    public float mouseSensitivity = 15f;
    public float controllerSensitivity = 120f;
    public float distanceFromPlayer = 5f;
    public float playerHeight = 2f;
    public float shoulderDistance = 1.2f;
    public float shoulderSpeed = 8f;
    public float followSpeed = 12f;
    public float normalPitch = 12f;

    private PlayerController playerController;
    private InputAction previousAction;
    private InputAction nextAction;
    private float cameraYaw;
    private float cameraPitch;
    private float shoulderSide = 1f;
    private float currentShoulderDistance;
    private int ignoreLookFrames;

    void Awake()
    {
        playerController = playerTransform.GetComponent<PlayerController>();
        cameraYaw = playerTransform.eulerAngles.y;
        cameraPitch = normalPitch;
        currentShoulderDistance = shoulderDistance;
    }

    void OnEnable()
    {
        previousAction = lookAction.action.actionMap.FindAction("Previous");
        nextAction = lookAction.action.actionMap.FindAction("Next");

        lookAction.action.Enable();
        previousAction?.Enable();
        nextAction?.Enable();
        ignoreLookFrames = 2;
    }

    void OnDisable()
    {
        lookAction.action.Disable();
        previousAction?.Disable();
        nextAction?.Disable();
    }

    void OnApplicationFocus(bool focused)
    {
        ignoreLookFrames = 2;
    }

    void LateUpdate()
    {
        if (Time.timeScale == 0f || playerTransform == null)
            return;

        if (previousAction != null && previousAction.WasPressedThisFrame())
            shoulderSide = -1f;

        if (nextAction != null && nextAction.WasPressedThisFrame())
            shoulderSide = 1f;

        if (playerController != null && playerController.IsPrecisionAiming)
            UpdateAim();
        else
            FollowPlayer();

        cameraPitch = Mathf.Clamp(cameraPitch, -20f, 60f);

        Quaternion cameraRotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);

        currentShoulderDistance = Mathf.Lerp(
            currentShoulderDistance,
            shoulderDistance * shoulderSide,
            shoulderSpeed * Time.deltaTime
        );

        Vector3 shoulderOffset = cameraRotation * Vector3.right * currentShoulderDistance;
        Vector3 cameraPosition = playerTransform.position - cameraRotation * Vector3.forward * distanceFromPlayer;

        cameraPosition.y += playerHeight;
        cameraPosition += shoulderOffset;

        transform.position = cameraPosition;
        transform.rotation = cameraRotation;
    }

    void FollowPlayer()
    {
        cameraYaw = Mathf.LerpAngle(cameraYaw, playerTransform.eulerAngles.y, followSpeed * Time.deltaTime);
        cameraPitch = Mathf.Lerp(cameraPitch, normalPitch, followSpeed * Time.deltaTime);
    }

    void UpdateAim()
    {
        Vector2 aimInput;

        if (Application.isMobilePlatform)
        {
            aimInput = playerController.AimInput;
            cameraYaw += aimInput.x * controllerSensitivity * Time.deltaTime;
            cameraPitch -= aimInput.y * controllerSensitivity * Time.deltaTime;
        }
        else
        {
            aimInput = lookAction.action.ReadValue<Vector2>();

            if (ignoreLookFrames > 0)
            {
                aimInput = Vector2.zero;
                ignoreLookFrames--;
            }

            bool usingGamepad = lookAction.action.activeControl != null &&
                                lookAction.action.activeControl.device is Gamepad;

            float sensitivity = usingGamepad
                ? controllerSensitivity * Time.deltaTime
                : mouseSensitivity / 60f;

            float verticalDirection = PlayerPrefs.GetInt("InvertY", 0) == 1 ? 1f : -1f;

            cameraYaw += aimInput.x * sensitivity;
            cameraPitch += aimInput.y * sensitivity * verticalDirection;
        }

        cameraYaw = Mathf.Repeat(cameraYaw, 360f);
    }
}
