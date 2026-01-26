using UnityEngine;
using UnityEngine.UI;
using System.Collections; 

public class SliderPedestalController1 : MonoBehaviour
{
    public Slider slider;
    public Transform pedestal;
    public int snapPoints = 9; 

    private float currentZAngle = 0f;
    private float previousValue;
    private int previousStepIndex;
    
    private bool isReverting = false; 

    // --- Control Flags ---
    [HideInInspector] public bool allowIncrease = true;
    [HideInInspector] public bool allowDecrease = true;

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
        previousValue = slider.value;
        float snapInterval = 1f / (snapPoints - 1);
        previousStepIndex = Mathf.RoundToInt(previousValue / snapInterval);
        SnapAndRotate(slider.value, false); 
    }

    void OnSliderValueChanged(float value)
    {
        if (isReverting) return;

        float snapInterval = 1f / (snapPoints - 1);
        int currentStepIndex = Mathf.RoundToInt(value / snapInterval);

        // --- 1. SNAPPY THRESHOLD CHECK ---
        if (currentStepIndex == previousStepIndex) return; 

        // --- 2. RESTRICTION LOGIC (Increase/Decrease) ---
        // Calculate direction: +1 is Increasing, -1 is Decreasing
        int direction = currentStepIndex > previousStepIndex ? 1 : -1;

        // Check if movement is allowed
        if (direction > 0 && !allowIncrease)
        {
            Debug.Log("Slider Increase Blocked by TMovement logic.");
            float safeValue = previousStepIndex * snapInterval;
            StartCoroutine(ForceSliderVisual(safeValue));
            return;
        }
        if (direction < 0 && !allowDecrease)
        {
            Debug.Log("Slider Decrease Blocked by TMovement logic.");
            float safeValue = previousStepIndex * snapInterval;
            StartCoroutine(ForceSliderVisual(safeValue));
            return;
        }

        // --- 3. ONE-STEP CONSTRAINT ---
        int stepJump = Mathf.Abs(currentStepIndex - previousStepIndex);
        if (stepJump > 1)
        {
            int allowedStepIndex = previousStepIndex + direction;
            float allowedValue = allowedStepIndex * snapInterval;
            StartCoroutine(ForceSliderVisual(allowedValue));
            value = allowedValue; 
        }
        else
        {
            // Valid jump: Snap value exactly
            float snappedValue = currentStepIndex * snapInterval;
            if (Mathf.Abs(value - snappedValue) > 0.001f)
            {
                StartCoroutine(ForceSliderVisual(snappedValue));
            }
        }

        // --- 4. PROCESS ROTATION ---
        SnapAndRotate(value, true);
    }

    void SnapAndRotate(float rawValue, bool checkForCollisions)
    {
        float snapInterval = 1f / (snapPoints - 1);
        int nearestStep = Mathf.RoundToInt(rawValue / snapInterval);
        float snappedValue = nearestStep * snapInterval;

        float targetAngleZ = snappedValue * 360f;
        float deltaZ = targetAngleZ - currentZAngle;

        if (Mathf.Abs(deltaZ) < 0.01f) return;

        Quaternion originalRotation = pedestal.rotation;
        float originalZAngle = currentZAngle;
        float originalSliderValue = previousValue;

        pedestal.Rotate(Vector3.forward, -deltaZ, Space.World);
        currentZAngle = targetAngleZ;

        // Collision Check
        if (checkForCollisions && tMovement != null)
        {
            Physics.SyncTransforms(); 

            if (tMovement.IsRotationColliding())
            {
                Debug.Log("Collision! Reverting Slider...");
                pedestal.rotation = originalRotation;
                currentZAngle = originalZAngle;
                StartCoroutine(ForceSliderVisual(originalSliderValue));
                return; 
            }
        }

        // Success
        previousValue = snappedValue;
        previousStepIndex = nearestStep;
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