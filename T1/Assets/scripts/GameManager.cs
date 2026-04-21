using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Threading;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // Static reference

    [Header("Spawn Settings")]
    public List<GameObject> objectsToSpawn; // Drag your prefabs here in Inspector
    public Transform spawnPoint;            // Optional: Drag a transform here to set spawn location
    public GameObject motherPlatform;

    List<Vector3> minusZYCoordinates = new List<Vector3>();
    List<Vector3> plusZYCoordinates = new List<Vector3>();
    List<Vector3> plusY = new List<Vector3>();
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

    // --- 1. Cardinals (Single Axis) ---
    public List<GameObject> plusYDimension = new List<GameObject>();
    public List<GameObject> minusYDimension = new List<GameObject>();
    public List<GameObject> minusXDimension = new List<GameObject>();
    public List<GameObject> plusXDimension = new List<GameObject>();
    public List<GameObject> plusZDimension = new List<GameObject>();
    public List<GameObject> minusZDimension = new List<GameObject>();

    // --- 2. Y-Z Plane ---
    public List<GameObject> plusYplusZDimension = new List<GameObject>();
    public List<GameObject> plusYminusZDimension = new List<GameObject>();
    public List<GameObject> minusYplusZDimension = new List<GameObject>();
    public List<GameObject> minusYminusZDimension = new List<GameObject>();

    // --- 3. X-Z Plane ---
    public List<GameObject> minusXminusZDimension = new List<GameObject>();
    public List<GameObject> minusXplusZDimension = new List<GameObject>();
    public List<GameObject> plusXminusZDimension = new List<GameObject>();
    public List<GameObject> plusXplusZDimension = new List<GameObject>();

    // --- 4. X-Y Plane ---
    public List<GameObject> minusXplusYDimension = new List<GameObject>();
    public List<GameObject> plusXplusYDimension = new List<GameObject>();
    public List<GameObject> minusXminusYDimension = new List<GameObject>();
    public List<GameObject> plusXminusYDimension = new List<GameObject>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        for (float v = 13.079f; v >= 1.767f - 0.0001f; v -= 0.707f)
        {
            // Y-Z Plane
            minusZYCoordinates.Add(new Vector3(0, v, -v));
            plusYminusZDimension.Add(null);

            plusZYCoordinates.Add(new Vector3(0, v, v));
            plusYplusZDimension.Add(null);

            minusYminusZCoordinate.Add(new Vector3(0, -v, -v));
            minusYminusZDimension.Add(null);

            minusYplusZCoordinate.Add(new Vector3(0, -v, v));
            minusYplusZDimension.Add(null);

            // X-Z Plane
            minusXminusZCoordinate.Add(new Vector3(-v, 0, -v));
            minusXminusZDimension.Add(null);

            miusXplusZCoordinate.Add(new Vector3(-v, 0, v));
            minusXplusZDimension.Add(null);

            plusXminusZCoordinate.Add(new Vector3(v, 0, -v));
            plusXminusZDimension.Add(null);

            plusXplusZCoordinate.Add(new Vector3(v, 0, v));
            plusXplusZDimension.Add(null);

            // X-Y Plane
            minusXplusY.Add(new Vector3(-v, v, 0));
            minusXplusYDimension.Add(null);

            plusXplusY.Add(new Vector3(v, v, 0));
            plusXplusYDimension.Add(null);

            minusXminusY.Add(new Vector3(-v, -v, 0));
            minusXminusYDimension.Add(null);

            plusXminusY.Add(new Vector3(v, -v, 0));
            plusXminusYDimension.Add(null);
        }

        // --- LOOP 2: CARDINALS ---
        for (float v = 18.5f; v >= 2.5f; v -= 1f)
        {
            plusY.Add(new Vector3(0, v, 0));
            plusYDimension.Add(null);

            minusY.Add(new Vector3(0, -v, 0));
            minusYDimension.Add(null);

            minusX.Add(new Vector3(-v, 0, 0));
            minusXDimension.Add(null);

            plusX.Add(new Vector3(v, 0, 0));
            plusXDimension.Add(null);

            plusZ.Add(new Vector3(0, 0, v));
            plusZDimension.Add(null);

            minusZ.Add(new Vector3(0, 0, -v));
            minusZDimension.Add(null);
        }
    }

    public bool HasChildAtPosition(Transform parent, Vector3 targetPosition)
    {
        foreach (Transform child in parent)
        {
            Vector3 a = child.position;
            Vector3 b = targetPosition;

            bool xMatch = Mathf.Round(a.x * 100f) == Mathf.Round(b.x * 100f);
            bool yMatch = Mathf.Round(a.y * 100f) == Mathf.Round(b.y * 100f);
            bool zMatch = Mathf.Round(a.z * 100f) == Mathf.Round(b.z * 100f);

            if (xMatch && yMatch && zMatch)
                return true;
        }
        return false;
    }

    public void checkRingToDestroy()
    {
        for (int i = 16; i >= 0; i--)
        {
            bool isXYRingFull =
                (minusXplusYDimension[i] != null) && (plusXplusYDimension[i] != null) &&
                (minusXminusYDimension[i] != null) && (plusXminusYDimension[i] != null) &&
                (plusYDimension[i] != null) && (minusYDimension[i] != null) &&
                (minusXDimension[i] != null) && (plusXDimension[i] != null);

            if (isXYRingFull)
            {
                Debug.Log($"<color=green>SUCCESS:</color> XY Ring at radius {i} is completed!");

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
        for (int i = 16; i >= 0; i--)
        {
            bool isYZRingFull =
                (plusYDimension[i] != null) && (minusYDimension[i] != null) &&
                (plusZDimension[i] != null) && (minusZDimension[i] != null) &&
                (plusYplusZDimension[i] != null) && (plusYminusZDimension[i] != null) &&
                (minusYplusZDimension[i] != null) && (minusYminusZDimension[i] != null);

            if (isYZRingFull)
            {
                Debug.Log($"<color=cyan>SUCCESS:</color> YZ Ring at radius {i} is completed!");

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
        for (int i = 16; i >= 0; i--)
        {
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

    public void SpawnRandomObject()
    {
        if (objectsToSpawn.Count == 0) return;
        int randomIndex = UnityEngine.Random.Range(0, objectsToSpawn.Count);
        GameObject prefab = objectsToSpawn[randomIndex];

        Vector3 spawnPos = new Vector3(0f, 16.5f, 0f);
        Instantiate(prefab, spawnPos, Quaternion.identity);
    }
}