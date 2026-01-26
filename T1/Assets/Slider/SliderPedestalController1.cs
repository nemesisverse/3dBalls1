using UnityEngine;
using UnityEngine.UI;
using System.Collections; 

public class SliderPedestalController1 : MonoBehaviour
{
    public Slider slider;
    public Transform pedestal;
    public int snapPoints = 9; 

    private float currentZAngle = 0f;
    
    // This is our "Safety Anchor". It tracks the index of the last valid step.
    private int safeStepIndex; 
    
    private bool isReverting = false; 

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
        
        // Initialize Safety Anchor
        float snapInterval = 1f / (snapPoints - 1);
        safeStepIndex = Mathf.RoundToInt(slider.value / snapInterval);
        
        // Initial setup
        SnapAndRotate(slider.value, false); 
    }

    void OnSliderValueChanged(float value)
    {
        if (isReverting) return;

        float snapInterval = 1f / (snapPoints - 1);
        int currentStepIndex = Mathf.RoundToInt(value / snapInterval);

        // --- 1. ONE-STEP CONSTRAINT ---
        // If we jump more than 1 step away from our SAFE anchor...
        int stepJump = Mathf.Abs(currentStepIndex - safeStepIndex);
        
        if (stepJump > 1)
        {
            // Determine valid neighbor
            int direction = currentStepIndex > safeStepIndex ? 1 : -1;
            int allowedStepIndex = safeStepIndex + direction;
            float allowedValue = allowedStepIndex * snapInterval;

            // Force visual slider back to the limit
            StartCoroutine(ForceSliderVisual(allowedValue));
            return; 
        }

        // --- 2. PROCESS ROTATION ---
        // Only attempt rotation if we have crossed into a new step index
        if (currentStepIndex != safeStepIndex)
        {
            // Calculate the target value for this new step
            float targetSnapValue = currentStepIndex * snapInterval;
            SnapAndRotate(targetSnapValue, true);
        }
    }

    void SnapAndRotate(float targetSnapValue, bool checkForCollisions)
    {
        float snapInterval = 1f / (snapPoints - 1);
        int targetStepIndex = Mathf.RoundToInt(targetSnapValue / snapInterval);

        float targetAngleZ = targetSnapValue * 360f;
        float deltaZ = targetAngleZ - currentZAngle;

        if (Mathf.Abs(deltaZ) < 0.01f) return;

        // --- PREPARE REVERT DATA ---
        Quaternion originalRotation = pedestal.rotation;
        float originalZAngle = currentZAngle;
        
        // Calculate the safe value based on our anchor
        float safeSliderValue = safeStepIndex * snapInterval; 

        // --- ACTION: ROTATE INSTANTLY ---
        pedestal.Rotate(Vector3.forward, -deltaZ, Space.World);
        currentZAngle = targetAngleZ;

        // --- CHECK COLLISION ---
        if (checkForCollisions && tMovement != null)
        {
            Physics.SyncTransforms(); 

            if (tMovement.IsRotationColliding())
            {
                Debug.Log("Collision Detected! Reverting to safe step: " + safeStepIndex);

                // A. Revert Rotation
                pedestal.rotation = originalRotation;
                currentZAngle = originalZAngle;

                // B. Revert Slider UI -> HARD FORCE back to safeStepIndex
                StartCoroutine(ForceSliderVisual(safeSliderValue));

                return; // STOP! Do not update safeStepIndex.
            }
        }

        // --- SUCCESS ---
        // We only update the anchor when the move is successful and collision-free
        safeStepIndex = targetStepIndex;

        HandleSwipeInputState(targetAngleZ);
    }

    IEnumerator ForceSliderVisual(float targetValue)
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