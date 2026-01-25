// using UnityEngine;
// using UnityEngine.UI;

// public class SliderPedestalController1 : MonoBehaviour
// {
//     public Slider slider;
//     public Transform pedestal;
//     public int snapPoints = 9;

//     private float currentZAngle = 0f;

//     void Start()
//     {
//         slider.onValueChanged.AddListener(OnSliderValueChanged);
//         SnapAndRotate(slider.value);
//     }

//     void OnSliderValueChanged(float value)
//     {
//         SnapAndRotate(value);
//     }

//     void SnapAndRotate(float value)
//     {
//         float snapInterval = 1f / (snapPoints - 1);
//         int nearestStep = Mathf.RoundToInt(value / snapInterval);
//         float snappedValue = nearestStep * snapInterval;
//         slider.value = snappedValue;

//         float angleZ = snappedValue * 360f;

//         float deltaZ = angleZ - currentZAngle;
//         currentZAngle = angleZ;

//         // Rotate around global Z axis, without affecting current X/Y rotation
//         pedestal.Rotate(Vector3.forward, -deltaZ, Space.World);
//         Debug.Log(value);
//     }
// }

// using UnityEngine;
// using UnityEngine.UI;

// public class SliderPedestalController1 : MonoBehaviour
// {
//     public Slider slider;
//     public Transform pedestal;
//     public int snapPoints = 9;

//     private float currentZAngle = 0f;

//     // --- Flow Control Variables (Accessed by TMovement) ---
//     [HideInInspector] public bool allowIncrease = true;
//     [HideInInspector] public bool allowDecrease = true;

//     private float previousValue;
//     private bool isReverting = false; // Prevents infinite loops when we force reset the value

//     void Start()
//     {
//         // Initialize previousValue to current start value
//         previousValue = slider.value;

//         slider.onValueChanged.AddListener(OnSliderValueChanged);
//         SnapAndRotate(slider.value);
//     }

//     void OnSliderValueChanged(float value)
//     {
//         // If we are currently forcing the slider back, ignore this event
//         if (isReverting) return;

//         // 1. Check if trying to INCREASE
//         if (value > previousValue)
//         {
//             if (!allowIncrease)
//             {
//                 RevertValue();
//                 return;
//             }
//         }
//         // 2. Check if trying to DECREASE
//         else if (value < previousValue)
//         {
//             if (!allowDecrease)
//             {
//                 RevertValue();
//                 return;
//             }
//         }

//         // If valid, update history and perform rotation
//         previousValue = value;
//         SnapAndRotate(value);
//     }

//     void RevertValue()
//     {
//         isReverting = true;       // Lock flag to prevent recursion
//         slider.value = previousValue; // Reset to old value
//         isReverting = false;      // Unlock flag
//         Debug.Log("Slider movement prevented by TMovement logic.");
//     }

//     void SnapAndRotate(float value)
//     {
//         float snapInterval = 1f / (snapPoints - 1);
//         int nearestStep = Mathf.RoundToInt(value / snapInterval);
//         float snappedValue = nearestStep * snapInterval;
//         slider.value = snappedValue;

//         // Visual snapping of the handle is optional; 
//         // if you want the handle to jump, you would set slider.value here, 
//         // but that requires removing/adding the listener to avoid loops.
//         // For now, we just snap the rotation.

//         float angleZ = snappedValue * 360f;
//         float deltaZ = angleZ - currentZAngle;
//         currentZAngle = angleZ;

//         // Rotate around global Z axis
//         pedestal.Rotate(Vector3.forward, -deltaZ, Space.World);
//         Debug.Log(snappedValue);
//     }
// }





using UnityEngine;
using UnityEngine.UI;

public class SliderPedestalController1 : MonoBehaviour
{
    public Slider slider;
    public Transform pedestal;
    public int snapPoints = 9;

    private float currentZAngle = 0f;

    // --- Flow Control Variables ---
    [HideInInspector] public bool allowIncrease = true;
    [HideInInspector] public bool allowDecrease = true;

    private float previousValue;
    private bool isReverting = false;

    void Start()
    {
        slider.onValueChanged.AddListener(OnSliderValueChanged);
        
        // Initial Snap to ensure we start on a valid step
        SnapAndRotate(slider.value);
        previousValue = slider.value; // Sync previousValue to the snapped start position
    }

    void OnSliderValueChanged(float value)
    {
        if (isReverting) return;

        // -------------------------------------------------------------
        // 1. STEP JUMP CONSTRAINT (New Logic)
        // -------------------------------------------------------------
        float snapInterval = 1f / (snapPoints - 1);
        
        // Calculate which "Step Index" we are coming from and going to
        int oldStepIndex = Mathf.RoundToInt(previousValue / snapInterval);
        int newStepIndex = Mathf.RoundToInt(value / snapInterval);

        // If the move is more than 1 step away...
        if (Mathf.Abs(newStepIndex - oldStepIndex) > 1)
        {
            // Determine the permitted target (only 1 step in the drag direction)
            int direction = newStepIndex > oldStepIndex ? 1 : -1;
            int allowedStepIndex = oldStepIndex + direction;
            float allowedValue = allowedStepIndex * snapInterval;

            // Force the slider to the allowed single-step position
            isReverting = true;
            slider.value = allowedValue;
            isReverting = false;

            // Update our local 'value' variable so the rest of the function 
            // processes this valid single step, rather than the large jump.
            value = allowedValue;
        }

        // -------------------------------------------------------------
        // 2. DIRECTION & PERMISSION CHECKS (Existing Logic)
        // -------------------------------------------------------------
        
        // Check Increase
        if (value > previousValue)
        {
            if (!allowIncrease)
            {
                RevertValue();
                return;
            }
        }
        // Check Decrease
        else if (value < previousValue)
        {
            if (!allowDecrease)
            {
                RevertValue();
                return;
            }
        }

        // Update history and rotate
        previousValue = value;
        SnapAndRotate(value);
    }

    void RevertValue()
    {
        isReverting = true;       
        slider.value = previousValue; 
        isReverting = false;      
        Debug.Log("Slider movement prevented by logic.");
    }

    void SnapAndRotate(float value)
    {
        float snapInterval = 1f / (snapPoints - 1);
        int nearestStep = Mathf.RoundToInt(value / snapInterval);
        float snappedValue = nearestStep * snapInterval;
        
        // Ensure slider visually snaps
        if (slider.value != snappedValue)
        {
            // Note: This setting might trigger OnValueChanged again recursively, 
            // but since snappedValue is "valid", the logic handles it safely.
            slider.value = snappedValue; 
        }

        float angleZ = snappedValue * 360f;
        float deltaZ = angleZ - currentZAngle;
        currentZAngle = angleZ;

        pedestal.Rotate(Vector3.forward, -deltaZ, Space.World);
        // Debug.Log(snappedValue);
    }
}