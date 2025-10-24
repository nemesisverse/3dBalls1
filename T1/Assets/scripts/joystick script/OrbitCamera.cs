//OrbitCamera
// OrbitCamera.cs
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class OrbitCamera : MonoBehaviour
{
    public Transform target;          // The object to orbit around
    public float rotationSpeed = 100f;
    public float smoothReturnSpeed = 3f;
    public Joystick joystick;         // Reference to your joystick

    [Header("Joystick thresholds")]
    public float fullExtendThreshold = 0.95f;  // Rotate only when joystick is almost fully pushed
    public float releaseThreshold = 0.05f;     // Magnitude threshold considered "centered"
    [Tooltip("Time (seconds) to wait after pointer up before actually starting return. Prevents jitter.")]
    public float releaseDelay = 0.06f;         // small debounce to avoid jitter

    private Vector3 originalPositionOffset;
    private Quaternion originalRotation;

    // state
    private bool isReturning = false;
    private bool joystickHeld = false;         // true while finger/mouse is down (or you call OnJoystickPointerDown)
    private bool manualPointerPressed = false; // set by manual hooks if your joystick provides pointer events
    private float releaseTimer = 0f;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("Target not assigned!");
            return;
        }

        // Save original offset and rotation
        originalPositionOffset = transform.position - target.position;
        originalRotation = transform.rotation;
    }

    void Update()
    {
        if (target == null) return;

        float horizontal = joystick != null ? joystick.Horizontal : 0f;
        float vertical = joystick != null ? joystick.Vertical : 0f;
        float mag = new Vector2(horizontal, vertical).magnitude;

        bool pointerDown = manualPointerPressed || IsPointerDownViaInputAPIs();

        // If pointer is down -> consider joystick held (regardless of magnitude)
        if (pointerDown)
        {
            joystickHeld = true;
            isReturning = false;
            releaseTimer = 0f;
        }
        else
        {
            if (joystickHeld)
            {
                releaseTimer += Time.deltaTime;

                if (releaseTimer >= releaseDelay && mag <= releaseThreshold)
                {
                    joystickHeld = false;
                    isReturning = true;
                }
                else if (releaseTimer >= releaseDelay && mag > releaseThreshold)
                {
                    joystickHeld = false;
                    isReturning = false;
                }
            }
        }

        // ✅ Flip directions here:
        horizontal = -horizontal; // invert left/right
        vertical = -vertical;     // invert up/down

        // Rotate only when fully extended and pointer is held
        if (mag >= fullExtendThreshold && joystickHeld)
        {
            transform.RotateAround(target.position, Vector3.up, horizontal * rotationSpeed * Time.deltaTime);
            transform.RotateAround(target.position, transform.right, -vertical * rotationSpeed * Time.deltaTime);
        }

        // Smoothly return when released (pointer up and centered)
        if (isReturning)
        {
            transform.position = Vector3.Lerp(transform.position, target.position + originalPositionOffset, Time.deltaTime * smoothReturnSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, originalRotation, Time.deltaTime * smoothReturnSpeed);

            if (pointerDown)
                isReturning = false;
        }
    }

    public void OnJoystickPointerDown() => manualPointerPressed = true;
    public void OnJoystickPointerUp() => manualPointerPressed = false;

    private bool IsPointerDownViaInputAPIs()
    {
#if ENABLE_INPUT_SYSTEM
        bool touchDown = false;
        if (Touchscreen.current != null)
        {
            touchDown = Touchscreen.current.primaryTouch.press.isPressed;
            if (!touchDown)
            {
                var touches = Touchscreen.current.touches;
                for (int i = 0; i < touches.Count; i++)
                {
                    if (touches[i].press.isPressed) { touchDown = true; break; }
                }
            }
        }

        bool mouseDown = false;
        if (Mouse.current != null)
            mouseDown = Mouse.current.leftButton.isPressed;

        return touchDown || mouseDown;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.touchCount > 0 || Input.GetMouseButton(0);
#else
        return false;
#endif
    }
}
