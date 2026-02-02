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
            swipeInput.OnSwipe += CheckWhichLocalPlaneIsYZ;
        }
    }

    // Unsubscribe when disabled (important to prevent errors)
    void OnDisable()
    {
        if (swipeInput != null)
        {
            swipeInput.OnSwipe -= CheckPlaneAngles;
            swipeInput.OnSwipe -= CheckWhichLocalPlaneIsYZ;
        }
    }

    // This method runs automatically whenever SwipeInput fires "OnSwipe"
void CheckPlaneAngles()
{
    // The Normal of the Global XY Plane is Global Forward (Z)
    Vector3 globalNormal = Vector3.forward;

    // 1. Check if Local XY Plane is the one aligned
    // The Normal of Local XY is Local Forward (Z)
    if (Mathf.Abs(Vector3.Dot(transform.forward, globalNormal)) > 0.99f)
    {
        Debug.Log("Aligned Plane with global XY: Local XY (Axes: Right & Up)");
    }

    // 2. Check if Local YZ Plane is the one aligned
    // The Normal of Local YZ is Local Right (X)
    else if (Mathf.Abs(Vector3.Dot(transform.right, globalNormal)) > 0.99f)
    {
        Debug.Log("Aligned Plane with global XY: Local YZ (Axes: Up & Forward)");
    }

    // 3. Check if Local ZX Plane is the one aligned
    // The Normal of Local ZX is Local Up (Y)
    else if (Mathf.Abs(Vector3.Dot(transform.up, globalNormal)) > 0.99f)
    {
        Debug.Log("Aligned Plane with global XY: Local ZX (Axes: Right & Forward)");
    }
}

void CheckWhichLocalPlaneIsYZ()
{
    // The Normal of the Global YZ Plane is Global Right (X)
    Vector3 globalNormal = Vector3.right;

    // 1. Check if Local YZ Plane is the one aligned
    // The Normal of Local YZ is Local Right (X)
    if (Mathf.Abs(Vector3.Dot(transform.right, globalNormal)) > 0.99f)
    {
        Debug.Log("Aligned with Global YZ: Local YZ Plane (Axes: Up & Forward)");
    }

    // 2. Check if Local ZX Plane is the one aligned
    // The Normal of Local ZX is Local Up (Y)
    else if (Mathf.Abs(Vector3.Dot(transform.up, globalNormal)) > 0.99f)
    {
        Debug.Log("Aligned with Global YZ: Local ZX Plane (Axes: Right & Forward)");
    }

    // 3. Check if Local XY Plane is the one aligned
    // The Normal of Local XY is Local Forward (Z)
    else if (Mathf.Abs(Vector3.Dot(transform.forward, globalNormal)) > 0.99f)
    {
        Debug.Log("Aligned with Global YZ: Local XY Plane (Axes: Right & Up)");
    }
}
}