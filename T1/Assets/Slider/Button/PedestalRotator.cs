using UnityEngine;

public class PedestalRotator : MonoBehaviour
{
    public Transform pedestal;            // Assign the pedestal (sphere) in the Inspector
    public float snapAngle = 30f;         // How much to rotate per click
    public float rotationSpeed = 360f;    // Degrees per second for smooth motion

    private float remainingAngle = 0f;    // How much is left to rotate in this step
    private bool isRotating = false;

    void Update()
    {
        if (isRotating && pedestal != null)
        {
            // Rotate around WORLD X AXIS
            float rotateStep = rotationSpeed * Time.deltaTime;
            float angleThisFrame = Mathf.Min(rotateStep, remainingAngle);

            pedestal.Rotate(Vector3.right, angleThisFrame, Space.World);
            remainingAngle -= angleThisFrame;

            if (remainingAngle <= 0f)
            {
                isRotating = false;
                remainingAngle = 0f;
            }
        }
    }

    public void RotatePedestal()
    {
        if (!isRotating && pedestal != null)
        {
            remainingAngle = snapAngle;
            isRotating = true;
        }
    }
}
