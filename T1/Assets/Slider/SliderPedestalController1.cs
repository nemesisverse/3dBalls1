using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class SliderPedestalController1 : MonoBehaviour
{
    public Slider slidercontrollerobject;
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
        if (slidercontrollerobject != null)
        {
            slidercontrollerobject.onValueChanged.AddListener(OnSliderValueChanged);
            
            // Initialize Safe State
            lastSafeSliderValue = slidercontrollerobject.value;
            lastSafeZAngle = 0f;
            lastSafeRotation = transform.rotation;
            
            // Initial Rotation (no collision check)
            SnapAndRotate(slidercontrollerobject.value, 0, false);
        }
    }

    void OnSliderValueChanged(float rawValue)
    {
        // Block input if we are already rotating or checking physics
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
        
        // Start processing the snap
        StartCoroutine(ProcessRotation(targetValue, direction));
    }

    IEnumerator ProcessRotation(float targetValue, int direction)
    {
        isProcessing = true;
        
        // ⬇️ DISALLOW HOLDING: Turn off interaction so the user can't drag during processing
        if (slidercontrollerobject != null)
            slidercontrollerobject.interactable = false;

        float snapInterval = 1f / (snapPoints - 1);
        float snappedValue = Mathf.Round(targetValue / snapInterval) * snapInterval;
        float targetAngleZ = snappedValue * 360f;
        float deltaZ = targetAngleZ - currentZAngle;

        if (Mathf.Abs(deltaZ) < 0.01f)
        {
            if (slidercontrollerobject != null) slidercontrollerobject.interactable = true;
            isProcessing = false;
            yield break;
        }

        // Store CURRENT safe state (for potential rollback)
        Quaternion savedRotation = lastSafeRotation;
        float savedZAngle = lastSafeZAngle;
        float savedSliderValue = lastSafeSliderValue;

        // Apply rotation to the pedestal
        transform.Rotate(Vector3.forward, -deltaZ, Space.World);
        currentZAngle = targetAngleZ;

        // Force Unity to update transforms for physics check
        Physics.SyncTransforms();
        
        // Wait for physics to settle (standard practice for collision detection)
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        // Check for collision via TMovement
        TMovement tMovement = FindFirstObjectByType<TMovement>();
        
        bool hasCollision = false;
        if (tMovement != null)
        {
            hasCollision = tMovement.IsRotationColliding();
        }

        if (hasCollision)
        {
            // REVERT to saved safe state
            transform.rotation = savedRotation;
            currentZAngle = savedZAngle;
            
            // Move UI slider back to the last safe point
            slidercontrollerobject.SetValueWithoutNotify(savedSliderValue);
            HandleSwipeInputState(savedZAngle);
        }
        else
        {
            // SUCCESS: Update safe state to new position
            lastSafeSliderValue = snappedValue;
            lastSafeZAngle = currentZAngle;
            lastSafeRotation = transform.rotation;
            
            // Update UI slider to exact snapped position
            slidercontrollerobject.SetValueWithoutNotify(snappedValue);
            HandleSwipeInputState(currentZAngle);
        }

        onSlide?.Invoke();
        
        // ⬇️ RE-ENABLE HOLDING: Let the user click/interact again
        if (slidercontrollerobject != null)
            slidercontrollerobject.interactable = true;

        isProcessing = false;
    }

    void SnapAndRotate(float targetValue, int direction, bool checkForCollisions)
    {
        float snapInterval = 1f / (snapPoints - 1);
        float snappedValue = Mathf.Round(targetValue / snapInterval) * snapInterval;
        float targetAngleZ = snappedValue * 360f;
        float deltaZ = targetAngleZ - currentZAngle;

        if (Mathf.Abs(deltaZ) < 0.01f) return;

        transform.Rotate(Vector3.forward, -deltaZ, Space.World);
        currentZAngle = targetAngleZ;
        
        lastSafeSliderValue = snappedValue;
        lastSafeZAngle = targetAngleZ;
        lastSafeRotation = transform.rotation;
        
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
        if (slidercontrollerobject != null)
            lastSafeSliderValue = slidercontrollerobject.value;
        
        lastSafeZAngle = currentZAngle;
        lastSafeRotation = transform.rotation;
    }
}