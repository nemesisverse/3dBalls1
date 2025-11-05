using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TouchControll : MonoBehaviour
{
    [Header("References")]
    public Transform pedestal;

    [Tooltip("Assign your Canvas's GraphicRaycaster. If left null the script will try to find one at Awake.")]
    public GraphicRaycaster uiRaycaster;

    [Header("Swipe Settings")]
    public float swipeThreshold = 50f;
    public float rotationDuration = 0.25f;
    public float joystickDeadzone = 0.2f;

    [Header("UI elements that should disable swipe")]
    [Tooltip("Drag UI GameObjects here (e.g. your Slider GameObject and your on-screen Joystick GameObject).")]
    public GameObject[] uiElementsToIgnore;

    [Header("How far (px) outside UI should also block swipe")]
    public float uiIgnorePadding = 20f;

    [Header("Pedestal angle rule (Z axis)")]
    [Tooltip("Permitted angle step (degrees). Swipes allowed only when pedestal.Z is a multiple of this value.")]
    public float pedestalAngleStepZ = 10f;

    [Tooltip("Tolerance (degrees) when checking if current pedestal.Z is a multiple of pedestalAngleStep.")]
    public float pedestalAngleToleranceZ = 0.5f;

    [Header("Pedestal angle rule (X axis)")]
    [Tooltip("Permitted angle step (degrees). Swipes allowed only when pedestal.X is a multiple of this value.")]
    public float pedestalAngleStepX = 10f;

    [Tooltip("Tolerance (degrees) when checking if current pedestal.X is a multiple of pedestalAngleStep.")]
    public float pedestalAngleToleranceX = 0.5f;

    [Header("Debug")]
    [Tooltip("Enables logging to the console to help diagnose ignored swipes.")]
    public bool debugMode = false;

    private Vector2 startPos;
    private bool isRotating = false;
    private bool isTouching = false;
    private bool ignoreSwipeThisTouch = false;

    [HideInInspector] public bool forceDisableSwipe = false;

    void Awake()
    {
#if UNITY_2023_1_OR_NEWER
        if (uiRaycaster == null)
            uiRaycaster = FindFirstObjectByType<GraphicRaycaster>();
#else
        if (uiRaycaster == null)
            uiRaycaster = FindObjectOfType<GraphicRaycaster>();
#endif
    }

    void Update()
    {
        // If pedestal angles are not aligned to steps (within tolerance), block swipe entirely
        if (!IsPedestalAnglesAllowed())
        {
            if (debugMode) Debug.Log($"Swipe blocked: pedestal X {GetPedestalX():F2} (step {pedestalAngleStepX}) or Z {GetPedestalZ():F2} (step {pedestalAngleStepZ}) not aligned within tolerances ±{pedestalAngleToleranceX}/{pedestalAngleToleranceZ}");
            return;
        }

        if (isRotating) return;
        if (forceDisableSwipe) return;
        if (IsGamepadActive()) return;

        if (Touchscreen.current == null) return;

        var touch = Touchscreen.current.primaryTouch;

        if (touch.press.isPressed)
        {
            Vector2 currentPos = touch.position.ReadValue();

            if (!isTouching)
            {
                isTouching = true;
                ignoreSwipeThisTouch = false;
                startPos = currentPos;

                if (IsPointOverIgnoredUI(currentPos))
                {
                    ignoreSwipeThisTouch = true;
                    if (debugMode) Debug.Log($"Touch started OVER ignored UI at {currentPos}");
                }
                else
                {
                    if (debugMode) Debug.Log($"Touch started at {currentPos} (not over ignored UI)");
                }
            }
            else
            {
                // ongoing touch: if it ever moves into ignored UI, mark it to ignore
                if (!ignoreSwipeThisTouch && IsPointOverIgnoredUI(currentPos))
                {
                    ignoreSwipeThisTouch = true;
                    if (debugMode) Debug.Log($"Touch moved INTO ignored UI at {currentPos}");
                }
            }

            return;
        }

        // finger released
        if (isTouching && !touch.press.isPressed)
        {
            isTouching = false;
            Vector2 endPos = touch.position.ReadValue();
            Vector2 delta = endPos - startPos;

            if (ignoreSwipeThisTouch)
            {
                if (debugMode) Debug.Log($"Swipe ignored because touch overlapped UI (start:{startPos} end:{endPos})");
                ignoreSwipeThisTouch = false;
                return;
            }

            if (debugMode) Debug.Log($"Swipe delta: {delta} (magnitude {delta.magnitude}) from {startPos} to {endPos}");

            if (delta.magnitude > swipeThreshold)
            {
                // Double-check pedestal angles before starting rotation (extra safeguard)
                if (!IsPedestalAnglesAllowed())
                {
                    if (debugMode) Debug.Log("Blocked rotation at swipe time because pedestal angles are not aligned.");
                    return;
                }

                Vector2 absDelta = new Vector2(Mathf.Abs(delta.x), Mathf.Abs(delta.y));
                if (absDelta.x > absDelta.y)
                {
                    if (delta.x > 0) TryStartRotate(Vector3.up, -90f);
                    else TryStartRotate(Vector3.up, 90f);
                }
                else
                {
                    if (delta.y > 0) TryStartRotate(Vector3.right, 90f);
                    else TryStartRotate(Vector3.right, -90f);
                }
            }
        }
    }

    void TryStartRotate(Vector3 axis, float degrees)
    {
        // Before starting rotation, verify pedestal is allowed (again) and not rotating
        if (isRotating) return;
        if (!IsPedestalAnglesAllowed())
        {
            if (debugMode) Debug.Log("Rotation prevented: pedestal angles not at allowed multiples.");
            return;
        }

        StartCoroutine(RotateByWorldAxis(axis, degrees));
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

        // Snap rotation to exact value to avoid floating point drift (important for modulo checks)
        pedestal.rotation = to;
        NormalizePedestalAnglesToStep(); // snap both X and Z to their nearest steps

        isRotating = false;
    }

    // ---------- helper methods ----------

    /// <summary>
    /// Returns true if the pedestal's Z and X angles are within their tolerances of multiples of their steps.
    /// Both axes must be aligned for swipes to be allowed.
    /// </summary>
    bool IsPedestalAnglesAllowed()
    {
        if (pedestal == null) return true; // if no pedestal assigned, don't block

        bool zOk = IsAngleMultipleWithinTolerance(GetPedestalZ(), pedestalAngleStepZ, pedestalAngleToleranceZ);
        bool xOk = IsAngleMultipleWithinTolerance(GetPedestalX(), pedestalAngleStepX, pedestalAngleToleranceX);
        if (debugMode && (!zOk || !xOk))
        {
            if (!zOk) Debug.Log($"Pedestal Z {GetPedestalZ():F2} fails step {pedestalAngleStepZ} ±{pedestalAngleToleranceZ}");
            if (!xOk) Debug.Log($"Pedestal X {GetPedestalX():F2} fails step {pedestalAngleStepX} ±{pedestalAngleToleranceX}");
        }
        return zOk && xOk;
    }

    /// <summary>
    /// Checks a single axis angle value (0..360) is within tolerance of a multiple of step.
    /// </summary>
    bool IsAngleMultipleWithinTolerance(float angle, float step, float tolerance)
    {
        if (step <= 0f) return true; // if step invalid, don't block
        float nearest = Mathf.Round(angle / step) * step;
        float delta = Mathf.Abs(Mathf.DeltaAngle(angle, nearest));
        return delta <= tolerance;
    }

    /// <summary>
    /// Snaps pedestal X and Z to the nearest multiples of their respective steps (preserving the remaining axis).
    /// Useful to prevent floating-point drift so future checks are accurate.
    /// </summary>
    void NormalizePedestalAnglesToStep()
    {
        if (pedestal == null) return;
        Vector3 e = pedestal.eulerAngles;

        if (pedestalAngleStepX > 0f)
        {
            float nearestX = Mathf.Round(e.x / pedestalAngleStepX) * pedestalAngleStepX;
            e.x = Mathf.Repeat(nearestX, 360f);
        }

        if (pedestalAngleStepZ > 0f)
        {
            float nearestZ = Mathf.Round(e.z / pedestalAngleStepZ) * pedestalAngleStepZ;
            e.z = Mathf.Repeat(nearestZ, 360f);
        }

        pedestal.eulerAngles = e;
        if (debugMode) Debug.Log($"Pedestal snapped to X:{e.x:F2}, Z:{e.z:F2}");
    }

    bool IsPointOverIgnoredUI(Vector2 screenPos)
    {
        // 1) GraphicRaycaster check (fast, accurate)
        if (EventSystem.current != null && uiRaycaster != null)
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = screenPos };
            List<RaycastResult> results = new List<RaycastResult>();
            uiRaycaster.Raycast(pointerData, results);

            if (results.Count > 0)
            {
                if (uiElementsToIgnore != null && uiElementsToIgnore.Length > 0)
                {
                    foreach (var res in results)
                    {
                        GameObject hitGo = res.gameObject;
                        foreach (var ignoreRoot in uiElementsToIgnore)
                        {
                            if (ignoreRoot == null) continue;
                            if (IsChildOfOrSame(hitGo, ignoreRoot))
                            {
                                if (debugMode) Debug.Log($"GraphicRaycaster hit {hitGo.name} which is child of ignored {ignoreRoot.name}");
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    if (debugMode) Debug.Log("GraphicRaycaster hit UI and no specific ignore list -> treating as ignored");
                    return true;
                }
            }
        }

        // 2) RectTransform area check with proper camera (handles ScreenSpaceCamera & World cameras)
        if (uiElementsToIgnore != null && uiElementsToIgnore.Length > 0)
        {
            Camera cam = null;
            Canvas rayCasterCanvas = (uiRaycaster != null) ? uiRaycaster.GetComponent<Canvas>() : null;
            if (rayCasterCanvas != null)
            {
                cam = rayCasterCanvas.renderMode == RenderMode.ScreenSpaceCamera ? rayCasterCanvas.worldCamera : null;
            }

            foreach (var ignoreRoot in uiElementsToIgnore)
            {
                if (ignoreRoot == null) continue;
                RectTransform rt = ignoreRoot.GetComponent<RectTransform>();
                if (rt == null) continue;

                Vector3[] worldCorners = new Vector3[4];
                rt.GetWorldCorners(worldCorners);

                Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(cam, worldCorners[0]);
                Vector2 topRight = RectTransformUtility.WorldToScreenPoint(cam, worldCorners[2]);

                Rect screenRect = Rect.MinMaxRect(bottomLeft.x - uiIgnorePadding, bottomLeft.y - uiIgnorePadding,
                                                  topRight.x + uiIgnorePadding, topRight.y + uiIgnorePadding);

                if (screenRect.Contains(screenPos))
                {
                    if (debugMode) Debug.Log($"Point {screenPos} is inside padded rect of {ignoreRoot.name}: {screenRect}");
                    return true;
                }
            }
        }

        return false;
    }

    bool IsChildOfOrSame(GameObject child, GameObject root)
    {
        if (child == null || root == null) return false;
        if (child == root) return true;
        Transform t = child.transform;
        while (t != null)
        {
            if (t.gameObject == root) return true;
            t = t.parent;
        }
        return false;
    }

    bool IsGamepadActive()
    {
        if (Gamepad.current == null) return false;
        Vector2 left = Gamepad.current.leftStick.ReadValue();
        Vector2 right = Gamepad.current.rightStick.ReadValue();
        return left.magnitude > joystickDeadzone || right.magnitude > joystickDeadzone;
    }

    /// <summary>
    /// Helper: safe pedestal Z getter (0..360)
    /// </summary>
    float GetPedestalZ()
    {
        if (pedestal == null) return 0f;
        return pedestal.eulerAngles.z % 360f;
    }

    /// <summary>
    /// Helper: safe pedestal X getter (0..360)
    /// </summary>
    float GetPedestalX()
    {
        if (pedestal == null) return 0f;
        return pedestal.eulerAngles.x % 360f;
    }
}
