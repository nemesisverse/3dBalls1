

using UnityEngine;
using UnityEngine.InputSystem;
using System; 

public class SwipeInput : MonoBehaviour
{
    private TouchControl touchControl;
    private Vector2 startPos;
    private Vector2 endPos;

    // REMOVED: public bool canSwipeDown, canSwipeUp, etc. 
    // We no longer limit these; we try to rotate and revert if it fails.
   
    public float minSwipeDistance = 100f;
    public GameManager gameManager;
    public event Action OnSwipe;

    public bool canSwipeRight = true;
    public bool canSwipeLeft = true;
    public bool canSwipeUp = true;
    public bool canSwipeDown = true;

    void Awake()
    {
        touchControl = new TouchControl();
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
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

       

        // Logic simplified: We just try to rotate. The checking happens inside ApplyRotationInstant.
        if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
        {
            if (swipe.x > 0)
            {
                if (canSwipeRight)
                {
                    // Swipe Right
                    ApplyRotationInstant(Vector3.up, -90f);
                }
                else
                {
                    Debug.Log("Swipe Right blocked by canSwipeRight");
                }
                // Swipe Right
                //ApplyRotationInstant(Vector3.up, -90f);
            }
            else
            {
                if (canSwipeLeft)
                {
                    // Swipe Left
                    ApplyRotationInstant(Vector3.up, 90f);
                }
                else
                {
                    Debug.Log("Swipe Left blocked by canSwipeLeft");
                }
                // Swipe Left
                //ApplyRotationInstant(Vector3.up, 90f);
            }
        }
        else
        {
            if (swipe.y > 0)
            {   
                if (canSwipeUp)
                {
                    // Swipe Up
                    ApplyRotationInstant(Vector3.right, 90f);
                }
                else
                {
                    Debug.Log("Swipe Up blocked by canSwipeUp");
                }

                // Swipe Up
                //ApplyRotationInstant(Vector3.right, 90f);
            }
            else
            {
                if (canSwipeDown)
                {
                    // Swipe Down
                    ApplyRotationInstant(Vector3.right, -90f);
                }
                else
                {
                   // Debug.Log("Swipe Down blocked by canSwipeDown");
                }
            }
        }
         OnSwipe?.Invoke(); //invoke ke liye check
    }

   
void ApplyRotationInstant(Vector3 axis, float degrees)
{
    // 1. Calculate and apply the new rotation directly
    gameManager.motherPlatform.transform.rotation = 
        Quaternion.AngleAxis(degrees, axis) * gameManager.motherPlatform.transform.rotation;

    // 2. (Optional) Sync if you have other logic checking collisions immediately after
    Physics.SyncTransforms(); 

    Debug.Log("Rotation Applied");
}
}