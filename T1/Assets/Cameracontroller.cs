// CameraController.cs
// Merged: block+platform framing  +  joystick orbit  +  instant snap-back on release.
//
// SETUP
//   1. Attach to Main Camera.
//   2. Assign "platform" (motherPlatform) and "joystick" in the Inspector.
//   3. Set "fixedDistance" to match your desired camera distance from the platform
//      (default 38.21 = distance derived from your Transform at (0.01, 5.62, -37.8)).
//   4. From GameManager / block spawner call:
//        cam.SetFallingBlock(block.transform);   // when block spawns
//        cam.ClearFallingBlock();                 // when block locks in
//   5. Hook OnJoystickPointerDown / OnJoystickPointerUp to your Joystick's
//      EventTrigger (PointerDown / PointerUp) if you want pointer-state tracking.

using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CameraController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Targets")]
    [Tooltip("Assign motherPlatform here.")]
    public Transform platform;

    // ─────────────────────────────────────────────────────────────────────────

    [Header("Block Framing")]
    [Tooltip("Fixed distance the camera keeps from the platform center along -Z. " +
             "Derived from your Transform (0.01, 5.62, -37.8) → |Z| ≈ 37.8.")]
    public float fixedDistance = 37.8f;

    [Tooltip("Speed at which the camera glides toward its rest position.")]
    public float positionSmoothSpeed = 5f;

    [Tooltip("Speed at which the camera rotates toward its rest orientation.")]
    public float rotationSmoothSpeed = 5f;

    // ─────────────────────────────────────────────────────────────────────────

    [Header("Joystick Orbit")]
    public Joystick joystick;

    [Tooltip("Degrees per second the camera orbits when joystick is fully extended.")]
    public float orbitSpeed = 100f;

    [Tooltip("Joystick magnitude required to start orbiting (0–1).")]
    public float fullExtendThreshold = 0.95f;

    [Tooltip("Magnitude below which joystick is considered centered.")]
    public float releaseThreshold = 0.05f;

    [Tooltip("Seconds to wait after pointer-up before confirming release. " +
             "Prevents snap from a single dropped touch frame.")]
    public float releaseDelay = 0.06f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private state
    // ─────────────────────────────────────────────────────────────────────────

    private Camera    _cam;
    private Transform _fallingBlock;

    // Orbit / joystick
    private bool  _isOrbiting           = false;
    private bool  _wasOrbiting          = false;
    private bool  _joystickHeld         = false;
    private bool  _manualPointerPressed = false;
    private float _releaseTimer         = 0f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        _cam = GetComponent<Camera>();
    }

    // LateUpdate: runs after all game objects, so block positions are final.
    void LateUpdate()
    {
        if (platform == null) return;

        // 1. Compute rest position/rotation every frame.
        ComputeHomeTransform(out Vector3 homePos, out Quaternion homeRot);

        // 2. Snapshot previous orbit state, then update.
        _wasOrbiting = _isOrbiting;
        UpdateOrbitState();

        // 3. Apply camera based on state.
        if (_isOrbiting)
        {
            // ── Orbiting: rotate freely around platform center via joystick ───
            ApplyOrbitInput();
        }
        else if (_wasOrbiting && !_isOrbiting)
        {
            // ── Snap: joystick just released → instant return to XY front ─────
            transform.position = homePos;
            transform.rotation = homeRot;
        }
        else
        {
            // ── Tracking: smoothly follow the rest position ───────────────────
            transform.position = Vector3.Lerp(
                transform.position, homePos,
                Time.deltaTime * positionSmoothSpeed);

            transform.rotation = Quaternion.Slerp(
                transform.rotation, homeRot,
                Time.deltaTime * rotationSmoothSpeed);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Home-position math
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Rest position is ALWAYS directly in front of the sphere along the -Z axis:
    ///   camera.position = platform.position + (0, 0, -fixedDistance)
    ///
    /// WHY THIS WORKS:
    ///   All block spokes (vertical, left diagonal, right diagonal) live entirely
    ///   in the XY plane (Z = 0), so a camera looking from -Z toward +Z sees both
    ///   the sphere and every falling block without needing to orbit.
    ///
    ///   The joystick orbit still allows free 3-D inspection.
    ///   Releasing the joystick always snaps back to this front-facing position.
    /// </summary>
    private void ComputeHomeTransform(out Vector3 pos, out Quaternion rot)
    {
        // ── Fixed front position: camera sits on -Z axis, looks toward +Z ────
        pos = platform.position + new Vector3(0f, 0f, -fixedDistance);

        // LookRotation: forward = platform.position - pos = (0,0,fixedDistance) = +Z
        //               up      = world Y — standard upright orientation
        rot = Quaternion.LookRotation(platform.position - pos, Vector3.up);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Orbit input
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Updates _joystickHeld (with debounce) and derives _isOrbiting.
    /// </summary>
    private void UpdateOrbitState()
    {
        float horizontal  = joystick != null ? joystick.Horizontal : 0f;
        float vertical    = joystick != null ? joystick.Vertical   : 0f;
        float mag         = new Vector2(horizontal, vertical).magnitude;
        bool  pointerDown = _manualPointerPressed || IsPointerDownViaInputAPIs();

        if (pointerDown)
        {
            // Finger on joystick: held, reset debounce timer.
            _joystickHeld = true;
            _releaseTimer = 0f;
        }
        else
        {
            // Finger lifted: wait releaseDelay before marking as released.
            if (_joystickHeld)
            {
                _releaseTimer += Time.deltaTime;

                if (_releaseTimer >= releaseDelay)
                {
                    _joystickHeld = false;
                    _releaseTimer = 0f;
                }
            }
        }

        // Orbit only while held AND joystick is pushed far enough.
        _isOrbiting = _joystickHeld && (mag >= fullExtendThreshold);
    }

    /// <summary>
    /// Applies joystick axes as RotateAround calls while in orbit mode.
    /// Camera can orbit freely in all directions — front constraint only applies on snap-back.
    /// </summary>
    private void ApplyOrbitInput()
    {
        float horizontal = joystick != null ? joystick.Horizontal : 0f;
        float vertical   = joystick != null ? joystick.Vertical   : 0f;

        // Invert to match expected swipe-feel (consistent with original OrbitCamera).
        horizontal = -horizontal;
        vertical   = -vertical;

        // Orbit around sphere center (free 3-D orbit while joystick is held).
        transform.RotateAround(platform.position, Vector3.up,        horizontal * orbitSpeed * Time.deltaTime);
        transform.RotateAround(platform.position, transform.right,  -vertical   * orbitSpeed * Time.deltaTime);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Input system detection
    // ─────────────────────────────────────────────────────────────────────────

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
        bool mouseDown = Mouse.current != null && Mouse.current.leftButton.isPressed;
        return touchDown || mouseDown;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.touchCount > 0 || Input.GetMouseButton(0);
#else
        return false;
#endif
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Register a newly spawned block. Call from GameManager on spawn.</summary>
    public void SetFallingBlock(Transform block) => _fallingBlock = block;

    /// <summary>Unregister the block when it locks into the grid.</summary>
    public void ClearFallingBlock() => _fallingBlock = null;

    /// <summary>Wire to Joystick EventTrigger → PointerDown.</summary>
    public void OnJoystickPointerDown() => _manualPointerPressed = true;

    /// <summary>Wire to Joystick EventTrigger → PointerUp.</summary>
    public void OnJoystickPointerUp() => _manualPointerPressed = false;
}