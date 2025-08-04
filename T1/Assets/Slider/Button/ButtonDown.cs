using UnityEngine;

public class ButtonDown : MonoBehaviour
{
    public Transform pedestal;        // Assign the sphere pedestal here
    public float snapAngle = 30f;     // Rotation per click
    public float holdInterval = 0.2f; // Time between rotations while holding

    private float totalXRotation = 0f;
    private bool isHolding = false;
    private float holdTimer = 0f;

    void Update()
    {
        if (isHolding)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= holdInterval)
            {
                RotatePedestal();
                holdTimer = 0f;
            }
        }
    }

    public void OnHoldButtonDown()
    {
        isHolding = true;
        holdTimer = holdInterval; // Instant first rotation
    }

    public void OnHoldButtonUp()
    {
        isHolding = false;
        holdTimer = 0f;
    }

    public void RotatePedestal()
    {
        if (pedestal == null) return;

        totalXRotation -= snapAngle;

        Quaternion currentRotation = pedestal.rotation;
        Matrix4x4 currentMatrix = Matrix4x4.Rotate(currentRotation);

        Quaternion xRotation = Quaternion.AngleAxis(-snapAngle, Vector3.right);
        Matrix4x4 rotationMatrix = Matrix4x4.Rotate(xRotation);

        Matrix4x4 finalMatrix = rotationMatrix * currentMatrix;

        pedestal.rotation = finalMatrix.rotation;
    }
}
