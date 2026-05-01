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
    // Assign in Inspector — empty GameObject named "DeletedRing"
    public Transform deletedRingContainer;

    // Assign in Inspector — empty GameObject named "RingTraversal"
    // Receives all non-ring motherPlatform children that lie on the
    // same local plane as a just-completed ring.
    public Transform ringTraversalContainer;

    // Flag to pause falling blocks during rotation
    public bool isRotating = false;

    // Tolerance (local units) used to decide whether a block's
    // "flat" coordinate is close enough to zero to be on a plane.
    // Cardinal/diagonal grid positions are exact multiples, so 0.1 is safe.
    private const float PLANE_THRESHOLD = 0.1f;

    // ----------------------------------------------------------------
    //  INIT
    // ----------------------------------------------------------------

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (sphericalGrid == null)
            sphericalGrid = FindFirstObjectByType<SphericalGrid>();

        // Auto-find containers if not assigned in Inspector
        if (deletedRingContainer == null)
        {
            GameObject found = GameObject.Find("DeletedRing");
            if (found != null)
                deletedRingContainer = found.transform;
            else
                Debug.LogWarning("[GameManager] No 'DeletedRing' GameObject found in scene. Please create one.");
        }

        if (ringTraversalContainer == null)
        {
            GameObject found = GameObject.Find("RingTraversal");
            if (found != null)
                ringTraversalContainer = found.transform;
            else
                Debug.LogWarning("[GameManager] No 'RingTraversal' GameObject found in scene. Please create one.");
        }
    }

    // ----------------------------------------------------------------
    //  RING DETECTION + REPARENTING
    // ----------------------------------------------------------------

    /// <summary>
    /// Call this after any block lands / is placed.
    ///
    /// For every completed ring:
    ///   1. Ring blocks        → reparented to DeletedRing
    ///   2. Coplanar blocks    → reparented to RingTraversal
    ///      (all other motherPlatform direct children whose local
    ///       "flat" coordinate is ≈ 0 for the ring's plane,
    ///       excluding the ring blocks themselves)
    /// </summary>
    public void CheckAndDestroyRings()
    {
        if (deletedRingContainer == null)
        {
            Debug.LogError("[GameManager] deletedRingContainer is null — cannot process rings.");
            return;
        }

        List<CompletedRing> completed = sphericalGrid.CheckAllRings();
        if (completed.Count == 0) return;

        // Track blocks already sent to RingTraversal so that multiple
        // rings completing on different planes in the same frame don't
        // double-reparent a shared cardinal block.
        var alreadyRerouted = new HashSet<GameObject>();

        foreach (var ring in completed)
        {
            Debug.Log($"<color=green>[GameManager] Ring COMPLETE:</color> {ring}");

            // --- Step 1: collect ring blocks, clear them from the grid ---
            List<GameObject> ringBlocks = sphericalGrid.CollectRingBlocks(ring.Plane, ring.RadiusIndex);
            var ringBlockSet = new HashSet<GameObject>(ringBlocks);

            // --- Step 2: snapshot coplanar children BEFORE reparenting ring blocks ---
            // (ring blocks are still children of motherPlatform at this point)
            List<Transform> coplanar = GetCoplanarChildren(ring.Plane, ringBlockSet, alreadyRerouted);

            // --- Step 3: send ring blocks → DeletedRing ---
            foreach (GameObject block in ringBlocks)
            {
                if (block == null) continue;
                block.transform.SetParent(deletedRingContainer, worldPositionStays: true);
                Debug.Log($"[GameManager]   '{block.name}' → DeletedRing");
            }

            // --- Step 4: send coplanar blocks → RingTraversal ---
            if (ringTraversalContainer != null)
            {
                foreach (Transform t in coplanar)
                {
                    if (t == null) continue;
                    t.SetParent(ringTraversalContainer, worldPositionStays: true);
                    alreadyRerouted.Add(t.gameObject);
                    Debug.Log($"[GameManager]   '{t.name}' → RingTraversal");
                }
            }
            else
            {
                Debug.LogWarning("[GameManager] ringTraversalContainer is null — coplanar blocks not reparented.");
            }
        }
    }

    // ----------------------------------------------------------------
    //  COPLANAR CHILD SEARCH
    // ----------------------------------------------------------------

    /// <summary>
    /// Returns all direct children of motherPlatform whose local position
    /// has its "flat" component ≈ 0 for the given plane, excluding any
    /// block already listed in <paramref name="excludeSet"/> or
    /// <paramref name="alreadyMoved"/>.
    ///
    /// Plane membership (in motherPlatform's local space):
    ///   XY (plane 0) → localPos.z ≈ 0
    ///   YZ (plane 1) → localPos.x ≈ 0
    ///   XZ (plane 2) → localPos.y ≈ 0
    /// </summary>
    private List<Transform> GetCoplanarChildren(
        int plane,
        HashSet<GameObject> excludeSet,
        HashSet<GameObject> alreadyMoved)
    {
        var result = new List<Transform>();

        if (motherPlatform == null)
        {
            Debug.LogWarning("[GameManager] motherPlatform is null — cannot find coplanar blocks.");
            return result;
        }

        Transform mp = motherPlatform.transform;

        foreach (Transform child in mp)
        {
            if (child == null) continue;
            GameObject go = child.gameObject;

            // Skip the ring blocks themselves
            if (excludeSet.Contains(go)) continue;

            // Skip blocks already dispatched to RingTraversal this frame
            if (alreadyMoved.Contains(go)) continue;

            // Convert world position → motherPlatform local space
            Vector3 localPos = mp.InverseTransformPoint(child.position);

            bool isOnPlane = plane switch
            {
                SphericalGrid.XY => Mathf.Abs(localPos.z) < PLANE_THRESHOLD,
                SphericalGrid.YZ => Mathf.Abs(localPos.x) < PLANE_THRESHOLD,
                SphericalGrid.XZ => Mathf.Abs(localPos.y) < PLANE_THRESHOLD,
                _                => false
            };

            if (isOnPlane)
                result.Add(child);
        }

        return result;
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