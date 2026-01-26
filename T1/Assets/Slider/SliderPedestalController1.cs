using UnityEngine;
using UnityEngine.UI;
using System.Collections; 

public class SliderPedestalController1 : MonoBehaviour
{
    public Slider slider;
    public Transform pedestal;
    public int snapPoints = 9; // Creates 45-degree intervals

    private float currentZAngle = 0f;
    private float previousValue;
    private bool isReverting = false; // Flag to prevent infinite loops

    public SwipeInput swipeInput;
    public TMovement tMovement; // Reference to the collision checker

    void Awake()
    {
        if (swipeInput == null) swipeInput = FindFirstObjectByType<SwipeInput>(); 
        if (tMovement == null) tMovement = FindFirstObjectByType<TMovement>();
    }

    void Start()
    {
        slider.onValueChanged.AddListener(OnSliderValueChanged);
        previousValue = slider.value;
        // Initial setup (false = don't check collision on game start)
        SnapAndRotate(slider.value, false); 
    }

    void OnSliderValueChanged(float value)
    {
        // If we are currently reverting via code, ignore this event
        if (isReverting) return;

        // --- 1. STEP RESTRICTION LOGIC ---
        // Calculate snap interval
        float snapInterval = 1f / (snapPoints - 1);
        
        // Determine which "Index" (0 to 8) the slider was at vs where it is now
        int previousStep = Mathf.RoundToInt(previousValue / snapInterval);
        int currentStep = Mathf.RoundToInt(value / snapInterval);

        // Calculate how many steps the user tried to jump
        int stepDifference = Mathf.Abs(currentStep - previousStep);

        // If the jump is greater than 1, we restrict it
        if (stepDifference > 1)
        {
            // Determine direction: +1 (Right) or -1 (Left)
            int direction = currentStep > previousStep ? 1 : -1;

            // Calculate the allowed index (Neighbor of previous)
            int allowedStep = previousStep + direction;
            
            // Convert back to slider float value (0.0 to 1.0)
            float restrictedValue = allowedStep * snapInterval;

            // Override the value to process
            value = restrictedValue;

            // Visually force the slider UI to stick to the 1-step limit
            // We use the Coroutine to override the user's mouse drag input
            StartCoroutine(RevertSliderVisual(restrictedValue));
        }

        // --- 2. ROTATION & COLLISION LOGIC ---
        // Attempt to rotate to the (possibly restricted) value
        SnapAndRotate(value, true);
    }

    void SnapAndRotate(float rawValue, bool checkForCollisions)
    {
        float snapInterval = 1f / (snapPoints - 1);
        int nearestStep = Mathf.RoundToInt(rawValue / snapInterval);
        float snappedValue = nearestStep * snapInterval;

        // Calculate the Target Angle
        float targetAngleZ = snappedValue * 360f;
        float deltaZ = targetAngleZ - currentZAngle;

        // Optimization: If the angle hasn't changed significantly, do nothing
        if (Mathf.Abs(deltaZ) < 0.01f) return;

        // STORE PREVIOUS STATE
        Quaternion originalRotation = pedestal.rotation;
        float originalZAngle = currentZAngle;

        // APPLY ROTATION
        pedestal.Rotate(Vector3.forward, -deltaZ, Space.World);
        currentZAngle = targetAngleZ;

        // CHECK FOR COLLISION
        if (checkForCollisions && tMovement != null)
        {
            Physics.SyncTransforms(); 

            if (tMovement.IsRotationColliding())
            {
                Debug.Log("Slider Collision Detected! Reverting...");

                // A. Revert Physical Rotation immediately
                pedestal.rotation = originalRotation;
                currentZAngle = originalZAngle;

                // B. Revert Slider UI to PREVIOUS valid value
                StartCoroutine(RevertSliderVisual(previousValue));

                return; // Stop here.
            }
        }

        // SUCCESS (No Collision)
        
        // Update previous value to this new valid position
        previousValue = snappedValue;
        
        // Visually snap the slider handle to the exact step
        if (slider.value != snappedValue)
        {
            StartCoroutine(RevertSliderVisual(snappedValue));
        }

        // Handle Swipe Input (Enable at 90s, Disable at 45s)
        HandleSwipeInputState(targetAngleZ);
    }

    // Helper Coroutine to force the slider value to change without triggering logic loops
    IEnumerator RevertSliderVisual(float targetValue)
    {
        isReverting = true;
        yield return new WaitForEndOfFrame(); 
        slider.value = targetValue;
        isReverting = false;
    }

    void HandleSwipeInputState(float angleZ)
    {
        if (swipeInput != null)
        {
            float remainder = angleZ % 90f;
            if (Mathf.Abs(remainder) < 1f || Mathf.Abs(remainder - 90f) < 1f)
            {
                if (!swipeInput.enabled) swipeInput.enabled = true;
            }
            else if (Mathf.Abs(remainder - 45f) < 1f)
            {
                if (swipeInput.enabled) swipeInput.enabled = false;
            }
        }
    }
}