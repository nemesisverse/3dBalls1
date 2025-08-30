using UnityEngine;

public class ButtonDown : MonoBehaviour
{
    public Transform pedestal;        // Assign the sphere pedestal here
    public float snapAngle = 30f;     // Rotation per click
    public float holdInterval = 0.2f; // Time between rotations while holding

    private float totalXRotation = 0f;
    private bool isHolding = false;
    private float holdTimer = 0f;

    //duplicate sphere
    // public Transform bigSphere;
    // public Transform smallSphere;     // The small sphere on top of the platform
    // public GameObject spherePrefab;   // Prefab of the small sphere to duplicate
    // public float minDistance = 0.1f;

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
        // duplicate sphere 
        // 1. Save current position of the small sphere in world space
        //Vector3 worldPos = smallSphere.position;
        //Quaternion worldRot = smallSphere.rotation;
        //
        //
        //
        //// 2. Instantiate duplicate sphere at that world position
        //GameObject newSphere = Instantiate(spherePrefab, worldPos, worldRot);
        //
        //// 3. Make the duplicate a child of the big sphere
        //newSphere.transform.SetParent(bigSphere);
        //////////////////////////////////////////////////////
        if (pedestal == null) return; //preventing null reference exception

        totalXRotation -= snapAngle;

        Quaternion currentRotation = pedestal.rotation;
        Matrix4x4 currentMatrix = Matrix4x4.Rotate(currentRotation);

        Quaternion xRotation = Quaternion.AngleAxis(-snapAngle, Vector3.right);
        Matrix4x4 rotationMatrix = Matrix4x4.Rotate(xRotation);

        Matrix4x4 finalMatrix = rotationMatrix * currentMatrix;

        pedestal.rotation = finalMatrix.rotation;
    }
}


