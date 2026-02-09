using UnityEngine;
using System.Collections.Generic;

using System.Collections;
using System.Threading;
using NUnit.Framework;
using System;

public class GameManager : MonoBehaviour
{

    public static GameManager Instance; // Static reference

    [Header("Spawn Settings")]
    public List<GameObject> objectsToSpawn; // Drag your prefabs here in Inspector
    public Transform spawnPoint;            // Optional: Drag a transform here to set spawn location
    // The reference your prefabs need
    public GameObject motherPlatform;//
    List<Vector3> minusZYCoordinates = new List<Vector3>();
    List<Vector3> plusZYCoordinates = new List<Vector3>();

    List<Vector3> plusY = new List<Vector3>();

    //List<Vector3> minusYCoordinate = new List<Vector3>();

    List<Vector3> minusXminusZCoordinate = new List<Vector3>();

    List<Vector3> miusXplusZCoordinate = new List<Vector3>();
    List<Vector3> plusXminusZCoordinate = new List<Vector3>();

    List<Vector3> plusXplusZCoordinate = new List<Vector3>();

    List<Vector3> minusYminusZCoordinate = new List<Vector3>();

    List<Vector3> minusYplusZCoordinate = new List<Vector3>();

    List<Vector3> minusX = new List<Vector3>();

    List<Vector3> plusX = new List<Vector3>();
    List<Vector3> plusZ = new List<Vector3>();

    List<Vector3> minusZ = new List<Vector3>();

    List<Vector3> minusY = new List<Vector3>();

    List<Vector3> minusXplusY = new List<Vector3>();

    List<Vector3> plusXplusY = new List<Vector3>();

    List<Vector3> minusXminusY = new List<Vector3>();

    List<Vector3> plusXminusY = new List<Vector3>();


    /// /////////////////////////////////////////////////




    // --- 1. Cardinals (Single Axis) ---
    public List<GameObject> plusYDimension = new List<GameObject>();  // Up
    public List<GameObject> minusYDimension = new List<GameObject>(); // Down
    public List<GameObject> minusXDimension = new List<GameObject>(); // Left
    public List<GameObject> plusXDimension = new List<GameObject>();  // Right
    public List<GameObject> plusZDimension = new List<GameObject>();  // Forward
    public List<GameObject> minusZDimension = new List<GameObject>(); // Back

    // --- 2. Y-Z Plane (Up/Down + Fwd/Back) ---
    public List<GameObject> plusYplusZDimension = new List<GameObject>();  // Up-Forward
    public List<GameObject> plusYminusZDimension = new List<GameObject>(); // Up-Back (This matches your minusZplusY example)
    public List<GameObject> minusYplusZDimension = new List<GameObject>(); // Down-Forward
    public List<GameObject> minusYminusZDimension = new List<GameObject>();// Down-Back

    // --- 3. X-Z Plane (Left/Right + Fwd/Back) ---
    public List<GameObject> minusXminusZDimension = new List<GameObject>(); // Left-Back
    public List<GameObject> minusXplusZDimension = new List<GameObject>();  // Left-Forward
    public List<GameObject> plusXminusZDimension = new List<GameObject>();  // Right-Back
    public List<GameObject> plusXplusZDimension = new List<GameObject>();   // Right-Forward

    // --- 4. X-Y Plane (Left/Right + Up/Down) ---
    public List<GameObject> minusXplusYDimension = new List<GameObject>();  // Left-Up
    public List<GameObject> plusXplusYDimension = new List<GameObject>();   // Right-Up
    public List<GameObject> minusXminusYDimension = new List<GameObject>(); // Left-Down
    public List<GameObject> plusXminusYDimension = new List<GameObject>();  // Right-Down





    void Awake()
    {
        //Instance = this;
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        for (float v = 10.251f; v >= 1.767f - 0.0001f; v -= 0.707f)
        {
            // Y-Z Plane
            minusZYCoordinates.Add(new Vector3(0, v, -v));      // Up-Back
            plusYminusZDimension.Add(null);


            plusZYCoordinates.Add(new Vector3(0, v, v));        // Up-Forward
            plusYplusZDimension.Add(null);

            minusYminusZCoordinate.Add(new Vector3(0, -v, -v)); // Down-Back
            minusYminusZDimension.Add(null);

            minusYplusZCoordinate.Add(new Vector3(0, -v, v));   // Down-Forward
            minusYplusZDimension.Add(null);

            // X-Z Plane
            minusXminusZCoordinate.Add(new Vector3(-v, 0, -v)); // Left-Back
            minusXminusZDimension.Add(null);

            miusXplusZCoordinate.Add(new Vector3(-v, 0, v));    // Left-Forward
            minusXplusZDimension.Add(null);

            plusXminusZCoordinate.Add(new Vector3(v, 0, -v));   // Right-Back
            plusXminusZDimension.Add(null);


            plusXplusZCoordinate.Add(new Vector3(v, 0, v));     // Right-Forward
            plusXplusZDimension.Add(null);

            // X-Y Plane
            minusXplusY.Add(new Vector3(-v, v, 0));             // Left-Up
            minusXplusYDimension.Add(null);

            plusXplusY.Add(new Vector3(v, v, 0));               // Right-Up
            plusXplusYDimension.Add(null);


            minusXminusY.Add(new Vector3(-v, -v, 0));           // Left-Down
            minusXminusYDimension.Add(null);

            plusXminusY.Add(new Vector3(v, -v, 0));             // Right-Down
            plusXminusYDimension.Add(null);

        }

        // --- LOOP 2: CARDINALS (Step 1.0) ---
        // Single axis directions (Up, Down, Left, Right, Forward, Back)
        for (float v = 14.5f; v >= 2.5f; v -= 1f)
        {
            plusY.Add(new Vector3(0, v, 0));  // Up
            plusYDimension.Add(null);

            minusY.Add(new Vector3(0, -v, 0));          // Down
            minusYDimension.Add(null);

            minusX.Add(new Vector3(-v, 0, 0));          // Left
            minusXDimension.Add(null);

            plusX.Add(new Vector3(v, 0, 0));            // Right
            plusXDimension.Add(null);

            plusZ.Add(new Vector3(0, 0, v));            // Forward
            plusZDimension.Add(null);

            minusZ.Add(new Vector3(0, 0, -v));          // Back
            minusZDimension.Add(null);
        }

    }

    // void Start()
    // {
    //     // Start the repeating check when the game begins
    //     StartCoroutine(RingCheckRoutine());
    // }

    // IEnumerator RingCheckRoutine()
    // {
    //     while (true)
    //     {
    //         checkRingToDestroy();
    //         // Wait for 0.1 seconds (checks 10 times per second)
    //         yield return new WaitForSeconds(0.1f);
    //     }
    // }

//     // In GameManager.cs (if you have access to modify it)
// public void RegisterBlockPlacement(GameObject block)
// {
//     // Your existing placement logic...
    
//     // Invalidate all active TMovement caches
//     TMovement[] allMovements = FindObjectsByType<TMovement>(FindObjectsSortMode.None);
//     foreach (var movement in allMovements)
//     {
//         movement.InvalidatePositionCache();
//     }
// }
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

    public void checkRingToDestroy()
    {
        // Ensure we don't go out of bounds of your lists
        //int checkCount = 13; 

        for (int i = 12; i >= 0; i--)
        {
            // 1. Check if slots are NOT null AND the GameObjects actually exist in the scene
            // We use '&& dimension[i]' as a shorthand for 'is not null and not destroyed'
            bool isXYRingFull =
                (minusXplusYDimension[i] != null) && (plusXplusYDimension[i] != null) &&
                (minusXminusYDimension[i] != null) && (plusXminusYDimension[i] != null) &&
                (plusYDimension[i] != null) && (minusYDimension[i] != null) &&
                (minusXDimension[i] != null) && (plusXDimension[i] != null);

            if (isXYRingFull)
            {
                Debug.Log($"<color=green>SUCCESS:</color> XY Ring at radius {i} is completed!");

                // Additional verification - count non-null entries
                int count = 0;
                if (plusYDimension[i] != null) count++;
                if (minusYDimension[i] != null) count++;
                if (minusXDimension[i] != null) count++;
                if (plusXDimension[i] != null) count++;
                if (minusXplusYDimension[i] != null) count++;
                if (plusXplusYDimension[i] != null) count++;
                if (minusXminusYDimension[i] != null) count++;
                if (plusXminusYDimension[i] != null) count++;

                Debug.Log($"Actual count of filled slots: {count}/8");
            }
        }
    }

public void checkYZRingToDestroy()
{
    for (int i = 12; i >= 0; i--)
    {
        // Cardinals: Up, Down, Forward, Back
        // Diagonals: Top-Forward, Top-Back, Bottom-Forward, Bottom-Back
        bool isYZRingFull =
            (plusYDimension[i] != null) && (minusYDimension[i] != null) &&
            (plusZDimension[i] != null) && (minusZDimension[i] != null) &&
            (plusYplusZDimension[i] != null) && (plusYminusZDimension[i] != null) &&
            (minusYplusZDimension[i] != null) && (minusYminusZDimension[i] != null);

        if (isYZRingFull)
        {
            Debug.Log($"<color=cyan>SUCCESS:</color> YZ Ring at radius {i} is completed!");
            
            // Optional: Detailed verification count
            int count = 0;
            if (plusYDimension[i] != null) count++;
            if (minusYDimension[i] != null) count++;
            if (plusZDimension[i] != null) count++;
            if (minusZDimension[i] != null) count++;
            if (plusYplusZDimension[i] != null) count++;
            if (plusYminusZDimension[i] != null) count++;
            if (minusYplusZDimension[i] != null) count++;
            if (minusYminusZDimension[i] != null) count++;

            Debug.Log($"YZ Ring {i} filled slots: {count}/8");
        }
    }
}
public void checkXZRingToDestroy()
{
    for (int i = 12; i >= 0; i--)
    {
        // Cardinals: Right, Left, Forward, Back
        // Diagonals: Front-Right, Front-Left, Back-Right, Back-Left
        bool isXZRingFull =
            (plusXDimension[i] != null) && (minusXDimension[i] != null) &&
            (plusZDimension[i] != null) && (minusZDimension[i] != null) &&
            (plusXplusZDimension[i] != null) && (minusXplusZDimension[i] != null) &&
            (plusXminusZDimension[i] != null) && (minusXminusZDimension[i] != null);

        if (isXZRingFull)
        {
            Debug.Log($"<color=magenta>SUCCESS:</color> XZ Ring at radius {i} is completed!");
            
            int count = 0;
            if (plusXDimension[i] != null) count++;
            if (minusXDimension[i] != null) count++;
            if (plusZDimension[i] != null) count++;
            if (minusZDimension[i] != null) count++;
            if (plusXplusZDimension[i] != null) count++;
            if (minusXplusZDimension[i] != null) count++;
            if (plusXminusZDimension[i] != null) count++;
            if (minusXminusZDimension[i] != null) count++;

            Debug.Log($"XZ Ring {i} filled slots: {count}/8");
        }
    }
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

    public void SpawnRandomObject()
    {
        if (objectsToSpawn.Count == 0) return;
        // 1. Pick a random index
        // explicit 'UnityEngine.Random' to avoid ambiguity
        int randomIndex = UnityEngine.Random.Range(0, objectsToSpawn.Count);
        GameObject prefab = objectsToSpawn[randomIndex];

        // 2. Define the specific position
        Vector3 spawnPos = new Vector3(0f, 16.5f, 0f);

        // 3. Instantiate at (0, 16.5, 0) with default rotation
        Instantiate(prefab, spawnPos, Quaternion.identity);
    }


    


}