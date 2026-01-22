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
   List<Vector3> minusZYCoordinates = new List<Vector3>();
   List<Vector3> plusZYCoordinates = new List<Vector3>();

   List<Vector3> plusYCoordinate = new List<Vector3>();

   List<Vector3> minusYCoordinate = new List<Vector3>();

   List<Vector3> minusXminusZCoordinate = new List<Vector3>();

   List<Vector3> miusXplusZCoordinate = new List<Vector3>();
   List<Vector3> plusXminusZCoordinate   = new List<Vector3>();
   
   List<Vector3> plusXplusZCoordinate   = new List<Vector3>();

   List<Vector3> minusYminusZCoordinate = new List<Vector3>();

   List<Vector3> minusYplusZCoordinate   = new List<Vector3>();

   List<Vector3> minusX = new List<Vector3>();

   List<Vector3> plusX = new List<Vector3>();
   List<Vector3> plusZ = new List<Vector3>();

   List<Vector3> minusZ = new List<Vector3>();

   List<Vector3> minusY = new List<Vector3>();

   List<Vector3> minusXplusY  = new List<Vector3>();

   List<Vector3> plusXplusY  = new List<Vector3>();

   List<Vector3> minusXminusY  = new List<Vector3>();

   List<Vector3> plusXminusY  = new List<Vector3>();

   

    void Awake()
    {
        Instance = this;

        for (float v = 10.251f; v >= 1.767f - 0.0001f; v -= 0.707f)
    {
        // Y-Z Plane
        minusZYCoordinates.Add(new Vector3(0, v, -v));      // Up-Back
        plusZYCoordinates.Add(new Vector3(0, v, v));        // Up-Forward
        minusYminusZCoordinate.Add(new Vector3(0, -v, -v)); // Down-Back
        minusYplusZCoordinate.Add(new Vector3(0, -v, v));   // Down-Forward

        // X-Z Plane
        minusXminusZCoordinate.Add(new Vector3(-v, 0, -v)); // Left-Back
        miusXplusZCoordinate.Add(new Vector3(-v, 0, v));    // Left-Forward
        plusXminusZCoordinate.Add(new Vector3(v, 0, -v));   // Right-Back
        plusXplusZCoordinate.Add(new Vector3(v, 0, v));     // Right-Forward

        // X-Y Plane
        minusXplusY.Add(new Vector3(-v, v, 0));             // Left-Up
        plusXplusY.Add(new Vector3(v, v, 0));               // Right-Up
        minusXminusY.Add(new Vector3(-v, -v, 0));           // Left-Down
        plusXminusY.Add(new Vector3(v, -v, 0));             // Right-Down
    }

    // --- LOOP 2: CARDINALS (Step 1.0) ---
    // Single axis directions (Up, Down, Left, Right, Forward, Back)
    for (float v = 14.5f; v >= 2.5f; v -= 1f)
    {
        plusYCoordinate.Add(new Vector3(0, v, 0));  // Up
        minusY.Add(new Vector3(0, -v, 0));          // Down
        
        minusX.Add(new Vector3(-v, 0, 0));          // Left
        plusX.Add(new Vector3(v, 0, 0));            // Right
        
        plusZ.Add(new Vector3(0, 0, v));            // Forward
        minusZ.Add(new Vector3(0, 0, -v));          // Back
    }
        // for (float v = 10.251f; v >= 1.767f - 0.0001f; v -= 0.707f)
        // {
        //     minusZYCoordinates.Add(new Vector3(0, v, -v));

        // }
        // for (float v = 10.251f; v >= 1.767f - 0.0001f; v -= 0.707f)
        // {
        //     plusZYCoordinates.Add(new Vector3(0, v, v));

        // }
        
       
        // for (float v = 10.251f; v >= 1.767f - 0.0001f; v -= 0.707f)
        // {
        //     minusXminusZCoordinate.Add(new Vector3(-v, 0, -v));

        // }
        // for (float v = 10.251f; v >= 1.767f - 0.0001f; v -= 0.707f)
        // {
        //     miusXplusZCoordinate.Add(new Vector3(-v, 0, v));

        // }
        // for (float v = 10.251f; v >= 1.767f - 0.0001f; v -= 0.707f)
        // {
        //     plusXminusZCoordinate.Add(new Vector3(v, 0, -v));

        // }
        // for (float v = 10.251f; v >= 1.767f - 0.0001f; v -= 0.707f)
        // {
        //     plusXplusZCoordinate.Add(new Vector3(v, 0, v));

        // }
        // for (float v = 10.251f; v >= 1.767f - 0.0001f; v -= 0.707f)
        // {
        //     minusYminusZCoordinate.Add(new Vector3(0, -v, -v));

        // }
        // for (float v = 10.251f; v >= 1.767f - 0.0001f; v -= 0.707f)
        // {
        //     minusYplusZCoordinate.Add(new Vector3(0, -v, v));

        // }

        // // single axis coordinates
        // for (float y = 14.5f; y >= 2.5f; y -= 1f)
        // {
        //     plusYCoordinate.Add(new Vector3(0f, y, 0f));
        // }
        // for(float y = 14.5f; y >= 2.5f; y -= 1f)
        // {
        //     minusY.Add(new Vector3(0f, -y, 0f));
        // }
        // for(float x = 14.5f ; x >= 2.5f; x -= 1f)
        // {
        //     minusX.Add(new Vector3(-x, 0, 0));
        // }
        // for(float x = 14.5f ; x >= 2.5f; x -= 1f)
        // {
        //     plusX.Add(new Vector3(x, 0, 0));
        // }
        // for(float z = 14.5f ; z >= 2.5f; z -= 1f)
        // {
        //     plusZ.Add(new Vector3(0, 0, z));
        // }
        // for(float z = 14.5f ; z >= 2.5f; z -= 1f)
        // {
        //     minusZ.Add(new Vector3(0, 0, -z));
        // }
        


        // for(float v = 10.251f; v >= 1.767f - 0.0001f; v -= 0.707f)
        // {
        //     minusXplusY.Add(new Vector3(-v, v, 0));
        // }
        // for(float v = 10.251f; v >= 1.767f - 0.0001f; v -= 0.707f)
        // {
        //     plusXplusY.Add(new Vector3(v, v, 0));
        // }

        // for(float v = 10.251f; v >= 1.767f - 0.0001f; v -= 0.707f)
        // {
        //     minusXminusY.Add(new Vector3(-v, -v, 0));
        // }

        // for(float v = 10.251f; v >= 1.767f - 0.0001f; v -= 0.707f)
        // {
        //     plusXminusY.Add(new Vector3(v, -v, 0));
        // }




        
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