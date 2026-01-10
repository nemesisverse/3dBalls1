using UnityEngine;
using UnityEngine.InputSystem;

public class SwipeInput : MonoBehaviour
{
    private TouchControl touchControl;
    private Vector2 startPos;
    private Vector2 endPos;
    
    public float minSwipeDistance = 100f;

    void Awake()
    {
        touchControl = new TouchControl();
    }

    private void OnEnable()
    {
        touchControl.Enable();
        touchControl.Touch.Press.performed += StartTouch;
        touchControl.Touch.Press.canceled += EndTouch;
    }

    private void OnDisable()
    {
        touchControl.Touch.Press.performed -= StartTouch;
        touchControl.Touch.Press.canceled -= EndTouch;
        touchControl.Disable();
    }

    private void StartTouch(InputAction.CallbackContext context)
    {
        startPos = touchControl.Touch.Position.ReadValue<Vector2>();
    }

    private void EndTouch(InputAction.CallbackContext context)
    {
        endPos = touchControl.Touch.Position.ReadValue<Vector2>();
        DetectSwipe();
    }

    private void DetectSwipe()
    {
        Vector2 swipe = endPos - startPos;

        if (swipe.magnitude < minSwipeDistance)
            return;

        if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
        {
            if (swipe.x > 0)
            {
                // Swipe Right
                ApplyRotationInstant(Vector3.up, -90f);
            }
            else
            {
                // Swipe Left
                ApplyRotationInstant(Vector3.up, 90f);
            }
        }
        else
        {
            if (swipe.y > 0)
            {
                // Swipe Up
                ApplyRotationInstant(Vector3.right, 90f);
            }
            else
            {
                // Swipe Down
                ApplyRotationInstant(Vector3.right, -90f);
            }
        }
    }

    // Replaces the Coroutine and TryStartRotate
    void ApplyRotationInstant(Vector3 axis, float degrees)
    {
        // 1. Calculate the new rotation
        // Multiplying on the LEFT applies the rotation in World Space (which matches your previous logic)
        Quaternion targetRotation = Quaternion.AngleAxis(degrees, axis) * transform.rotation;

        // 2. Snap instantly
        transform.rotation = targetRotation;

        // 3. (Optional) If you need to trigger Game Manager updates, do it here
        // GameManager.Instance.CheckGrid(); 
    }
}