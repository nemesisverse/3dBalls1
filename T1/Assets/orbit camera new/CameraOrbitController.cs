using UnityEngine;

public class CameraOrbitController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Joystick joystick;   // your Fixed Joystick
    [SerializeField] private Transform target;     // the currently selected sphere
    [SerializeField] private Camera targetCamera;  // leave empty to use Camera.main

    [Header("Orbit speed (deg/sec at full deflection)")]
    [SerializeField] private float horizontalSpeed = 120f;
    [SerializeField] private float verticalSpeed = 80f;

    [Header("Edge gate")]
    // Joystick Pack clamps Horizontal/Vertical to a unit circle, so full push
    // gives magnitude ~1.0. Camera only orbits when deflection >= this value —
    // i.e. the stick is at the outer edge/corner. Anything inside → no movement.
    [SerializeField, Range(0.5f, 1f)] private float edgeThreshold = 0.95f;

    [Header("Vertical limits (per hold)")]
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;
    [SerializeField] private float deadZone = 0.05f;

    private Transform cam;
    private Vector3 homePosition;      // captured ONCE — the original initial pose
    private Quaternion homeRotation;
    private bool wasActive;
    private float pitch;

    public void SetTarget(Transform newTarget) => target = newTarget;

    void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        cam = targetCamera.transform;

        // Original initial position/rotation — release always snaps back here.
        homePosition = cam.position;
        homeRotation = cam.rotation;
    }

    void LateUpdate()
    {
        if (target == null) return;

        bool active = JoystickTouchGate.Active;

        if (active)
        {
            float h = joystick.Horizontal;
            float v = joystick.Vertical;

            // EDGE GATE: stick must be at the outer boundary to orbit.
            // Inside the boundary → skip movement → camera holds current angle.
            float deflection = new Vector2(h, v).magnitude;
            if (deflection >= edgeThreshold)
            {
                // Horizontal orbit around world up.
                if (Mathf.Abs(h) > deadZone)
                    cam.RotateAround(target.position, Vector3.up,
                        -h * horizontalSpeed * Time.unscaledDeltaTime);

                // Vertical orbit around the camera's current right axis, clamped.
                if (Mathf.Abs(v) > deadZone)
                {
                    float delta = v * verticalSpeed * Time.unscaledDeltaTime;
                    float clamped = Mathf.Clamp(pitch + delta, minPitch, maxPitch);
                    delta = clamped - pitch;
                    pitch = clamped;
                    cam.RotateAround(target.position, cam.right, delta);
                }
            }
        }

        // Released: snap back instantly to the original initial pose. No lerp.
        if (!active && wasActive)
        {
            cam.position = homePosition;
            cam.rotation = homeRotation;
            pitch = 0f; // reset tilt budget for the next hold
        }

        wasActive = active;
    }
}