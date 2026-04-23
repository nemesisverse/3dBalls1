using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Spawn Settings")]
    public List<GameObject> objectsToSpawn;
    public Transform spawnPoint;
    public GameObject motherPlatform;

    [Header("Grid System")]
    public SphericalGrid sphericalGrid;

    // Flag to pause falling blocks during rotation
    public bool isRotating = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (sphericalGrid == null)
            sphericalGrid = FindFirstObjectByType<SphericalGrid>();
    }

    // ----------------------------------------------------------------
    //  RING DETECTION — replaces checkRingToDestroy + checkYZRing + checkXZRing
    // ----------------------------------------------------------------

    /// <summary>
    /// Call this after any block is placed. Checks all 3 planes × all radius levels.
    /// </summary>
    public void CheckAndDestroyRings()
    {
        var completed = sphericalGrid.CheckAllRings();
        foreach (var ring in completed)
        {
            Debug.Log($"<color=green> Spherical Grid RING COMPLETE:</color> {ring}");
            //sphericalGrid.DestroyRing(ring.Plane, ring.RadiusIndex);

            // Uncomment if you want blocks above a destroyed ring to fall inward:
            // sphericalGrid.ShiftBlocksInward(ring.Plane, ring.RadiusIndex);
        }
    }

    // ----------------------------------------------------------------
    //  COLLISION CHECK — kept temporarily for TMovement falling logic
    //  TODO: Replace with sphericalGrid.IsOccupied() calls in TMovement
    // ----------------------------------------------------------------

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

    // ----------------------------------------------------------------
    //  SPAWNING
    // ----------------------------------------------------------------

    public void SpawnRandomObject()
    {
        if (objectsToSpawn.Count == 0) return;

        int randomIndex = UnityEngine.Random.Range(0, objectsToSpawn.Count);
        GameObject prefab = objectsToSpawn[randomIndex];
        Vector3 spawnPos = new Vector3(0f, 16.5f, 0f);

        Instantiate(prefab, spawnPos, Quaternion.identity);
    }
}