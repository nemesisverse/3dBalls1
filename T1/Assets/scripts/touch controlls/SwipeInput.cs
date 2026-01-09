using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;


public class SwipeInput : MonoBehaviour
{

    [SerializeField] private float rotationDuration = 0.25f;
    private bool isRotating = false;

    private TouchControl touchControl;
    Vector2 startPos;
    Vector2 endPos;
    public float minSwipeDistance = 100f;



    void Awake()
    {
        touchControl = new TouchControl();

    }

    private void OnEnable()
    {
        Debug.Log("SwipeInput ENABLED");
        //GameObject becomes active
        //Component becomes enabled
        //Scene loads and object is active
        touchControl.Enable();
        touchControl.Touch.Press.performed += StartTouch; //initial touch ke time position read karna hai
        touchControl.Touch.Press.canceled += EndTouch;  //touch chodne ke time position read karna hai

    }
    private void OnDisable()
    {
        // UNSUBSCRIBE
        //GameObject is disabled
        //Component is disabled
        //Scene unloads
        //Object is destroyed

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
                Debug.Log("Swipe Right");
                RightRotate();
            }
            else
            {
                LeftRotate();
            }
        }
        else
        {
            if (swipe.y > 0)
            {
                Debug.Log("Swipe Up");
                UpRotate();
            }
            else
            {
                Debug.Log("Swipe Down");
                DownRotate();
            }
        }
    }

    void RightRotate()
    {
        TryStartRotate(Vector3.up, -90f);
    }

    void LeftRotate()
    {
        TryStartRotate(Vector3.up, 90f);
    }

    void UpRotate()
    {
        TryStartRotate(Vector3.right, 90f);
    }

    void DownRotate()
    {
        TryStartRotate(Vector3.right, -90f);
    }


    void TryStartRotate(Vector3 axis, float degrees)
    {
        if (isRotating) return;
        StartCoroutine(RotateByWorldAxis(axis, degrees));
    }
    //This code rotates the object around a world axis over time
    IEnumerator RotateByWorldAxis(Vector3 axis, float degrees)
    {
        isRotating = true;

        Quaternion from = transform.rotation;
        Quaternion to = Quaternion.AngleAxis(degrees, axis) * from;

        float elapsed = 0f;
        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / rotationDuration);
            transform.rotation = Quaternion.Slerp(from, to, t);
            yield return null;
        }

        // snap exactly (prevents drift)
        transform.rotation = to;

        isRotating = false;
    }

}
