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
//



// using UnityEngine;
// using UnityEngine.UI;

// public class SliderPedestalController1 : MonoBehaviour
// {
//     public Slider slider;
//     public Transform pedestal;
//     public int snapPoints = 9;

//     private float currentZAngle = 0f;

//     // --- Flow Control Variables ---
//     [HideInInspector] public bool allowIncrease = true;
//     [HideInInspector] public bool allowDecrease = true;

//     private float previousValue;
//     private bool isReverting = false;

//     public SwipeInput swipeInput;

//     void Awake()
//     {
//         if (swipeInput == null)
//         {
//             swipeInput = FindFirstObjectByType<SwipeInput>();
//             // Note: use FindObjectOfType<SwipeInput>() if on older Unity versions
//         }
//     }
//     void Start()
//     {
//         slider.onValueChanged.AddListener(OnSliderValueChanged);
        
//         // Initial Snap to ensure we start on a valid step
//         SnapAndRotate(slider.value);
//         previousValue = slider.value; // Sync previousValue to the snapped start position
//     }

//     void OnSliderValueChanged(float value)
//     {
//         if (isReverting) return;

//         // -------------------------------------------------------------
//         // 1. STEP JUMP CONSTRAINT (New Logic)
//         // -------------------------------------------------------------
//         float snapInterval = 1f / (snapPoints - 1);
        
//         // Calculate which "Step Index" we are coming from and going to
//         int oldStepIndex = Mathf.RoundToInt(previousValue / snapInterval);
//         int newStepIndex = Mathf.RoundToInt(value / snapInterval);

//         // If the move is more than 1 step away...
//         if (Mathf.Abs(newStepIndex - oldStepIndex) > 1)
//         {
//             // Determine the permitted target (only 1 step in the drag direction)
//             int direction = newStepIndex > oldStepIndex ? 1 : -1;
//             int allowedStepIndex = oldStepIndex + direction;
//             float allowedValue = allowedStepIndex * snapInterval;

//             // Force the slider to the allowed single-step position
//             isReverting = true;
//             slider.value = allowedValue;
//             isReverting = false;

//             // Update our local 'value' variable so the rest of the function 
//             // processes this valid single step, rather than the large jump.
//             value = allowedValue;
//         }

//         // -------------------------------------------------------------
//         // 2. DIRECTION & PERMISSION CHECKS (Existing Logic)
//         // -------------------------------------------------------------
        
//         // Check Increase
//         if (value > previousValue)
//         {
//             if (!allowIncrease)
//             {
//                 RevertValue();
//                 return;
//             }
//         }
//         // Check Decrease
//         else if (value < previousValue)
//         {
//             if (!allowDecrease)
//             {
//                 RevertValue();
//                 return;
//             }
//         }

//         // Update history and rotate
//         previousValue = value;
//         SnapAndRotate(value);
//     }

//     void RevertValue()
//     {
//         isReverting = true;       
//         slider.value = previousValue; 
//         isReverting = false;      
//         Debug.Log("Slider movement prevented by logic.");
//     }

//     void SnapAndRotate(float value)
//     {
//         float snapInterval = 1f / (snapPoints - 1);
//         int nearestStep = Mathf.RoundToInt(value / snapInterval);
//         float snappedValue = nearestStep * snapInterval;
        
//         // Ensure slider visually snaps
//         if (slider.value != snappedValue)
//         {
//             // Note: This setting might trigger OnValueChanged again recursively, 
//             // but since snappedValue is "valid", the logic handles it safely.
//             slider.value = snappedValue; 
//         }

//         float angleZ = snappedValue * 360f;
//         float deltaZ = angleZ - currentZAngle;
//         currentZAngle = angleZ;

//         pedestal.Rotate(Vector3.forward, -deltaZ, Space.World);
//         // Debug.Log(snappedValue);
//     }
// }

using UnityEngine;
using UnityEngine.UI;

public class SliderPedestalController1 : MonoBehaviour
{
    public Slider slider;
    public Transform pedestal;
    public int snapPoints = 9; // Creates 45-degree intervals

    private float currentZAngle = 0f;

    // --- Flow Control Variables ---
    [HideInInspector] public bool allowIncrease = true;
    [HideInInspector] public bool allowDecrease = true;

    private float previousValue;
    private bool isReverting = false;

    public SwipeInput swipeInput;

    void Awake()
    {
        if (swipeInput == null)
        {
            swipeInput = Object.FindFirstObjectByType<SwipeInput>(); 
            
        }
    }

    void Start()
    {
        slider.onValueChanged.AddListener(OnSliderValueChanged);
        
        // Initial Snap
        SnapAndRotate(slider.value);
        previousValue = slider.value; 
    }

    void OnSliderValueChanged(float value)
    {
        if (isReverting) return;

        // 1. STEP JUMP CONSTRAINT
        float snapInterval = 1f / (snapPoints - 1);
        int oldStepIndex = Mathf.RoundToInt(previousValue / snapInterval);
        int newStepIndex = Mathf.RoundToInt(value / snapInterval);

        if (Mathf.Abs(newStepIndex - oldStepIndex) > 1)
        {
            int direction = newStepIndex > oldStepIndex ? 1 : -1;
            int allowedStepIndex = oldStepIndex + direction;
            float allowedValue = allowedStepIndex * snapInterval;

            isReverting = true;
            slider.value = allowedValue;
            isReverting = false;
            value = allowedValue;
        }

        // 2. DIRECTION CHECKS
        if (value > previousValue && !allowIncrease)
        {
            RevertValue();
            return;
        }
        else if (value < previousValue && !allowDecrease)
        {
            RevertValue();
            return;
        }

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
        
        if (slider.value != snappedValue)
        {
            slider.value = snappedValue; 
        }

        float angleZ = snappedValue * 360f;
        
        // --- UPDATED LOGIC: Modulo Check ---
        if (swipeInput != null)
        {
            // We get the remainder when divided by 90.
            // If angle is 0, 90, 180... remainder is 0.
            // If angle is 45, 135, 225... remainder is 45.
            float remainder = angleZ % 90f;

            // Check if we are on a 90-degree mark (Allow specific epsilon for float errors)
            if (Mathf.Abs(remainder) < 1f || Mathf.Abs(remainder - 90f) < 1f)
            {
                if (!swipeInput.enabled) 
                {
                    swipeInput.enabled = true;
                    // Debug.Log($"Swipe Enabled at {angleZ}");
                }
            }
            // Check if we are on a 45-degree mark
            else if (Mathf.Abs(remainder - 45f) < 1f)
            {
                if (swipeInput.enabled)
                {
                    swipeInput.enabled = false;
                    // Debug.Log($"Swipe Disabled at {angleZ}");
                }
            }
        }
        // ------------------------------------

        float deltaZ = angleZ - currentZAngle;
        currentZAngle = angleZ;

        pedestal.Rotate(Vector3.forward, -deltaZ, Space.World);
    }
}