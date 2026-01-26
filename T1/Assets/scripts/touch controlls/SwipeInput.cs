// using UnityEngine;
// using UnityEngine.InputSystem;

// public class SwipeInput : MonoBehaviour
// {
//     private TouchControl touchControl;
//     private Vector2 startPos;
//     private Vector2 endPos;

//    public bool canSwipeDown = true;
//    public bool canSwipeUp = true;

//    public bool canSwipeLeft = true;
//    public bool canSwipeRight = true;
   
//     public float minSwipeDistance = 100f;
//     public GameManager gameManager;

//     void Awake()
//     {
//         touchControl = new TouchControl();
//         if (gameManager == null)
//         {
//             gameManager = FindFirstObjectByType<GameManager>();
//             // Note: use FindObjectOfType<GameManager>() if on older Unity versions
//         }
        
//     }

//     private void OnEnable()
//     {
//         touchControl.Enable();
//         touchControl.Touch.Press.performed += StartTouch;
//         touchControl.Touch.Press.canceled += EndTouch;
//     }

//     private void OnDisable()
//     {
//         touchControl.Touch.Press.performed -= StartTouch;
//         touchControl.Touch.Press.canceled -= EndTouch;
//         touchControl.Disable();
//     }

//     private void StartTouch(InputAction.CallbackContext context)
//     {
//         startPos = touchControl.Touch.Position.ReadValue<Vector2>();
//     }

//     private void EndTouch(InputAction.CallbackContext context)
//     {
//         endPos = touchControl.Touch.Position.ReadValue<Vector2>();
//         DetectSwipe();
//     }

//     private void DetectSwipe()
//     {
//         Vector2 swipe = endPos - startPos;

//         if (swipe.magnitude < minSwipeDistance)
//             return;

//         if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
//         {
//             if (swipe.x > 0)
//             {
//                 if(canSwipeRight)
//                 {
//                     // Swipe Right
//                     ApplyRotationInstant(Vector3.up, -90f);
//                 }
//                 else
//                 {
//                     Debug.Log("Swipe Right Disabled");
//                 }
//                 // Swipe Right
//                 //ApplyRotationInstant(Vector3.up, -90f);
                
                

//             }
//             else
//             {
//                 if(canSwipeLeft)
//                 {
//                     // Swipe Left
//                     ApplyRotationInstant(Vector3.up, 90f);
//                 }
//                 else
//                 {
//                     Debug.Log("Swipe Left Disabled");
//                 }
                
//               //  ApplyRotationInstant(Vector3.up, 90f);
                
                
                
//             }
//         }
//         else
//         {
//             if (swipe.y > 0)
//             {
//                 // Swipe Up
//                 if(canSwipeUp)
//                 {
//                     ApplyRotationInstant(Vector3.right, 90f);
//                 }
//                 else
//                 {
//                     Debug.Log("Swipe Up Disabled"); 
//                 }
//                 //ApplyRotationInstant(Vector3.right, 90f);
                
//             }
//             else
//             {
//                 if(canSwipeDown)ApplyRotationInstant(Vector3.right, -90f);
//                 else
//                 {
//                     Debug.Log("Swipe Down Disabled");
//                 }
//                 // Swipe Down
                
                
//             }
//         }
//     }






//     // Replaces the Coroutine and TryStartRotate
//     void ApplyRotationInstant(Vector3 axis, float degrees)
//     {
//         // 1. Calculate the new rotation
//         // Multiplying on the LEFT applies the rotation in World Space (which matches your previous logic)
//         Quaternion targetRotation = Quaternion.AngleAxis(degrees, axis) * transform.rotation;

//         // 2. Snap instantly
//         transform.rotation = targetRotation;

//         // 3. (Optional) If you need to trigger Game Manager updates, do it here
//         // GameManager.Instance.CheckGrid(); 
//     }

   
// }


using UnityEngine;
using UnityEngine.InputSystem;

public class SwipeInput : MonoBehaviour
{
    private TouchControl touchControl;
    private Vector2 startPos;
    private Vector2 endPos;

    // REMOVED: public bool canSwipeDown, canSwipeUp, etc. 
    // We no longer limit these; we try to rotate and revert if it fails.
   
    public float minSwipeDistance = 100f;
    public GameManager gameManager;

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

    // UPDATED: Logic to Try Rotate -> Check Collision -> Revert if needed
    void ApplyRotationInstant(Vector3 axis, float degrees)
    {
        // 1. Store previous rotation
        Quaternion originalRotation = transform.rotation;

        // 2. Apply Rotation
        Quaternion targetRotation = Quaternion.AngleAxis(degrees, axis) * transform.rotation;
        transform.rotation = targetRotation;

        // Force transforms to update immediately so we can check positions accurately
        Physics.SyncTransforms(); 

        // 3. Find the active TMovement script to check for overlaps
        TMovement activeMovement = FindFirstObjectByType<TMovement>();

        if (activeMovement != null)
        {
            // If the rotation we just did caused a collision...
            if (activeMovement.IsRotationColliding())
            {
                Debug.Log("Rotation Blocked by Collision! Reverting...");
                // 4. Revert
                transform.rotation = originalRotation;
            }
        }
    }
}