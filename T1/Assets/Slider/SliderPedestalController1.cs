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
    private int previousStepIndex;
    
    private bool isReverting = false; // Prevents infinite loops

    public SwipeInput swipeInput;
    public TMovement tMovement; 

    void Awake()
    {
        if (swipeInput == null) swipeInput = FindFirstObjectByType<SwipeInput>(); 
        if (tMovement == null) tMovement = FindFirstObjectByType<TMovement>();
    }

    void Start()
    {
        slider.onValueChanged.AddListener(OnSliderValueChanged);
        
        // Initialize State
        previousValue = slider.value;
        float snapInterval = 1f / (snapPoints - 1);
        previousStepIndex = Mathf.RoundToInt(previousValue / snapInterval);

        // Initial setup
        SnapAndRotate(slider.value, false); 
    }

    void OnSliderValueChanged(float value)
    {
        if (isReverting) return;

        float snapInterval = 1f / (snapPoints - 1);
        int currentStepIndex = Mathf.RoundToInt(value / snapInterval);

        // --- 1. ONE-STEP CONSTRAINT ---
        // If we jump more than 1 step, clamp it to the neighbor
        int stepJump = Mathf.Abs(currentStepIndex - previousStepIndex);
        if (stepJump > 1)
        {
            int direction = currentStepIndex > previousStepIndex ? 1 : -1;
            currentStepIndex = previousStepIndex + direction;
        }

        // --- 2. CALCULATE SNAPPED VALUE ---
        float snappedValue = currentStepIndex * snapInterval;

        // --- 3. VISUAL SNAP (The "No Smooth Moving" Logic) ---
        // Even if the user is dragging between steps, FORCE the handle to the snap point.
        // This makes the slider feel like it "pops" or "jumps" to the next tick.
        if (Mathf.Abs(slider.value - snappedValue) > 0.001f)
        {
            StartCoroutine(ForceSliderVisual(snappedValue));
        }

        // --- 4. PROCESS ROTATION ---
        // Only run rotation logic if we have actually changed steps
        if (currentStepIndex != previousStepIndex)
        {
            SnapAndRotate(snappedValue, true);
        }
    }

    void SnapAndRotate(float snappedValue, bool checkForCollisions)
    {
        // Calculate Target Angle
        float targetAngleZ = snappedValue * 360f;
        float deltaZ = targetAngleZ - currentZAngle;

        // --- PREPARE ---
        Quaternion originalRotation = pedestal.rotation;
        float originalZAngle = currentZAngle;
        float originalSliderValue = previousValue;
        
        // --- ACTION: ROTATE INSTANTLY ---
        pedestal.Rotate(Vector3.forward, -deltaZ, Space.World);
        currentZAngle = targetAngleZ;

        // --- CHECK COLLISION ---
        if (checkForCollisions && tMovement != null)
        {
            Physics.SyncTransforms(); 

            if (tMovement.IsRotationColliding())
            {
                Debug.Log("Collision! Reverting Slider...");

                // A. Revert Rotation
                pedestal.rotation = originalRotation;
                currentZAngle = originalZAngle;

                // B. Revert Slider UI (Force back to previous step)
                StartCoroutine(ForceSliderVisual(originalSliderValue));

                return; // Stop here. State is not updated.
            }
        }

        // --- SUCCESS ---
        // Commit the new state
        previousValue = snappedValue;
        float snapInterval = 1f / (snapPoints - 1);
        previousStepIndex = Mathf.RoundToInt(snappedValue / snapInterval);

        // Handle Swipe Input rules
        HandleSwipeInputState(targetAngleZ);
    }

    IEnumerator ForceSliderVisual(float targetValue)
    {
        isReverting = true; 
        yield return new WaitForEndOfFrame(); // Wait for Unity to finish processing Drag
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