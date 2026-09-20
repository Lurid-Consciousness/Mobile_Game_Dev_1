using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{

    public Transform playerTransform;

    public InputActionReference lookAction;

    public float mouseSensitivity = 15f;
    public float distanceFromPlayer = 5f;
    public float playerHeight = 2f;
    public float shoulderDistance = 1.2f;
    public float shoulderSpeed = 8f;
    public float controllerSensitivity = 120f;

    private float mouseX;
    private float mouseY;
    private float shoulderSide = 1f;
    private float currentShoulderDistance;
    private InputAction previousAction;
    private InputAction nextAction;
    private int ignoreLookFrames;


    void Awake()
    {
        mouseX = transform.eulerAngles.y;
        mouseY = Mathf.DeltaAngle(0f, transform.eulerAngles.x);
        currentShoulderDistance = shoulderDistance;
    }

    private void OnEnable()
    {
        previousAction = lookAction.action.actionMap.FindAction("Previous");
        nextAction = lookAction.action.actionMap.FindAction("Next");

        lookAction.action.Enable();
        previousAction.Enable();
        nextAction.Enable();
        ignoreLookFrames = 2;
    }

    private void OnDisable()
    {
        lookAction.action.Disable();
        previousAction.Disable();
        nextAction.Disable();
    }

    private void OnApplicationFocus(bool focused)
    {
        ignoreLookFrames = 2;
    }

    void LateUpdate()
    {
        if (Time.timeScale == 0f || !Application.isFocused || playerTransform == null)
            return;

        Vector2 lookInput = lookAction.action.ReadValue<Vector2>();

        if (previousAction.WasPressedThisFrame())
            shoulderSide = -1f;

        if (nextAction.WasPressedThisFrame())
            shoulderSide = 1f;

        if (ignoreLookFrames > 0)
        {
            lookInput = Vector2.zero;
            ignoreLookFrames--;
        }
        bool usingGamepad = lookAction.action.activeControl != null && lookAction.action.activeControl.device is Gamepad;
        float sensitivity = usingGamepad ? controllerSensitivity * Time.deltaTime : mouseSensitivity / 60f;
        sensitivity *= PlayerPrefs.GetFloat("LookSensitivity", 1f);
        mouseX = Mathf.Repeat(mouseX + lookInput.x * sensitivity, 360f);
        mouseY += lookInput.y * sensitivity * (PlayerPrefs.GetInt("InvertY", 0) == 1 ? 1f : -1f);

        mouseY = Mathf.Clamp(mouseY, -20f, 60f);

        Quaternion cameraRotation = Quaternion.Euler(mouseY, mouseX, 0f);
        currentShoulderDistance = Mathf.Lerp(currentShoulderDistance, shoulderDistance * shoulderSide, shoulderSpeed * Time.deltaTime);

        Vector3 shoulderOffset = cameraRotation * Vector3.right * currentShoulderDistance;
        Vector3 cameraPosition = playerTransform.position - cameraRotation * Vector3.forward * distanceFromPlayer;

        cameraPosition.y += playerHeight;
        cameraPosition += shoulderOffset;

        transform.position = cameraPosition;
        transform.rotation = cameraRotation;

    }
}
