using UnityEngine;

public class RowShifter : MonoBehaviour
{
    public SwipeInput swipeInput;

    void Awake()
    {
        if (swipeInput == null) swipeInput = FindFirstObjectByType<SwipeInput>();
    }

    // Subscribe to the event when this object is enabled
    void OnEnable()
    {
        if (swipeInput != null)
        {
            swipeInput.OnSwipe += CheckPlaneAngles;
        }
    }

    // Unsubscribe when disabled (important to prevent errors)
    void OnDisable()
    {
        if (swipeInput != null)
        {
            swipeInput.OnSwipe -= CheckPlaneAngles;
        }
    }

    // This method runs automatically whenever SwipeInput fires "OnSwipe"
    void CheckPlaneAngles()
{
    // --- 1. YZ Plane Check (Normal is X Axis) ---
    // Compares Global Right vs Local Right
    float angleYZ = Vector3.Angle(Vector3.right, transform.right);

    // --- 2. ZX Plane Check (Normal is Y Axis) ---
    // Compares Global Up vs Local Up
    float angleZX = Vector3.Angle(Vector3.up, transform.up);

    // --- 3. XY Plane Check (Normal is Z Axis) ---
    // Compares Global Forward vs Local Forward
    float angleXY = Vector3.Angle(Vector3.forward, transform.forward);

    Debug.Log($"Plane Angles -> YZ: {angleYZ}° | ZX: {angleZX}° | XY: {angleXY}°");
}
}