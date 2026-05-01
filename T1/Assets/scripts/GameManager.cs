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
    // Assign in Inspector — empty GameObject named "DeletedRing" with DeletedRing component
    public Transform deletedRingContainer;

    // Pauses falling blocks during platform rotation
    public bool isRotating = false;

    // True while a ring-clear + inward-shift cycle is running.
    // Blocks spawning and prevents re-entrant ring checks.
    public bool isProcessingRings = false;

    // Rings whose deletion has been queued; shift fires once DeletedRing empties
    private List<CompletedRing> _pendingShifts = new List<CompletedRing>();

    // ================================================================
    //  INIT
    // ================================================================

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (sphericalGrid == null)
            sphericalGrid = FindFirstObjectByType<SphericalGrid>();

        if (deletedRingContainer == null)
        {
            GameObject found = GameObject.Find("DeletedRing");
            if (found != null)
                deletedRingContainer = found.transform;
            else
                Debug.LogError("[GameManager] No 'DeletedRing' GameObject found in scene! Please create one.");
        }
    }

    // ================================================================
    //  UPDATE — polls DeletedRing.childCount to trigger inward shift
    // ================================================================

    void Update()
    {
        // Only active while a ring-clear cycle is in progress
        if (!isProcessingRings) return;
        if (_pendingShifts.Count == 0) return;
        if (deletedRingContainer == null) return;

        // Still waiting for DeletedRing to empty
        if (deletedRingContainer.childCount > 0) return;

        // ----- All deleted blocks are gone — apply inward shift -----

        Debug.Log("[GameManager] DeletedRing is empty. Applying inward shift.");

        var shiftsToApply = new List<CompletedRing>(_pendingShifts);
        _pendingShifts.Clear();

        ApplyInwardShift(shiftsToApply);

        // Check for chain reactions created by the shift
        List<CompletedRing> chainRings = sphericalGrid.CheckAllRings();
        if (chainRings.Count > 0)
        {
            Debug.Log($"[GameManager] Chain reaction! {chainRings.Count} new ring(s) after shift.");
            ReparentRings(chainRings);
            // Stay in processing state; Update() will re-trigger once DeletedRing empties again
        }
        else
        {
            isProcessingRings = false;
            Debug.Log("[GameManager] Ring processing complete — spawning resumed.");
        }
    }

    // ================================================================
    //  RING DETECTION + REPARENTING
    //  Call this after any block lands.
    // ================================================================

    public void CheckAndDestroyRings()
    {
        // Guard: don't start a new cycle while one is already running
        if (isProcessingRings)
        {
            Debug.Log("[GameManager] CheckAndDestroyRings skipped — already processing rings.");
            return;
        }

        if (deletedRingContainer == null)
        {
            Debug.LogError("[GameManager] deletedRingContainer is null — cannot process rings.");
            return;
        }

        List<CompletedRing> completed = sphericalGrid.CheckAllRings();
        if (completed.Count == 0) return;

        isProcessingRings = true;
        ReparentRings(completed);
    }

    // ================================================================
    //  HELPERS
    // ================================================================

    /// <summary>
    /// For each completed ring: clears its grid cells, reparents the
    /// block GameObjects to DeletedRing, and queues the ring for shifting.
    /// </summary>
    private void ReparentRings(List<CompletedRing> rings)
    {
        foreach (var ring in rings)
        {
            Debug.Log($"<color=green>[GameManager] Ring COMPLETE:</color> {ring}");

            List<GameObject> ringBlocks =
                sphericalGrid.CollectRingBlocks(ring.Plane, ring.RadiusIndex);

            Debug.Log($"[GameManager] Collected {ringBlocks.Count} block(s) from {ring} — reparenting to DeletedRing.");

            foreach (GameObject block in ringBlocks)
            {
                if (block == null) continue;
                block.transform.SetParent(deletedRingContainer, worldPositionStays: true);
            }

            _pendingShifts.Add(ring);
        }

        Debug.Log($"[GameManager] {_pendingShifts.Count} ring shift(s) pending. Waiting for DeletedRing to empty...");
    }

    /// <summary>
    /// Calls ShiftBlocksInwardTetris for every plane that had rings deleted.
    /// A shared HashSet prevents cardinal blocks (present on two planes)
    /// from being repositioned twice.
    /// </summary>
    private void ApplyInwardShift(List<CompletedRing> deletedRings)
    {
        // Build per-plane list of deleted radii
        var deletedRadiiPerPlane = new List<int>[SphericalGrid.PLANE_COUNT];
        for (int i = 0; i < SphericalGrid.PLANE_COUNT; i++)
            deletedRadiiPerPlane[i] = new List<int>();

        foreach (var ring in deletedRings)
            deletedRadiiPerPlane[ring.Plane].Add(ring.RadiusIndex);

        string[] planeNames = { "XY", "YZ", "XZ" };

        // Shared across planes so cardinal blocks aren't double-moved
        var alreadyMoved = new HashSet<GameObject>();

        for (int p = 0; p < SphericalGrid.PLANE_COUNT; p++)
        {
            if (deletedRadiiPerPlane[p].Count == 0) continue;

            Debug.Log($"[GameManager] ApplyInwardShift — {planeNames[p]} " +
                      $"deleted radii: [{string.Join(", ", deletedRadiiPerPlane[p])}]");

            sphericalGrid.ShiftBlocksInwardTetris(p, deletedRadiiPerPlane[p], alreadyMoved);
        }
    }

    // ================================================================
    //  COLLISION CHECK
    // ================================================================

    public bool HasChildAtPosition(Transform parent, Vector3 targetPosition)
    {
        foreach (Transform child in parent)
        {
            Vector3 a = child.position;
            Vector3 b = targetPosition;

            bool xMatch = Mathf.Round(a.x * 100f) == Mathf.Round(b.x * 100f);
            bool yMatch = Mathf.Round(a.y * 100f) == Mathf.Round(b.y * 100f);
            bool zMatch = Mathf.Round(a.z * 100f) == Mathf.Round(b.z * 100f);

            if (xMatch && yMatch && zMatch) return true;
        }
        return false;
    }

    // ================================================================
    //  SPAWNING
    // ================================================================

    public void SpawnRandomObject()
    {
        if (objectsToSpawn.Count == 0) return;
        if (isProcessingRings) return;   // hold off during ring clear cycle

        int idx = UnityEngine.Random.Range(0, objectsToSpawn.Count);
        Instantiate(objectsToSpawn[idx], new Vector3(0f, 16.5f, 0f), Quaternion.identity);
    }
}