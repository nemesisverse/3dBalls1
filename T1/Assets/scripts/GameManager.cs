using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // Static reference

    // The reference your prefabs need
    public GameObject motherPlatform;
    public TMovement currentMovementScript;


    void Awake()
    {
        Instance = this;
        // Try to find the component and assign it
 // Search the ENTIRE scene for the TMovement script

    }


}