using UnityEngine;

public class SkyboxRotator : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = 0.5f;
    [SerializeField] private Camera targetCamera; // drag Main Camera here

    private Skybox skyboxComponent;
    private Material skyboxMaterial;
    private float currentRotation;

    void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;

        skyboxComponent = targetCamera.GetComponent<Skybox>();
        if (skyboxComponent == null || skyboxComponent.material == null)
        {
            Debug.LogError("No Skybox component or Custom Skybox material found on target camera.");
            return;
        }

        // Instance it so we don't edit the shared asset
        skyboxMaterial = new Material(skyboxComponent.material);
        skyboxComponent.material = skyboxMaterial;

        if (skyboxMaterial.HasProperty("_Rotation"))
            currentRotation = skyboxMaterial.GetFloat("_Rotation");
    }

    void Update()
    {
        if (skyboxMaterial == null || !skyboxMaterial.HasProperty("_Rotation")) return;

        currentRotation += degreesPerSecond * Time.deltaTime;
        if (currentRotation > 360f) currentRotation -= 360f;
        skyboxMaterial.SetFloat("_Rotation", currentRotation);
    }
}