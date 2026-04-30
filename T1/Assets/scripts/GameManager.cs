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

    [Header("Ring Collection")]
    // Assign this in the Inspector — an empty GameObject named "DeletedRing"
    public Transform deletedRingContainer;

    // Flag to pause falling blocks during rotation
    public bool isRotating = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (sphericalGrid == null)
            sphericalGrid = FindFirstObjectByType<SphericalGrid>();

        // Auto-find DeletedRing if not assigned in Inspector
        if (deletedRingContainer == null)
        {
            GameObject found = GameObject.Find("DeletedRing");
            if (found != null)
                deletedRingContainer = found.transform;
            else
                Debug.LogWarning("[GameManager] No 'DeletedRing' GameObject found in scene. Please create one.");
        }
    }

    // ----------------------------------------------------------------
    //  RING DETECTION + REPARENTING
    // ----------------------------------------------------------------

    /// <summary>
    /// Call this after any block is placed.
    /// Checks all 3 planes × all radius levels.
    /// On completion: moves ring blocks out of motherPlatform and into
    /// the DeletedRing container, then clears those cells from the grid.
    /// </summary>
    public void CheckAndDestroyRings()
    {
        if (deletedRingContainer == null)
        {
            Debug.LogError("[GameManager] deletedRingContainer is null — cannot reparent ring blocks.");
            return;
        }

        var completed = sphericalGrid.CheckAllRings();

        foreach (var ring in completed)
        {
            Debug.Log($"<color=green>Ring COMPLETE:</color> {ring}");

            // Collect the blocks from the grid and clear those cells
            List<GameObject> ringBlocks = sphericalGrid.CollectRingBlocks(ring.Plane, ring.RadiusIndex);

            foreach (GameObject block in ringBlocks)
            {
                if (block == null) continue;

                // Move block out of motherPlatform, preserve world position
                block.transform.SetParent(deletedRingContainer, worldPositionStays: true);

                Debug.Log($"[GameManager] Reparented '{block.name}' → DeletedRing");
            }
        }
    }

    // ----------------------------------------------------------------
    //  COLLISION CHECK
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