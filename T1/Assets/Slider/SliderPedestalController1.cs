using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class SliderPedestalController1 : MonoBehaviour
{
    public Slider slider;
    public Transform pedestal;
    public int snapPoints = 9; 

    // --- State Tracking ---
    private float currentZAngle = 0f;
    
    // The last KNOWN SAFE rotation & slider value (no collision)
    private float lastSafeSliderValue;
    private float lastSafeZAngle;
    private Quaternion lastSafeRotation;
    
    private bool isProcessing = false;

    // References
    public SwipeInput swipeInput;
    // DON'T cache TMovement - it gets destroyed/recreated each piece

    public event Action onSlide;

    void Awake()
    {
        if (swipeInput == null) 
        {
            swipeInput = FindFirstObjectByType<SwipeInput>();
            if (swipeInput == null)
                Debug.LogError("[SliderController] SwipeInput not found in scene!");
        }
    }

    void Start()
    {
        slider.onValueChanged.AddListener(OnSliderValueChanged);
        
        // Initialize Safe State
        lastSafeSliderValue = slider.value;
        lastSafeZAngle = 0f;
        lastSafeRotation = pedestal.rotation;
        
        // Initial Rotation (no collision check)
        SnapAndRotate(slider.value, 0, false); 
    }

    void OnSliderValueChanged(float rawValue)
    {
        // Block ALL input during processing
        if (isProcessing) 
        {
            return;
        }

        float snapInterval = 1f / (snapPoints - 1);
        
        // Calculate snapped target value
        float targetValue = Mathf.Round(rawValue / snapInterval) * snapInterval;

        // No change? Ignore
        if (Mathf.Abs(targetValue - lastSafeSliderValue) < 0.01f) return;

        // Determine direction of movement
        int direction = targetValue > lastSafeSliderValue ? 1 : -1;
        
        // Start processing
        StartCoroutine(ProcessRotation(targetValue, direction));
    }

    IEnumerator ProcessRotation(float targetValue, int direction)
    {
        isProcessing = true;

        // Calculate target angle
        float snapInterval = 1f / (snapPoints - 1);
        float snappedValue = Mathf.Round(targetValue / snapInterval) * snapInterval;
        float targetAngleZ = snappedValue * 360f;
        float deltaZ = targetAngleZ - currentZAngle;

        // Validate there's actually a change needed
        if (Mathf.Abs(deltaZ) < 0.01f)
        {
            isProcessing = false;
            yield break;
        }

        // Store CURRENT safe state (for potential rollback)
        Quaternion savedRotation = lastSafeRotation;
        float savedZAngle = lastSafeZAngle;
        float savedSliderValue = lastSafeSliderValue;

        // Apply rotation
        pedestal.Rotate(Vector3.forward, -deltaZ, Space.World);
        currentZAngle = targetAngleZ;

        // Force Unity to update transforms
        Physics.SyncTransforms();
        
        // Wait for physics to settle
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        // ⬇️ FIND TMovement DYNAMICALLY (it gets destroyed/recreated each piece) ⬇️
        TMovement tMovement = FindFirstObjectByType<TMovement>();
        
        // Check collision
        bool hasCollision = false;
        if (tMovement != null)
        {
            hasCollision = tMovement.IsRotationColliding();
        }
        else
        {
            // No active falling piece = no collision possible
            hasCollision = false;
        }

        if (hasCollision)
        {
            // REVERT rotation to saved safe state
            pedestal.rotation = savedRotation;
            currentZAngle = savedZAngle;
            
            // Force slider back to safe value
            slider.SetValueWithoutNotify(savedSliderValue);
            
            HandleSwipeInputState(savedZAngle);
        }
        else
        {
            // Update safe state to NEW position
            lastSafeSliderValue = snappedValue;
            lastSafeZAngle = currentZAngle;
            lastSafeRotation = pedestal.rotation;
            
            // Ensure slider shows exact snapped value
            slider.SetValueWithoutNotify(snappedValue);
            
            HandleSwipeInputState(currentZAngle);
        }

        onSlide?.Invoke();
        
        isProcessing = false;
    }

    void SnapAndRotate(float targetValue, int direction, bool checkForCollisions)
    {
        // Used for initial setup only
        float snapInterval = 1f / (snapPoints - 1);
        float snappedValue = Mathf.Round(targetValue / snapInterval) * snapInterval;
        float targetAngleZ = snappedValue * 360f;
        float deltaZ = targetAngleZ - currentZAngle;

        if (Mathf.Abs(deltaZ) < 0.01f) return;

        pedestal.Rotate(Vector3.forward, -deltaZ, Space.World);
        currentZAngle = targetAngleZ;
        
        lastSafeSliderValue = snappedValue;
        lastSafeZAngle = targetAngleZ;
        lastSafeRotation = pedestal.rotation;
        
        HandleSwipeInputState(targetAngleZ);
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

    public void UpdateSafeState()
    {
        lastSafeSliderValue = slider.value;
        lastSafeZAngle = currentZAngle;
        lastSafeRotation = pedestal.rotation;
    }
}