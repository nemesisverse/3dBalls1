using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // new input system

public class TouchControll : MonoBehaviour
{
    public Transform pedestal;
    public float swipeThreshold = 50f;
    public float rotationDuration = 0.25f;

    private Vector2 startPos;
    private bool isRotating = false;
    private bool isTouching = false;

    void Update()
    {
        if (isRotating) return;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (!isTouching)
            {
                startPos = touch.position.ReadValue();
                isTouching = true;
            }
        }
        else if (isTouching && Touchscreen.current != null)
        {
            Vector2 endPos = Touchscreen.current.primaryTouch.position.ReadValue();
            Vector2 delta = endPos - startPos;
            isTouching = false;

            if (delta.magnitude > swipeThreshold)
            {
                Vector2 absDelta = new Vector2(Mathf.Abs(delta.x), Mathf.Abs(delta.y));

                if (absDelta.x > absDelta.y)
                {
                    if (delta.x > 0) StartCoroutine(RotateByWorldAxis(Vector3.up, -90f));
                    else StartCoroutine(RotateByWorldAxis(Vector3.up, 90f));
                }
                else
                {
                    if (delta.y > 0) StartCoroutine(RotateByWorldAxis(Vector3.right, 90f));
                    else StartCoroutine(RotateByWorldAxis(Vector3.right, -90f));
                }
            }
        }
    }

    IEnumerator RotateByWorldAxis(Vector3 axis, float degrees)
    {
        isRotating = true;
        Quaternion from = pedestal.rotation;
        Quaternion to = Quaternion.AngleAxis(degrees, axis) * from;

        float elapsed = 0f;
        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / rotationDuration);
            pedestal.rotation = Quaternion.Slerp(from, to, t);
            yield return null;
        }

        pedestal.rotation = to;
        isRotating = false;
    }
}
