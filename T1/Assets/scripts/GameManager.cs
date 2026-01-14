using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using NUnit.Framework;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // Static reference

    // The reference your prefabs need
    public GameObject motherPlatform;






    void Awake()
    {
        Instance = this;
        // Try to find the component and assign it
        // Search the ENTIRE scene for the TMovement script


    }
    public bool HasChildAtPosition(Transform parent, Vector3 targetPosition)
    {
        foreach (Transform child in parent)
        {
            if (child.position == targetPosition)
            {
                return true;
            }
        }
        return false;
    }



}