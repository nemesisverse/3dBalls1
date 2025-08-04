using UnityEngine;
using UnityEngine.UI;

public class SliderSnap : MonoBehaviour
{
    public Slider slider;
    private int totalSteps = 11; // 11 intervals between 0 and 1 → 12 snap points

    

    void Start()
    {
        slider.onValueChanged.AddListener(SnapValue);
    }

    void SnapValue(float value)
    {
        float stepSize = 1f / totalSteps;
        float snappedValue = Mathf.Round(value / stepSize) * stepSize;
        slider.SetValueWithoutNotify(snappedValue);
    }
}
