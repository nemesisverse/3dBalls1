using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PedestalController : MonoBehaviour
{
    [Header("Slider Control")]
    public Slider rotationSlider;

    [Header("Snapping Settings")]
    public int snapSteps = 12;

    [Header("Touch & Mouse Rotation")]
    public float rotationSpeed = 0.2f;
    public float dragThreshold = 5f;

    private Vector2 startTouchPosition;
    private bool isDragging = false;
    private string activeAxis = null;

    private InputSystem inputActions; // CHANGED

    void Awake()
    {
        inputActions = new InputSystem(); // CHANGED
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void Start()
    {
        if (rotationSlider != null)
        {
            rotationSlider.onValueChanged.AddListener(SnapSliderRotation);
            SnapSliderRotation(rotationSlider.value); // Initialize Z rotation
        }
    }

    void Update()
    {
        Vector2 inputPosition = Vector2.zero;
        bool isPressed = false;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            isPressed = true;
            inputPosition = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            isPressed = true;
            inputPosition = Mouse.current.position.ReadValue();
        }

        if (isPressed)
        {
            if (!isDragging)
            {
                isDragging = true;
                startTouchPosition = inputPosition;
                activeAxis = null;
            }
            else
            {
                Vector2 delta = inputPosition - startTouchPosition;

                if (activeAxis == null)
                {
                    if (Mathf.Abs(delta.x) > dragThreshold && Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                        activeAxis = "x";
                    else if (Mathf.Abs(delta.y) > dragThreshold)
                        activeAxis = "y";
                }

                Vector3 currentAngles = transform.eulerAngles;

                if (activeAxis == "x")
                {
                    float newAngle = currentAngles.x - delta.x * rotationSpeed;
                    transform.rotation = Quaternion.Euler(
                        SnapAngle(newAngle),
                        currentAngles.y,
                        currentAngles.z
                    );
                }
                else if (activeAxis == "y")
                {
                    float newAngle = currentAngles.y + delta.y * rotationSpeed;
                    transform.rotation = Quaternion.Euler(
                        currentAngles.x,
                        SnapAngle(newAngle),
                        currentAngles.z
                    );
                }
            }
        }
        else
        {
            isDragging = false;
        }
    }

    void SnapSliderRotation(float value)
    {
        float snappedValue = Mathf.Round(value * (snapSteps - 1)) / (snapSteps - 1);
        rotationSlider.SetValueWithoutNotify(snappedValue);

        float zRotation = snappedValue * 360f;
        transform.rotation = Quaternion.Euler(
            transform.eulerAngles.x,
            transform.eulerAngles.y,
            zRotation
        );
    }

    float SnapAngle(float angle)
    {
        float step = 360f / snapSteps;
        return Mathf.Round(angle / step) * step;
    }
}
