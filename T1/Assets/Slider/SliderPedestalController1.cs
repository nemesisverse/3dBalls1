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

using UnityEngine;
using UnityEngine.UI;

public class SliderPedestalController1 : MonoBehaviour
{
    public Slider slider;
    public Transform pedestal;
    public int snapPoints = 9;

    private float currentZAngle = 0f;

    // --- Flow Control Variables (Accessed by TMovement) ---
    [HideInInspector] public bool allowIncrease = true;
    [HideInInspector] public bool allowDecrease = true;

    private float previousValue;
    private bool isReverting = false; // Prevents infinite loops when we force reset the value

    void Start()
    {
        // Initialize previousValue to current start value
        previousValue = slider.value;

        slider.onValueChanged.AddListener(OnSliderValueChanged);
        SnapAndRotate(slider.value);
    }

    void OnSliderValueChanged(float value)
    {
        // If we are currently forcing the slider back, ignore this event
        if (isReverting) return;

        // 1. Check if trying to INCREASE
        if (value > previousValue)
        {
            if (!allowIncrease)
            {
                RevertValue();
                return;
            }
        }
        // 2. Check if trying to DECREASE
        else if (value < previousValue)
        {
            if (!allowDecrease)
            {
                RevertValue();
                return;
            }
        }

        // If valid, update history and perform rotation
        previousValue = value;
        SnapAndRotate(value);
    }

    void RevertValue()
    {
        isReverting = true;       // Lock flag to prevent recursion
        slider.value = previousValue; // Reset to old value
        isReverting = false;      // Unlock flag
        Debug.Log("Slider movement prevented by TMovement logic.");
    }

    void SnapAndRotate(float value)
    {
        float snapInterval = 1f / (snapPoints - 1);
        int nearestStep = Mathf.RoundToInt(value / snapInterval);
        float snappedValue = nearestStep * snapInterval;

        // Visual snapping of the handle is optional; 
        // if you want the handle to jump, you would set slider.value here, 
        // but that requires removing/adding the listener to avoid loops.
        // For now, we just snap the rotation.

        float angleZ = snappedValue * 360f;
        float deltaZ = angleZ - currentZAngle;
        currentZAngle = angleZ;

        // Rotate around global Z axis
        pedestal.Rotate(Vector3.forward, -deltaZ, Space.World);
    }
}