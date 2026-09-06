using UnityEngine;

public class SkyboxRotator : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = 1f; // slow drift

    private Material skyboxMaterial;
    private float currentRotation;

    void Start()
    {
        // Instance the material so you're not editing the shared asset
        skyboxMaterial = new Material(RenderSettings.skybox);
        RenderSettings.skybox = skyboxMaterial;
        currentRotation = skyboxMaterial.GetFloat("_Rotation");
    }

    void Update()
    {
        currentRotation += degreesPerSecond * Time.deltaTime;
        if (currentRotation > 360f) currentRotation -= 360f;
        skyboxMaterial.SetFloat("_Rotation", currentRotation);
    }
}