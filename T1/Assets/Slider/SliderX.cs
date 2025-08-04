using UnityEngine;
using UnityEngine.UI;

public class SliderX : MonoBehaviour
{
    public Slider slider;
    public Transform pedestal;
    public int snapPoints = 12;

    private float currentAngle = 0f;

    void Start()
    {
        slider.onValueChanged.AddListener(OnSliderValueChanged);
        SnapAndRotate(slider.value);
    }

    void OnSliderValueChanged(float value)
    {
        SnapAndRotate(value);
    }

    void SnapAndRotate(float value)
    {
        float snapInterval = 1f / (snapPoints - 1);
        int nearestStep = Mathf.RoundToInt(value / snapInterval);
        float snappedValue = nearestStep * snapInterval;
        slider.value = snappedValue;

        float angle = snappedValue * 360f;

        float deltaAngle = angle - currentAngle;
        currentAngle = angle;

        // Rotate around global -X axis, without affecting current Y/Z rotation
        pedestal.Rotate(Vector3.left, deltaAngle, Space.World);
    }
}
