using UnityEngine;

public class RowShifter : MonoBehaviour
{
    public SwipeInput swipeInput;
    public SliderPedestalController1 sliderValueChanger;

    void Awake()
    {
        if (swipeInput == null) swipeInput = FindFirstObjectByType<SwipeInput>();
        if (sliderValueChanger == null) sliderValueChanger = FindFirstObjectByType<SliderPedestalController1>();
    }

    // Subscribe to the event when this object is enabled
    void OnEnable()
    {
        if (swipeInput != null && sliderValueChanger != null)
        {
            // swipeInput.OnSwipe += CheckPlaneAngles;
            // swipeInput.OnSwipe += CheckWhichLocalPlaneIsYZ;

            //sliderValueChanger.onSlide += CheckWhichPlanesAre45;
        }
    }

    // Unsubscribe when disabled (important to prevent errors)
    void OnDisable()
    {
        if (swipeInput != null && sliderValueChanger != null)
        {
            // swipeInput.OnSwipe -= CheckPlaneAngles;
            // swipeInput.OnSwipe -= CheckWhichLocalPlaneIsYZ;
            //sliderValueChanger.onSlide -= CheckWhichPlanesAre45;
        }
    }

    





















    // This method runs automatically whenever SwipeInput fires "OnSwipe"
    // void CheckPlaneAngles()
    // {
    //     // The Normal of the Global XY Plane is Global Forward (Z)
    //     Vector3 globalNormal = Vector3.forward;

    //     // 1. Check if Local XY Plane is the one aligned
    //     // The Normal of Local XY is Local Forward (Z)
    //     if (Mathf.Abs(Vector3.Dot(transform.forward, globalNormal)) > 0.99f)
    //     {
    //         Debug.Log("Aligned Plane with global XY: Local XY (Axes: Right & Up)");
    //     }

    //     // 2. Check if Local YZ Plane is the one aligned
    //     // The Normal of Local YZ is Local Right (X)
    //     else if (Mathf.Abs(Vector3.Dot(transform.right, globalNormal)) > 0.99f)
    //     {
    //         Debug.Log("Aligned Plane with global XY: Local YZ (Axes: Up & Forward)");
    //     }

    //     // 3. Check if Local ZX Plane is the one aligned
    //     // The Normal of Local ZX is Local Up (Y)
    //     else if (Mathf.Abs(Vector3.Dot(transform.up, globalNormal)) > 0.99f)
    //     {
    //         Debug.Log("Aligned Plane with global XY: Local ZX (Axes: Right & Forward)");
    //     }
    // }

    // void CheckWhichLocalPlaneIsYZ()
    // {
    //     // The Normal of the Global YZ Plane is Global Right (X)
    //     Vector3 globalNormal = Vector3.right;

    //     // 1. Check if Local YZ Plane is the one aligned
    //     // The Normal of Local YZ is Local Right (X)
    //     if (Mathf.Abs(Vector3.Dot(transform.right, globalNormal)) > 0.99f)
    //     {
    //         Debug.Log("Aligned with Global YZ: Local YZ Plane (Axes: Up & Forward)");
    //     }

    //     // 2. Check if Local ZX Plane is the one aligned
    //     // The Normal of Local ZX is Local Up (Y)
    //     else if (Mathf.Abs(Vector3.Dot(transform.up, globalNormal)) > 0.99f)
    //     {
    //         Debug.Log("Aligned with Global YZ: Local ZX Plane (Axes: Right & Forward)");
    //     }

    //     // 3. Check if Local XY Plane is the one aligned
    //     // The Normal of Local XY is Local Forward (Z)
    //     else if (Mathf.Abs(Vector3.Dot(transform.forward, globalNormal)) > 0.99f)
    //     {
    //         Debug.Log("Aligned with Global YZ: Local XY Plane (Axes: Right & Up)");
    //     }
    // }
    void CheckWhichPlanesAre45()
    {
        // 1. Reference: Global YZ Plane (Normal is Global X)
        Vector3 globalYZNormal = Vector3.right;
        Vector3 rotationAxis = Vector3.forward;
        //float tolerance = 1.0f;

        void IdentifyPlane(Vector3 localNormal, string planeName)
        {
            // FILTER: Skip the face pointing at the camera
            // if (Mathf.Abs(Vector3.Dot(localNormal, rotationAxis)) > 0.9f) return;

            // // CALCULATE: Angle relative to Global X
            // float angle = Vector3.SignedAngle(globalYZNormal, localNormal, rotationAxis);

            // CHECK ALL 4 DIAGONALS (45, -45, 135, -135)

            // Case 1: 45 degrees
            // if (Mathf.Abs(angle - 45f) <= tolerance)
            //     Debug.Log($"[+45°] {planeName} is tilted +45°.");

            // // Case 2: -45 degrees
            // else if (Mathf.Abs(angle - (-45f)) <= tolerance)
            //     Debug.Log($"[-45°] {planeName} is tilted -45°.");

            // // Case 3: 135 degrees (Same tilt, opposite facing)
            // else if (Mathf.Abs(angle - 135f) <= tolerance)
            //     Debug.Log($"[+135°] {planeName} is tilted 135° (equivalent to -45°).");

            // // Case 4: -135 degrees
            // else if (Mathf.Abs(angle - (-135f)) <= tolerance)
            //     Debug.Log($"[-135°] {planeName} is tilted -135° (equivalent to +45°).");

            



        }

        IdentifyPlane(transform.right, "Local YZ Plane");
        IdentifyPlane(transform.up, "Local ZX Plane");
        IdentifyPlane(transform.forward, "Local XY Plane");
    }
}




// Condition: Local XY is Parallel to Global XY (Locked on Z) AND Local YZ is Tilted (Rotated on Z)
            // if ((Mathf.Abs(Vector3.Dot(transform.forward, Vector3.forward)) > 0.99f) &&
            //      Mathf.Abs(Vector3.Dot(transform.right, Vector3.right)) > 0.01f && Mathf.Abs(Vector3.Dot(transform.right, Vector3.right)) < 0.99f)
            // {
            //     Debug.Log("local XY is flat against XY Plane, but rotated (Tilted)!");
            // }

            // // Condition: Local YZ is Parallel to Global XY (Locked on Z) AND the other axes are Tilted
            // else if ((Mathf.Abs(Vector3.Dot(transform.right, Vector3.forward)) > 0.99f) &&
            //      (Mathf.Abs(Vector3.Dot(transform.up, Vector3.up)) > 0.01f && Mathf.Abs(Vector3.Dot(transform.up, Vector3.up)) < 0.99f))
            // {
            //     Debug.Log("Local YZ Plane is flat against Global XY, but rotated (Tilted)!");
            // }
            // /////////
            // // Condition: Local XZ Plane is Parallel to Global XY (Locked on Z) AND the other axes are Tilted
            // else if ((Mathf.Abs(Vector3.Dot(transform.up, Vector3.forward)) > 0.99f) &&
            //      (Mathf.Abs(Vector3.Dot(transform.right, Vector3.right)) > 0.01f && Mathf.Abs(Vector3.Dot(transform.right, Vector3.right)) < 0.99f))
            // {
            //     Debug.Log("Local XZ Plane is flat against Global XY, but rotated (Tilted)!");
            // }
