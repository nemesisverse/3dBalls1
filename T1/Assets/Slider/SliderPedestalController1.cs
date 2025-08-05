using UnityEngine;
using UnityEngine.UI;

public class SliderPedestalController1 : MonoBehaviour
{
    public Slider slider;
    public Transform pedestal;
    public int snapPoints = 12;

    private float currentZAngle = 0f;

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

        float angleZ = snappedValue * 360f;

        float deltaZ = angleZ - currentZAngle;
        currentZAngle = angleZ;

        // Rotate around global Z axis, without affecting current X/Y rotation
        pedestal.Rotate(Vector3.forward, -deltaZ, Space.World);
    }
}
