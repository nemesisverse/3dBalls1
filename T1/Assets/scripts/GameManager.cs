using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using NUnit.Framework;
using System;

public class GameManager : MonoBehaviour
{

    public static GameManager Instance; // Static reference
    // The reference your prefabs need
    public GameObject motherPlatform;
   

    [System.Obsolete]
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
            // 1. Get positions
            Vector3 a = child.position;
            Vector3 b = targetPosition;

            // 2. Check X, Y, and Z individually
            // We multiply by 100 and round to convert 5.123 -> 512
            bool xMatch = Mathf.Round(a.x * 100f) == Mathf.Round(b.x * 100f);
            bool yMatch = Mathf.Round(a.y * 100f) == Mathf.Round(b.y * 100f);
            bool zMatch = Mathf.Round(a.z * 100f) == Mathf.Round(b.z * 100f);

            // 3. If all 3 match, return true
            if (xMatch && yMatch && zMatch)
            {
                return true;
            }
        }
        return false;
    }

    // public bool HasChildAtPosition(Transform parent, Vector3 targetPosition)
    // {
    //     foreach (Transform child in parent)
    //     {
    //         if (child.position == targetPosition)
    //         {
    //             return true;
    //         }
    //     }
    //     return false;
    // }

 


}