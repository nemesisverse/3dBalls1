using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PedestalController1 : MonoBehaviour
{
    [Header("References")]
    public Transform pedestal;                // center of sphere / parent transform
    public Transform referencePoint;          // world transform of the reference point on the sphere
    public List<Transform> targetPoints;      // the 42 points (Transforms). If you have world positions, convert them to transforms or fill a list of empty transforms at positions.

    [Header("Rotation Settings")]
    public float snapAngle = 36f;             // initial check range (your example uses 36°)
    public float expansionStep = 10f;         // expand by +10°
    public float expansionCap = 180f;         // stop expanding after this
    public float holdInterval = 0.2f;         // time between repeated rotations while holding (buttons)
    public float ySnapAngle = 30f;            // yaw rotation per click for left/right buttons (unchanged behavior)

    // internal hold state
    float holdTimer = 0f;
    bool isHolding = false;
    System.Action holdAction = null;

    // slider support (optional)
    [Header("Slider (optional)")]
    public Slider zSlider;                    // optional slider that maps 0..1 to 0..360 deg around global Z
    public int sliderSnapPoints = 12;         // how many discrete steps on slider

    void Start()
    {
        if (zSlider != null)
        {
            zSlider.onValueChanged.AddListener(OnSliderValueChanged);
            // initialize slider snapping to current rotation
            SnapAndRotateSlider(zSlider.value, true);
        }
    }

    void Update()
    {
        if (isHolding && holdAction != null)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= holdInterval)
            {
                holdAction.Invoke();
                holdTimer = 0f;
            }
        }
    }

    // ---------- Public UI-callable methods ----------
    // Call these from your UI Buttons (OnPointerDown => BeginHold, OnPointerUp => EndHold; or simple OnClick to call the non-hold version)

    // Rotate around global +X by the 'smart snap' algorithm
    public void RotateX_Positive()
    {
        DoSmartSnap(Vector3.right, snapAngle);
    }
    public void RotateX_Negative()
    {
        DoSmartSnap(Vector3.right, -snapAngle);
    }

    // Rotate around global +Z by the 'smart snap' algorithm
    public void RotateZ_Positive()
    {
        DoSmartSnap(Vector3.forward, snapAngle);
    }
    public void RotateZ_Negative()
    {
        DoSmartSnap(Vector3.forward, -snapAngle);
    }

    // simple yaw left/right (global Y), preserved from your ButtonLeft/ButtonRight
    public void RotateY_Left()
    {
        pedestal.rotation = Quaternion.AngleAxis(ySnapAngle, Vector3.up) * pedestal.rotation;
    }
    public void RotateY_Right()
    {
        pedestal.rotation = Quaternion.AngleAxis(-ySnapAngle, Vector3.up) * pedestal.rotation;
    }

    // Start/stop holding for a specific action (UI pointer down/up)
    public void BeginHold(System.Action action)
    {
        holdAction = action;
        isHolding = true;
        holdTimer = holdInterval; // instant first activation as you did before
        action?.Invoke();
    }
    public void EndHold()
    {
        isHolding = false;
        holdAction = null;
        holdTimer = 0f;
    }

    // ---------- Core smart-snap algorithm ----------
    /// <summary>
    /// Try to snap the reference point to the nearest target using the search rules you described.
    /// </summary>
    /// <param name="initialAxis">Global axis of the initial rotation (Vector3.right or Vector3.forward)</param>
    /// <param name="directionalSnap">The snap angle magnitude with sign (e.g., +36 or -36). The sign's only use is to indicate initial direction — the algorithm checks +/- within range anyway.</param>
    void DoSmartSnap(Vector3 initialAxis, float directionalSnap)
    {
        if (pedestal == null || referencePoint == null || targetPoints == null || targetPoints.Count == 0)
            return;

        // normalize axes to be sure
        initialAxis = initialAxis.normalized;
        Vector3 otherAxis = (initialAxis == Vector3.right) ? Vector3.forward : Vector3.right;

        float searchRange = Mathf.Abs(directionalSnap); // initial 0..36 range
        bool found = false;
        Quaternion rotationToApply = Quaternion.identity;

        // compute current direction vectors from pedestal center to points (world-space)
        Vector3 refDir = (referencePoint.position - pedestal.position).normalized;

        // We'll alternate starting axis->other->axis->other... until found or cap reached
        bool startWithInitial = true;
        while (searchRange <= expansionCap && !found)
        {
            Vector3 axisToCheck = startWithInitial ? initialAxis : otherAxis;
            // iterate all targets and find the one with smallest absolute signed-angle around axis that is within searchRange
            float bestAbsAngle = float.MaxValue;
            float bestSignedAngle = 0f;
            Transform bestTarget = null;

            foreach (var t in targetPoints)
            {
                if (t == null) continue;
                Vector3 targetDir = (t.position - pedestal.position).normalized;
                // signed angle from current refDir to targetDir around axis
                float signed = Vector3.SignedAngle(refDir, targetDir, axisToCheck); // range -180..180
                float absA = Mathf.Abs(signed);

                if (absA <= searchRange)
                {
                    if (absA < bestAbsAngle)
                    {
                        bestAbsAngle = absA;
                        bestSignedAngle = signed;
                        bestTarget = t;
                    }
                }
            }

            if (bestTarget != null)
            {
                // found a target within searchRange around this axis.
                // Apply rotation around axis by bestSignedAngle (this aligns ref->target exactly, rotating only about axisToCheck).
                rotationToApply = Quaternion.AngleAxis(bestSignedAngle, axisToCheck);
                pedestal.rotation = rotationToApply * pedestal.rotation;
                found = true;
                break;
            }

            // no candidate on this axis: switch axis (user-specified alternating), and if we already tried both this loop, expand search range
            if (startWithInitial)
            {
                startWithInitial = false; // next try otherAxis
            }
            else
            {
                startWithInitial = true;  // next main loop will try initialAxis again
                searchRange += expansionStep; // expand search range by 10°
            }
        }

        // If still not found (very unlikely if expansionCap big), fallback: just rotate by the original directionalSnap around the initialAxis
        if (!found)
        {
            pedestal.rotation = Quaternion.AngleAxis(directionalSnap, initialAxis) * pedestal.rotation;
        }
    }

    // ---------- Slider functions ----------
    void OnSliderValueChanged(float value)
    {
        SnapAndRotateSlider(value, false);
    }

    /// <summary>
    /// Snaps slider input into discrete steps and rotates the pedestal around global Z by the snapped amount.
    /// </summary>
    /// <param name="rawValue">slider.value</param>
    /// <param name="forceSnap">if true, set slider value to snapped value immediately</param>
    void SnapAndRotateSlider(float rawValue, bool forceSnap)
    {
        if (zSlider == null || pedestal == null) return;

        if (sliderSnapPoints < 2) sliderSnapPoints = 2;
        float snapInterval = 1f / (sliderSnapPoints - 1);
        int nearestStep = Mathf.RoundToInt(rawValue / snapInterval);
        float snappedValue = nearestStep * snapInterval;

        if (forceSnap) zSlider.SetValueWithoutNotify(snappedValue);
        else zSlider.value = snappedValue; // keep UI in sync

        float angleZ = snappedValue * 360f;

        // We want to rotate the pedestal's global Z to angleZ while preserving X/Y.
        // We'll track current Z angle in world space by projecting forward/up? Simpler: compute delta from a stored currentZAngle.
        // Use difference from current world Z rotation by extracting euler angles on world rotation's Z.
        float currentZ = pedestal.rotation.eulerAngles.z;
        // choose shortest delta
        float deltaZ = Mathf.DeltaAngle(currentZ, angleZ);

        // rotate around global Z by -deltaZ to match previous behaviour that used Vector3.forward, -deltaZ
        pedestal.Rotate(Vector3.forward, -deltaZ, Space.World);
    }

    // ---------- Utility: debug helper to compute angles (optional) ----------
    /// <summary>
    /// Returns the signed angle around axis between current reference direction and a target point.
    /// </summary>
    public float SignedAngleToTarget(Transform target, Vector3 axis)
    {
        if (pedestal == null || referencePoint == null || target == null) return 0f;
        Vector3 refDir = (referencePoint.position - pedestal.position).normalized;
        Vector3 targetDir = (target.position - pedestal.position).normalized;
        return Vector3.SignedAngle(refDir, targetDir, axis);
    }
}
