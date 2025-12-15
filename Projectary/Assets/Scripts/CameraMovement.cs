using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private float speed = 0.01f;
    [SerializeField] private float minFov = 10f;
    [SerializeField] private float maxFov = 70f;
    [SerializeField] private float pinchMultiplier = 10f;
    [SerializeField] private float panSpeed = 0.01f; // higher values -> faster panning

    private InputAction touch0Contact;
    private InputAction touch1Contact;
    private InputAction touch0Pos;
    private InputAction touch1Pos;

    private Camera cam;
    private float previousMagnitude = 0f;
    private bool touch0Active = false;
    private bool touch1Active = false;

    private bool isPanning = false;
    private Vector2 previousTouchPosition = Vector2.zero;

    private void Awake()
    {
        cam = Camera.main;

        touch0Contact = new InputAction("Touch0Contact", binding: "<Touchscreen>/touch0/press");
        touch1Contact = new InputAction("Touch1Contact", binding: "<Touchscreen>/touch1/press");

        touch0Pos = new InputAction("Touch0Pos", binding: "<Touchscreen>/touch0/position");
        touch1Pos = new InputAction("Touch1Pos", binding: "<Touchscreen>/touch1/position");
    }

    private void OnEnable()
    {
        touch0Contact.Enable();
        touch1Contact.Enable();
        touch0Contact.performed += OnTouch0Pressed;
        touch1Contact.performed += OnTouch1Pressed;
        touch0Contact.canceled += OnTouch0Canceled;
        touch1Contact.canceled += OnTouch1Canceled;

        touch0Pos.Enable();
        touch1Pos.Enable();
        touch0Pos.performed += OnTouchPositionChanged;
        touch1Pos.performed += OnTouchPositionChanged;
    }

    private void OnDisable()
    {
        touch0Contact.performed -= OnTouch0Pressed;
        touch0Contact.canceled -= OnTouch0Canceled;
        touch0Contact.Disable();
        touch0Contact.Dispose();

        touch1Contact.performed -= OnTouch1Pressed;
        touch1Contact.canceled -= OnTouch1Canceled;
        touch1Contact.Disable();
        touch1Contact.Dispose();

        touch0Pos.performed -= OnTouchPositionChanged;
        touch0Pos.Disable();
        touch0Pos.Dispose();

        touch1Pos.performed -= OnTouchPositionChanged;
        touch1Pos.Disable();
        touch1Pos.Dispose();
    }

    private void OnTouch0Pressed(InputAction.CallbackContext _)
    {
        touch0Active = true;

        // If second touch is not active, start panning using current touch position as reference
        if (!touch1Active)
        {
            isPanning = true;
            previousTouchPosition = touch0Pos.ReadValue<Vector2>();
        }
        else
        {
            // Two touches -> disable panning and prepare for pinch
            isPanning = false;
            previousTouchPosition = Vector2.zero;
        }
    }

    private void OnTouch1Pressed(InputAction.CallbackContext _)
    {
        touch1Active = true;
        // Two touches -> disable panning and prepare for pinch
        isPanning = false;
        previousTouchPosition = Vector2.zero;
    }

    private void OnTouch0Canceled(InputAction.CallbackContext _)
    {
        touch0Active = false;
        previousMagnitude = 0f;
        isPanning = false;
        previousTouchPosition = Vector2.zero;
    }

    private void OnTouch1Canceled(InputAction.CallbackContext _)
    {
        touch1Active = false;
        previousMagnitude = 0f;

        // If touch0 is still active after lifting touch1, resume panning
        if (touch0Active)
        {
            isPanning = true;
            previousTouchPosition = touch0Pos.ReadValue<Vector2>();
        }
        else
        {
            isPanning = false;
            previousTouchPosition = Vector2.zero;
        }
    }

    private void OnTouchPositionChanged(InputAction.CallbackContext _)
    {
        if (cam == null) return;

        // If both touches active -> pinch
        if (touch0Active && touch1Active)
        {
            EvaluatePinch();
            return;
        }

        // Single-touch panning
        if (isPanning && touch0Active && !touch1Active)
        {
            Pan();
        }
    }

    private void EvaluatePinch()
    {
        Vector2 p0 = touch0Pos.ReadValue<Vector2>();
        Vector2 p1 = touch1Pos.ReadValue<Vector2>();
        float magnitude = (p0 - p1).magnitude;

        if (previousMagnitude == 0f)
            previousMagnitude = magnitude;

        float difference = magnitude - previousMagnitude;
        previousMagnitude = magnitude;

        cam.fieldOfView = Mathf.Clamp(cam.fieldOfView - difference * speed * pinchMultiplier, minFov, maxFov);
    }

    private void Pan()
    {
        Vector2 current = touch0Pos.ReadValue<Vector2>();

        if (previousTouchPosition == Vector2.zero)
        {
            previousTouchPosition = current;
            return;
        }

        Vector2 delta = current - previousTouchPosition;
        previousTouchPosition = current;

        // Convert screen delta to a camera movement in world space:
        // Use viewport delta so panning speed is resolution-independent, and scale by panSpeed.
        Vector2 viewportDelta = new Vector2(delta.x / (float)Screen.width, delta.y / (float)Screen.height);

        // Move camera in X and Y world axes. Invert so dragging finger moves the scene in the dragged direction.
        Vector3 movement = new Vector3(-viewportDelta.x * panSpeed * (cam.fieldOfView / 50f), -viewportDelta.y * panSpeed * (cam.fieldOfView / 50f), 0f);

        cam.transform.position += movement;
    }
}