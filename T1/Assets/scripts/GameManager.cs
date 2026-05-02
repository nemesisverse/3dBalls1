using UnityEngine;
using System.Collections;
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
    // Receives coplanar blocks that are OUTER than the completed ring.
    // After all DeletedRing children are destroyed, these blocks shift
    // inward by (ringCount × step), where step = 1.0 for cardinal blocks
    // and 0.707 for diagonal blocks.
    public Transform ringTraversalContainer;

    // Flag to pause falling blocks during rotation
    public bool isRotating = false;

    // Tolerance used to decide whether a world-space coordinate is
    // "effectively zero" (i.e. the block lies on that plane axis).
    private const float ZERO_THRESHOLD = 0.1f;

    // Tolerance for the coplanar flat-axis check.
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

        if (deletedRingContainer == null)
        {
            GameObject found = GameObject.Find("DeletedRing");
            if (found != null)
                deletedRingContainer = found.transform;
            else
                Debug.LogWarning("[GameManager] No 'DeletedRing' GameObject found in scene.");
        }

        if (ringTraversalContainer == null)
        {
            GameObject found = GameObject.Find("RingTraversal");
            if (found != null)
                ringTraversalContainer = found.transform;
            else
                Debug.LogWarning("[GameManager] No 'RingTraversal' GameObject found in scene.");
        }
    }

    // ----------------------------------------------------------------
    //  RING DETECTION + REPARENTING
    // ----------------------------------------------------------------

    /// <summary>
    /// Call this after any block lands / is placed.
    ///
    /// For every completed ring:
    ///   1. Ring blocks                      → reparented to DeletedRing
    ///   2. Coplanar blocks at OUTER radius  → reparented to RingTraversal
    ///      (same plane, radius index less than the ring's index)
    ///
    /// After all DeletedRing children are gone a coroutine shifts every
    /// RingTraversal child inward by (completedRingCount × step), then
    /// reparents them back to motherPlatform.
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

        var alreadyRerouted = new HashSet<GameObject>();

        foreach (var ring in completed)
        {
            Debug.Log($"<color=green>[GameManager] Ring COMPLETE:</color> {ring}");

            List<GameObject> ringBlocks = sphericalGrid.CollectRingBlocks(ring.Plane, ring.RadiusIndex);
            var ringBlockSet = new HashSet<GameObject>(ringBlocks);

            List<Transform> outerCoplanar = GetOuterCoplanarChildren(
                ring.Plane, ring.RadiusIndex, ringBlockSet, alreadyRerouted);

            // Ring blocks → DeletedRing
            foreach (GameObject block in ringBlocks)
            {
                if (block == null) continue;
                block.transform.SetParent(deletedRingContainer, worldPositionStays: true);
                Debug.Log($"[GameManager]   '{block.name}' → DeletedRing");
            }

            // Outer coplanar blocks → RingTraversal
            if (ringTraversalContainer != null)
            {
                foreach (Transform t in outerCoplanar)
                {
                    if (t == null) continue;
                    t.SetParent(ringTraversalContainer, worldPositionStays: true);
                    alreadyRerouted.Add(t.gameObject);
                    Debug.Log($"[GameManager]   '{t.name}' → RingTraversal (outer of ring r={ring.RadiusIndex})");
                }
            }
            else
            {
                Debug.LogWarning("[GameManager] ringTraversalContainer is null — outer coplanar blocks not reparented.");
            }
        }

        // Launch the wait-then-shift coroutine once for all rings cleared this frame.
        // completed.Count is the number of rings simultaneously destroyed.
        StartCoroutine(WaitForDeletionThenShift(completed.Count));
    }

    // ----------------------------------------------------------------
    //  INWARD SHIFT — triggered after DeletedRing is emptied
    // ----------------------------------------------------------------

    /// <summary>
    /// Polls every frame until DeletedRing has no children, then moves
    /// every RingTraversal child inward by (ringCount × step), and finally
    /// reparents them all back to motherPlatform.
    ///
    /// step per block:
    ///   Cardinal block (1 non-zero world axis)  → 1.000 unity unit per ring
    ///   Diagonal block (2 non-zero world axes)  → 0.707 unity units per ring
    ///
    /// "Inward" means toward zero on each non-zero world-space axis:
    ///   positive coordinate → subtract step
    ///   negative coordinate → add    step
    ///   coordinate ≈ 0     → unchanged
    /// </summary>
    private IEnumerator WaitForDeletionThenShift(int ringCount)
    {
        if (deletedRingContainer == null) yield break;

        // Wait until your custom destruction logic has emptied DeletedRing
        while (deletedRingContainer.childCount > 0)
            yield return null;

        ShiftRingTraversalChildrenInward(ringCount);
    }

    /// <summary>
    /// Applies the inward positional shift to all current children of
    /// RingTraversal in world space, then reparents them back to motherPlatform.
    ///
    /// Examples (single ring cleared, ringCount = 1):
    ///   ( 6.009,  6.009, 0) → delta (-0.707, -0.707, 0) → ( 5.302,  5.302, 0)
    ///   (-4.595,  4.595, 0) → delta ( 0.707, -0.707, 0) → (-3.888,  3.888, 0)
    ///   ( 7.000,  0.000, 0) → delta (-1.000,  0.000, 0) → ( 6.000,  0.000, 0)
    ///   ( 0.000, -6.009, 6.009) in YZ plane
    ///                       → delta ( 0.000,  0.707,-0.707) → (0, -5.302, 5.302)
    /// </summary>
    private void ShiftRingTraversalChildrenInward(int ringCount)
    {
        if (ringTraversalContainer == null) return;
        if (ringCount <= 0) return;

        Debug.Log($"[GameManager] Shifting RingTraversal children inward × {ringCount} ring(s).");

        // Snapshot children first — modifying the hierarchy during
        // iteration can cause skips.
        var children = new List<Transform>();
        foreach (Transform child in ringTraversalContainer)
            if (child != null) children.Add(child);

        foreach (Transform child in children)
        {
            if (child == null) continue;

            Vector3 worldPos = child.position;

            // ── Determine block type ─────────────────────────────────
            // Cardinal: exactly 1 world-space axis is non-zero  → step 1.000
            // Diagonal: exactly 2 world-space axes are non-zero → step 0.707
            //
            // We count non-zero axes from the world position.
            // Blocks that were reparented with worldPositionStays=true retain
            // their correct world positions regardless of sphere rotation.
            int nonZeroAxes = 0;
            if (Mathf.Abs(worldPos.x) > ZERO_THRESHOLD) nonZeroAxes++;
            if (Mathf.Abs(worldPos.y) > ZERO_THRESHOLD) nonZeroAxes++;
            if (Mathf.Abs(worldPos.z) > ZERO_THRESHOLD) nonZeroAxes++;

            float step = (nonZeroAxes == 1) ? 1.000f : 0.707f;

            // ── Build per-step inward delta ──────────────────────────
            // Each non-zero coordinate moves toward zero:
            //   positive → subtract step
            //   negative → add    step
            Vector3 stepDelta = Vector3.zero;
            if (Mathf.Abs(worldPos.x) > ZERO_THRESHOLD)
                stepDelta.x = (worldPos.x > 0f) ? -step : step;
            if (Mathf.Abs(worldPos.y) > ZERO_THRESHOLD)
                stepDelta.y = (worldPos.y > 0f) ? -step : step;
            if (Mathf.Abs(worldPos.z) > ZERO_THRESHOLD)
                stepDelta.z = (worldPos.z > 0f) ? -step : step;

            // ── Apply full shift (step × rings cleared simultaneously) ──
            child.position = worldPos + stepDelta * ringCount;

            Debug.Log($"[GameManager]   Shifted '{child.name}' " +
                      $"from {worldPos} " +
                      $"by {stepDelta * ringCount} " +
                      $"(axes={nonZeroAxes}, step={step}, rings={ringCount})");
        }

        // ── Reparent all shifted blocks back to motherPlatform ───────
        if (motherPlatform != null)
        {
            foreach (Transform child in children)
            {
                if (child == null) continue;
                child.SetParent(motherPlatform.transform, worldPositionStays: true);
                Debug.Log($"[GameManager]   '{child.name}' → reparented back to motherPlatform");
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] motherPlatform is null — could not reparent RingTraversal children.");
        }
    }

    // ----------------------------------------------------------------
    //  OUTER COPLANAR CHILD SEARCH
    // ----------------------------------------------------------------

    /// <summary>
    /// Returns direct children of motherPlatform that satisfy ALL of:
    ///   1. Lie on <paramref name="plane"/> (flat coord ≈ 0 in local space)
    ///   2. Are registered in the grid at a radius index LESS THAN
    ///      <paramref name="ringRadiusIndex"/> (further from centre)
    ///   3. Are not in <paramref name="excludeSet"/> (the ring blocks)
    ///   4. Have not yet been moved this frame (<paramref name="alreadyMoved"/>)
    /// </summary>
    private List<Transform> GetOuterCoplanarChildren(
        int plane,
        int ringRadiusIndex,
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

            if (excludeSet.Contains(go)) continue;
            if (alreadyMoved.Contains(go)) continue;

            // ── Plane check ──────────────────────────────────────────
            Vector3 localPos = mp.InverseTransformPoint(child.position);

            bool isOnPlane = plane switch
            {
                SphericalGrid.XY => Mathf.Abs(localPos.z) < PLANE_THRESHOLD,
                SphericalGrid.YZ => Mathf.Abs(localPos.x) < PLANE_THRESHOLD,
                SphericalGrid.XZ => Mathf.Abs(localPos.y) < PLANE_THRESHOLD,
                _                => false
            };

            if (!isOnPlane) continue;

            // ── Radius check ─────────────────────────────────────────
            int blockRadiusIndex = sphericalGrid.GetRadiusIndexForBlock(go);

            if (blockRadiusIndex < 0)
            {
                Debug.LogWarning($"[GameManager] Coplanar block '{go.name}' not in grid — skipping.");
                continue;
            }

            // Smaller index = further from centre = outer
            if (blockRadiusIndex >= ringRadiusIndex)
            {
                Debug.Log($"[GameManager]   Skipping '{go.name}' (blockR={blockRadiusIndex} >= ringR={ringRadiusIndex})");
                continue;
            }

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