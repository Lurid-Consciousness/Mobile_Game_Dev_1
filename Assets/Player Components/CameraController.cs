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


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        currentShoulderDistance = shoulderDistance;

    }

    private void OnEnable()
    {
        previousAction = lookAction.action.actionMap.FindAction("Previous");
        nextAction = lookAction.action.actionMap.FindAction("Next");

        lookAction.action.Enable();
        previousAction.Enable();
        nextAction.Enable();
    }

    private void OnDisable()
    {
        lookAction.action.Disable();
        previousAction.Disable();
        nextAction.Disable();
    }

    // Update is called once per frame
    void LateUpdate()
    {

        Vector2 lookInput = lookAction.action.ReadValue<Vector2>();

        if (previousAction.WasPressedThisFrame())
            shoulderSide = -1f;

        if (nextAction.WasPressedThisFrame())
            shoulderSide = 1f;

        float currentSensitivity = lookAction.action.activeControl != null && lookAction.action.activeControl.device is Gamepad ? controllerSensitivity : mouseSensitivity;

        mouseX += lookInput.x * currentSensitivity * Time.deltaTime;
        mouseY -= lookInput.y * currentSensitivity * Time.deltaTime;

        mouseY = Mathf.Clamp(mouseY, -20f, 60f);

        Quaternion cameraRotation = Quaternion.Euler(mouseY, mouseX, 0f);
        currentShoulderDistance = Mathf.Lerp(currentShoulderDistance, shoulderDistance * shoulderSide, shoulderSpeed * Time.deltaTime);

        Vector3 shoulderOffset = cameraRotation * Vector3.right * currentShoulderDistance;
        Vector3 cameraPosition = playerTransform.position - cameraRotation * Vector3.forward * distanceFromPlayer;

        cameraPosition.y += playerHeight;
        cameraPosition += shoulderOffset;

        transform.position = cameraPosition;
        transform.LookAt(playerTransform.position + Vector3.up + shoulderOffset);

    }
}
