using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // Static reference

    // The reference your prefabs need
    public GameObject motherPlatform;

    void Awake()
    {
        Instance = this;
    }
}