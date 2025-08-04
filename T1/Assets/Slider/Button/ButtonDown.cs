using UnityEngine;

public class ButtonDown : MonoBehaviour
{
    public Transform pedestal;        // Assign the sphere pedestal here
    public float snapAngle = 30f;     // Rotation per click

    private float totalXRotation = 0f;

    public void RotatePedestal()
    {
        if (pedestal == null) return;

        // Accumulate rotation on global X axis (in reverse)
        totalXRotation -= snapAngle;

        // Get current global rotation
        Quaternion currentRotation = pedestal.rotation;

        // Convert to world-space matrix
        Matrix4x4 currentMatrix = Matrix4x4.Rotate(currentRotation);

        // Build rotation only around world X axis (negative angle)
        Quaternion xRotation = Quaternion.AngleAxis(-snapAngle, Vector3.right);
        Matrix4x4 rotationMatrix = Matrix4x4.Rotate(xRotation);

        // Multiply rotation: new = rotationMatrix * currentMatrix
        Matrix4x4 finalMatrix = rotationMatrix * currentMatrix;

        // Apply to pedestal
        pedestal.rotation = finalMatrix.rotation;
    }
}
