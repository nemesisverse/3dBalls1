using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwipeInput : MonoBehaviour
{
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
            Debug.Log(swipe.x > 0 ? "Swipe Right" : "Swipe Left");
        }
        else
        {
            Debug.Log(swipe.y > 0 ? "Swipe Up" : "Swipe Down");
        }
    }


}
