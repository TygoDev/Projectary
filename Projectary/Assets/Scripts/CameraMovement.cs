using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    [Header("Zoom")]
    [SerializeField] private float speed = 0.01f;
    [SerializeField] private float minFov = 10f;
    [SerializeField] private float maxFov = 70f;
    [SerializeField] private float pinchMultiplier = 10f;

    [Header("Pan")]
    [SerializeField] private float panSpeed = 0.01f; // higher values -> faster panning

    // Touch input actions
    private InputAction touch0Contact;
    private InputAction touch1Contact;
    private InputAction touch0Pos;
    private InputAction touch1Pos;

    // Camera reference
    private Camera cam;

    // Pinch tracking
    private float previousPinchDistance = 0f;
    private bool previousPinchDistanceInitialized;

    // Pan tracking
    private Vector2 previousTouchPosition;
    private bool hasPreviousTouchPosition;

    // Touch state flags
    private bool touch0Active;
    private bool touch1Active;

    private const string Touch0ContactBinding = "<Touchscreen>/touch0/press";
    private const string Touch1ContactBinding = "<Touchscreen>/touch1/press";
    private const string Touch0PosBinding = "<Touchscreen>/touch0/position";
    private const string Touch1PosBinding = "<Touchscreen>/touch1/position";

    private void Awake()
    {
        cam = Camera.main ?? GetComponent<Camera>();

        touch0Contact = CreateAction("Touch0Contact", Touch0ContactBinding);
        touch1Contact = CreateAction("Touch1Contact", Touch1ContactBinding);

        touch0Pos = CreateAction("Touch0Pos", Touch0PosBinding);
        touch1Pos = CreateAction("Touch1Pos", Touch1PosBinding);
    }

    private void OnEnable()
    {
        EnableAndSubscribe(touch0Contact, onStarted: OnTouch0Started, onCanceled: OnTouch0Canceled);
        EnableAndSubscribe(touch1Contact, onStarted: OnTouch1Started, onCanceled: OnTouch1Canceled);

        EnableAndSubscribe(touch0Pos, onPerformed: OnTouchPositionChanged);
        EnableAndSubscribe(touch1Pos, onPerformed: OnTouchPositionChanged);
    }

    private void OnDisable()
    {
        UnsubscribeAndDisable(touch0Contact, OnTouch0Started, OnTouch0Canceled);
        UnsubscribeAndDisable(touch1Contact, OnTouch1Started, OnTouch1Canceled);

        UnsubscribeAndDisable(touch0Pos, OnTouchPositionChanged, null);
        UnsubscribeAndDisable(touch1Pos, OnTouchPositionChanged, null);
    }

    private InputAction CreateAction(string name, string binding)
    {
        return new InputAction(name, binding: binding);
    }

    private void EnableAndSubscribe(InputAction action, Action<InputAction.CallbackContext> onStarted = null, Action<InputAction.CallbackContext> onPerformed = null, Action<InputAction.CallbackContext> onCanceled = null)
    {
        if (action == null) return;

        if (onStarted != null) action.started += onStarted;
        if (onPerformed != null) action.performed += onPerformed;
        if (onCanceled != null) action.canceled += onCanceled;

        action.Enable();
    }

    private void UnsubscribeAndDisable(InputAction action, Action<InputAction.CallbackContext> startedHandler, Action<InputAction.CallbackContext> canceledHandler)
    {
        if (action == null) return;

        if (startedHandler != null) action.started -= startedHandler;
        if (canceledHandler != null) action.canceled -= canceledHandler;

        // some actions only had performed subscribed (position actions)
        action.performed -= OnTouchPositionChanged;

        action.Disable();
        action.Dispose();
    }

    // Touch contact handlers
    private void OnTouch0Started(InputAction.CallbackContext _)
    {
        touch0Active = true;

        if (!touch1Active)
        {
            // Start panning with touch0
            isPanStartFromCurrentTouch(touch0Pos);
        }
        else
        {
            // Two touches: prepare for pinch
            ResetPanState();
        }

        // reset pinch init so pinch starts measuring fresh when second finger arrives
        previousPinchDistanceInitialized = false;
    }

    private void OnTouch1Started(InputAction.CallbackContext _)
    {
        touch1Active = true;
        // Any time a second finger touches, switch to pinch mode.
        ResetPanState();
        previousPinchDistanceInitialized = false;
    }

    private void OnTouch0Canceled(InputAction.CallbackContext _)
    {
        touch0Active = false;
        ResetPinchState();
        ResetPanState();
    }

    private void OnTouch1Canceled(InputAction.CallbackContext _)
    {
        touch1Active = false;
        ResetPinchState();

        // If touch0 remains, resume panning from its current position
        if (touch0Active)
            isPanStartFromCurrentTouch(touch0Pos);
        else
            ResetPanState();
    }

    private void isPanStartFromCurrentTouch(InputAction positionAction)
    {
        if (positionAction == null) return;
        previousTouchPosition = positionAction.ReadValue<Vector2>();
        hasPreviousTouchPosition = true;
    }

    private void ResetPanState()
    {
        previousTouchPosition = Vector2.zero;
        hasPreviousTouchPosition = false;
    }

    private void ResetPinchState()
    {
        previousPinchDistance = 0f;
        previousPinchDistanceInitialized = false;
    }

    // Called when either touch position action changes
    private void OnTouchPositionChanged(InputAction.CallbackContext _)
    {
        if (cam == null) return;

        if (touch0Active && touch1Active)
        {
            HandlePinch();
            return;
        }

        // Single-touch panning (touch0 used as pan finger)
        if (touch0Active && !touch1Active && hasPreviousTouchPosition)
        {
            HandlePan();
        }
    }

    private void HandlePinch()
    {
        if (touch0Pos == null || touch1Pos == null) return;

        Vector2 p0 = touch0Pos.ReadValue<Vector2>();
        Vector2 p1 = touch1Pos.ReadValue<Vector2>();

        float currentDistance = Vector2.Distance(p0, p1);

        if (!previousPinchDistanceInitialized)
        {
            previousPinchDistance = currentDistance;
            previousPinchDistanceInitialized = true;
            return;
        }

        float delta = currentDistance - previousPinchDistance;
        previousPinchDistance = currentDistance;

        // Zoom: larger pinch (positive delta) -> pinch out -> zoom out (increase FOV)
        // Invert delta so pinch together reduces FOV and apart increases it, matching original behaviour
        float newFov = cam.fieldOfView - delta * speed * pinchMultiplier;
        cam.fieldOfView = Mathf.Clamp(newFov, minFov, maxFov);
    }

    private void HandlePan()
    {
        if (touch0Pos == null) return;

        Vector2 current = touch0Pos.ReadValue<Vector2>();
        Vector2 delta = current - previousTouchPosition;

        // Update for next frame
        previousTouchPosition = current;

        // Convert to viewport delta so pan is resolution independent
        Vector2 viewportDelta = new Vector2(delta.x / (float)Screen.width, delta.y / (float)Screen.height);

        // Scale pan by field of view to keep perceived speed roughly consistent
        float fovScale = cam.fieldOfView / 50f;

        // Invert so dragging moves content in the drag direction
        Vector3 movement = new Vector3(-viewportDelta.x * panSpeed * fovScale, -viewportDelta.y * panSpeed * fovScale, 0f);
        cam.transform.position += movement;
    }

    // Ensure we clean up if the object is destroyed while still enabled
    private void OnDestroy()
    {
        // OnDisable already disposes actions, but guard in case OnDisable wasn't invoked
        try
        {
            if (touch0Contact != null) { touch0Contact.Disable(); touch0Contact.Dispose(); }
            if (touch1Contact != null) { touch1Contact.Disable(); touch1Contact.Dispose(); }
            if (touch0Pos != null) { touch0Pos.Disable(); touch0Pos.Dispose(); }
            if (touch1Pos != null) { touch1Pos.Disable(); touch1Pos.Dispose(); }
        }
        catch { /* swallow - safe cleanup */ }
    }
}