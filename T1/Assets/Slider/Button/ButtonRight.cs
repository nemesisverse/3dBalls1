using UnityEngine;

public class ButtonRight : MonoBehaviour
{
    public Transform pedestal;        // Assign the sphere pedestal here
    public float snapAngle = 30f;     // Rotation per click
    public float holdInterval = 0.2f; // Time between rotations while holding

    private float totalYRotation = 0f;
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

        totalYRotation -= snapAngle;

        Quaternion currentRotation = pedestal.rotation;
        Matrix4x4 currentMatrix = Matrix4x4.Rotate(currentRotation);

        // Rotate around global Y axis (left turn)
        Quaternion yRotation = Quaternion.AngleAxis(-snapAngle, Vector3.up);
        Matrix4x4 rotationMatrix = Matrix4x4.Rotate(yRotation);

        Matrix4x4 finalMatrix = rotationMatrix * currentMatrix;

        pedestal.rotation = finalMatrix.rotation;
    }
}
