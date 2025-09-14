using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PedestalController1 : MonoBehaviour
{
    [Header("References")]
    public Transform pedestal;                // center of sphere / parent transform
    public Transform referencePoint;          // world transform of the reference point on the sphere
    public List<Transform> targetPoints;      // the 42 points (Transforms)

    [Header("Rotation Settings")]
    public float snapAngle = 36f;             // initial check range
    public float expansionStep = 10f;         // expand by +10°
    public float expansionCap = 180f;         // stop expanding after this
    public float holdInterval = 0.2f;         // time between repeated rotations while holding (buttons)
    public float ySnapAngle = 30f;            // yaw rotation per click for left/right buttons (unchanged behavior)

    // internal hold state
    float holdTimer = 0f;
    bool isHolding = false;
    Action holdAction = null;

    // slider support (optional)
    [Header("Slider (optional)")]
    public Slider zSlider;                    // optional slider that maps 0..1 to 0..360 deg around global Z
    public int sliderSnapPoints = 12;         // how many discrete steps on slider

    void Start()
    {
        if (zSlider != null)
        {
            zSlider.onValueChanged.AddListener(OnSliderValueChanged);
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
    // One-shot methods (for OnClick)
    public void RotateX_Positive() => DoSmartSnap(Vector3.right, snapAngle);
    public void RotateX_Negative() => DoSmartSnap(Vector3.right, -snapAngle);

    public void RotateZ_Positive() => DoSmartSnap(Vector3.forward, snapAngle);
    public void RotateZ_Negative() => DoSmartSnap(Vector3.forward, -snapAngle);

    public void RotateY_Left() => pedestal.rotation = Quaternion.AngleAxis(ySnapAngle, Vector3.up) * pedestal.rotation;
    public void RotateY_Right() => pedestal.rotation = Quaternion.AngleAxis(-ySnapAngle, Vector3.up) * pedestal.rotation;

    // ---------- Hold wrappers (call these from EventTrigger PointerDown) ----------
    // X axis (top / down)
    public void BeginHoldX_Positive() => BeginHold(RotateX_Positive);
    public void BeginHoldX_Negative() => BeginHold(RotateX_Negative);

    // Z axis (if you want hold for Z)
    public void BeginHoldZ_Positive() => BeginHold(RotateZ_Positive);
    public void BeginHoldZ_Negative() => BeginHold(RotateZ_Negative);

    // Y axis (left / right)
    public void BeginHoldY_Left() => BeginHold(RotateY_Left);
    public void BeginHoldY_Right() => BeginHold(RotateY_Right);

    // Shared EndHold for PointerUp
    public void EndHold()
    {
        isHolding = false;
        holdAction = null;
        holdTimer = 0f;
    }

    // Internal BeginHold that accepts an Action
    public void BeginHold(Action action)
    {
        holdAction = action;
        isHolding = true;
        holdTimer = holdInterval; // instant first activation
        action?.Invoke();
    }

    // ---------- Core smart-snap algorithm ----------
    void DoSmartSnap(Vector3 initialAxis, float directionalSnap)
    {
        if (pedestal == null || referencePoint == null || targetPoints == null || targetPoints.Count == 0)
            return;

        initialAxis = initialAxis.normalized;
        Vector3 otherAxis = (initialAxis == Vector3.right) ? Vector3.forward : Vector3.right;

        float searchRange = Mathf.Abs(directionalSnap);
        bool found = false;

        Vector3 refDir = (referencePoint.position - pedestal.position).normalized;

        bool startWithInitial = true;
        while (searchRange <= expansionCap && !found)
        {
            Vector3 axisToCheck = startWithInitial ? initialAxis : otherAxis;
            float bestAbsAngle = float.MaxValue;
            float bestSignedAngle = 0f;
            Transform bestTarget = null;

            foreach (var t in targetPoints)
            {
                if (t == null) continue;
                Vector3 targetDir = (t.position - pedestal.position).normalized;
                float signed = Vector3.SignedAngle(refDir, targetDir, axisToCheck); // -180..180
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
                Quaternion rotationToApply = Quaternion.AngleAxis(bestSignedAngle, axisToCheck);
                pedestal.rotation = rotationToApply * pedestal.rotation;
                found = true;
                break;
            }

            if (startWithInitial)
            {
                startWithInitial = false;
            }
            else
            {
                startWithInitial = true;
                searchRange += expansionStep;
            }
        }

        if (!found)
        {
            pedestal.rotation = Quaternion.AngleAxis(directionalSnap, initialAxis) * pedestal.rotation;
        }
    }

    // ---------- Slider functions ----------
    void OnSliderValueChanged(float value) => SnapAndRotateSlider(value, false);

    void SnapAndRotateSlider(float rawValue, bool forceSnap)
    {
        if (zSlider == null || pedestal == null) return;

        if (sliderSnapPoints < 2) sliderSnapPoints = 2;
        float snapInterval = 1f / (sliderSnapPoints - 1);
        int nearestStep = Mathf.RoundToInt(rawValue / snapInterval);
        float snappedValue = nearestStep * snapInterval;

        if (forceSnap) zSlider.SetValueWithoutNotify(snappedValue);
        else zSlider.value = snappedValue;

        float angleZ = snappedValue * 360f;
        float currentZ = pedestal.rotation.eulerAngles.z;
        float deltaZ = Mathf.DeltaAngle(currentZ, angleZ);
        pedestal.Rotate(Vector3.forward, -deltaZ, Space.World);
    }

    // ---------- Utility ----------
    public float SignedAngleToTarget(Transform target, Vector3 axis)
    {
        if (pedestal == null || referencePoint == null || target == null) return 0f;
        Vector3 refDir = (referencePoint.position - pedestal.position).normalized;
        Vector3 targetDir = (target.position - pedestal.position).normalized;
        return Vector3.SignedAngle(refDir, targetDir, axis);
    }
}
