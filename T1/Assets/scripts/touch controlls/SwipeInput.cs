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

    private bool isProcessingSwipe = false; // to stop everything when position comparision is happening

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
        OnSwipe?.Invoke(); //invoke ke liye check
    }

    // void ApplyRotationInstant(Vector3 axis, float degrees)
    // {
    //     gameManager.isRotating = true; // //to stop the for loop to cause the movemennt to falling block when rotation is made 
    //                                    // 1. Calculate and apply the new rotation directly
    //     gameManager.motherPlatform.transform.rotation =
    //         Quaternion.AngleAxis(degrees, axis) * gameManager.motherPlatform.transform.rotation;

    //     // 2. (Optional) Sync if you have other logic checking collisions immediately after
    //     Physics.SyncTransforms();
    //     gameManager.isRotating = false; //to stop the for loop to cause the movemennt to falling block when rotation is made 

    //     Debug.Log("Rotation Applied");
    // }

    void ApplyRotationInstant(Vector3 axis, float degrees)
    {
        if (isProcessingSwipe) return; // Block double swipes
        isProcessingSwipe = true;
        gameManager.isRotating = true; // Pauses falling blocks

        Quaternion oldRotation = gameManager.motherPlatform.transform.rotation;

        gameManager.motherPlatform.transform.rotation =
            Quaternion.AngleAxis(degrees, axis) * gameManager.motherPlatform.transform.rotation;

        Physics.SyncTransforms();

        if (CheckOverlapWithFallingBlocks())
        {
            gameManager.motherPlatform.transform.rotation = oldRotation;
            Physics.SyncTransforms();
            Debug.Log("Rotation REVERTED due to overlap");
        }
        else
        {
            Debug.Log("Rotation Applied");
        }

        gameManager.isRotating = false; // Resume falling blocks
        isProcessingSwipe = false; // Allow swipes again
    }


    bool CheckOverlapWithFallingBlocks()
    {
        MonoBehaviour[] allBlocks = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        foreach (MonoBehaviour block in allBlocks)
        {
            if (block is IFallingBlock fallingBlock)
            {
                if (!fallingBlock.enabled) continue;
                if (CheckBlockOverlap(fallingBlock.transform)) return true;
            }
        }
        return false;
    }

    bool CheckBlockOverlap(Transform blockTransform)
    {
        foreach (Transform fallingChild in blockTransform)
        {
            Vector3 fallingPos = fallingChild.position;
            foreach (Transform motherChild in gameManager.motherPlatform.transform)
            {
                Vector3 motherPos = motherChild.position;

                bool xMatch = Mathf.Round(fallingPos.x * 10f) == Mathf.Round(motherPos.x * 10f);
                bool yMatch = Mathf.Round(fallingPos.y * 10f) == Mathf.Round(motherPos.y * 10f);
                bool zMatch = Mathf.Round(fallingPos.z * 10f) == Mathf.Round(motherPos.z * 10f);

                if (xMatch && yMatch && zMatch)
                    return true;
            }
        }
        return false;
    }

    // bool CheckOverlapWithFallingBlocks()
    // {
    //     TMovement[] fallingBlocks = FindObjectsByType<TMovement>(FindObjectsSortMode.None);

    //     foreach (TMovement block in fallingBlocks)
    //     {
    //         if (!block.enabled) continue;

    //         foreach (Transform fallingChild in block.transform)
    //         {
    //             Vector3 fallingPos = fallingChild.position;

    //             foreach (Transform motherChild in gameManager.motherPlatform.transform)
    //             {
    //                 Vector3 motherPos = motherChild.position;

    //                 bool xMatch = Mathf.Round(fallingPos.x * 10f) == Mathf.Round(motherPos.x * 10f);
    //                 bool yMatch = Mathf.Round(fallingPos.y * 10f) == Mathf.Round(motherPos.y * 10f);
    //                 bool zMatch = Mathf.Round(fallingPos.z * 10f) == Mathf.Round(motherPos.z * 10f);

    //                 if (xMatch && yMatch && zMatch)
    //                     return true;
    //             }
    //         }
    //     }
    //     return false;
    // }
    // bool CheckOverlapWithFallingBlocks()
    // {
    //     // Check TMovement blocks
    //     TMovement[] fallingBlocks = FindObjectsByType<TMovement>(FindObjectsSortMode.None);
    //     foreach (TMovement block in fallingBlocks)
    //     {
    //         if (!block.enabled) continue;
    //         if (CheckBlockOverlap(block.transform)) return true;
    //     }

    //     // Check T1Movement blocks
    //     T1Movement[] fallingBlocks1 = FindObjectsByType<T1Movement>(FindObjectsSortMode.None);
    //     foreach (T1Movement block in fallingBlocks1)
    //     {
    //         if (!block.enabled) continue;
    //         if (CheckBlockOverlap(block.transform)) return true;
    //     }

    //     return false;
    // }

    // bool CheckBlockOverlap(Transform blockTransform)
    // {
    //     foreach (Transform fallingChild in blockTransform)
    //     {
    //         Vector3 fallingPos = fallingChild.position;
    //         foreach (Transform motherChild in gameManager.motherPlatform.transform)
    //         {
    //             Vector3 motherPos = motherChild.position;

    //             bool xMatch = Mathf.Round(fallingPos.x * 10f) == Mathf.Round(motherPos.x * 10f);
    //             bool yMatch = Mathf.Round(fallingPos.y * 10f) == Mathf.Round(motherPos.y * 10f);
    //             bool zMatch = Mathf.Round(fallingPos.z * 10f) == Mathf.Round(motherPos.z * 10f);

    //             if (xMatch && yMatch && zMatch)
    //                 return true;
    //         }
    //     }
    //     return false;
    // }
}