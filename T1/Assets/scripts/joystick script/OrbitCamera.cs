//OrbitCamera
using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    public Transform target;          // The object to orbit around
    public float rotationSpeed = 100f;
    public float smoothReturnSpeed = 3f;
    public Joystick joystick;         // Reference to your joystick

    [Header("Joystick thresholds")]
    [Tooltip("Joystick magnitude <= deadzone => treat as centered (return to original).")]
    public float deadzone = 0.10f;
    [Tooltip("Joystick magnitude >= fullExtendThreshold => allow rotation. Between deadzone and this value => hold (no rotate).")]
    public float fullExtendThreshold = 0.95f;

    private Vector3 originalPositionOffset;
    private Quaternion originalRotation;
    private bool isReturning = false;

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

        // read joystick safely (if joystick not assigned, treat as zero)
        float horizontal = joystick != null ? joystick.Horizontal : 0f;
        float vertical = joystick != null ? joystick.Vertical : 0f;

        float mag = new Vector2(horizontal, vertical).magnitude;

        // 1) If joystick is centered -> begin returning
        if (mag <= deadzone)
        {
            if (!isReturning) isReturning = true;
        }
        else
        {
            // not centered -> do not return
            isReturning = false;
        }

        // 2) If fully extended -> allow rotation
        if (mag >= fullExtendThreshold)
        {
            // Rotate only while fully extended
            // Note: you can tweak rotationSpeed to taste
            transform.RotateAround(target.position, Vector3.up, horizontal * rotationSpeed * Time.deltaTime);
            transform.RotateAround(target.position, transform.right, -vertical * rotationSpeed * Time.deltaTime);
        }
        // else if deadzone < mag < fullExtendThreshold -> DO NOTHING (hold current transform)
        // This is intentional: we want the camera to "freeze" so player can inspect / stop at any angle

        // 3) If we should return to original, smoothly interpolate back
        if (isReturning)
        {
            transform.position = Vector3.Lerp(transform.position, target.position + originalPositionOffset, Time.deltaTime * smoothReturnSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, originalRotation, Time.deltaTime * smoothReturnSpeed);
        }
    }
}
