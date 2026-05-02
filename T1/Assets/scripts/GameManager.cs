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
    public Transform deletedRingContainer;
    public Transform ringTraversalContainer;

    public bool isRotating = false;

    // True for the entire duration of the ring-clear pipeline
    // (including any chain reactions). Rotation input and new
    // block spawning must be gated behind this flag.
    public bool isProcessingRings = false;

    private const float ZERO_THRESHOLD  = 0.1f;
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
            if (found != null) deletedRingContainer = found.transform;
            else Debug.LogWarning("[GameManager] No 'DeletedRing' GameObject found in scene.");
        }

        if (ringTraversalContainer == null)
        {
            GameObject found = GameObject.Find("RingTraversal");
            if (found != null) ringTraversalContainer = found.transform;
            else Debug.LogWarning("[GameManager] No 'RingTraversal' GameObject found in scene.");
        }
    }

    // ----------------------------------------------------------------
    //  ENTRY POINT — call after any block lands
    // ----------------------------------------------------------------

    /// <summary>
    /// Kicks off the full ring-clear pipeline, including unlimited chain
    /// reactions. The pipeline runs until a complete pass finds zero
    /// completed rings. isProcessingRings stays true for the entire
    /// duration — no rotation input or spawning should happen while it
    /// is set.
    /// </summary>
    public void CheckAndDestroyRings()
    {
        if (deletedRingContainer == null)
        {
            Debug.LogError("[GameManager] deletedRingContainer is null.");
            return;
        }

        // Quick pre-check so we don't start a coroutine on every block land
        List<CompletedRing> initial = sphericalGrid.CheckAllRings();
        if (initial.Count == 0) return;

        isProcessingRings = true;
        StartCoroutine(RingClearPipeline(initial));
    }

    // ----------------------------------------------------------------
    //  CHAIN-REACTION PIPELINE
    // ----------------------------------------------------------------

    /// <summary>
    /// Core loop — runs one pass per iteration, chains until no rings remain:
    ///
    ///   1. For every completed ring this pass:
    ///        a. Ring blocks           → DeletedRing
    ///        b. Outer coplanar blocks → RingTraversal
    ///   2. Wait until DeletedRing is empty (your custom destruction runs here).
    ///   3. Shift every RingTraversal child inward, update the grid,
    ///      reparent back to motherPlatform.
    ///   4. Yield one frame so Unity propagates the new transforms.
    ///   5. Re-check for newly completed rings.
    ///        Rings found  → loop back to step 1  (chain reaction continues)
    ///        No rings     → exit, clear isProcessingRings
    /// </summary>
    private IEnumerator RingClearPipeline(List<CompletedRing> firstPass)
    {
        List<CompletedRing> completed = firstPass;
        int chainDepth = 0;

        while (completed.Count > 0)
        {
            chainDepth++;
            Debug.Log($"<color=cyan>[GameManager] Chain pass {chainDepth} — {completed.Count} ring(s).</color>");

            var alreadyRerouted = new HashSet<GameObject>();

            // ── Step 1: reparent ring blocks + outer coplanar blocks ─────
            foreach (var ring in completed)
            {
                Debug.Log($"<color=green>[GameManager] Ring COMPLETE:</color> {ring}");

                List<GameObject> ringBlocks = sphericalGrid.CollectRingBlocks(ring.Plane, ring.RadiusIndex);
                var ringBlockSet = new HashSet<GameObject>(ringBlocks);

                List<Transform> outerCoplanar = GetOuterCoplanarChildren(
                    ring.Plane, ring.RadiusIndex, ringBlockSet, alreadyRerouted);

                foreach (GameObject block in ringBlocks)
                {
                    if (block == null) continue;
                    block.transform.SetParent(deletedRingContainer, worldPositionStays: true);
                    Debug.Log($"[GameManager]   '{block.name}' → DeletedRing");
                }

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
                    Debug.LogWarning("[GameManager] ringTraversalContainer is null.");
                }
            }

            // ── Step 2: wait for your custom destruction to empty DeletedRing
            Debug.Log("[GameManager] Waiting for DeletedRing to empty...");
            while (deletedRingContainer.childCount > 0)
                yield return null;

            // ── Step 3: shift inward, sync grid, reparent to motherPlatform
            ShiftRingTraversalChildrenInward(completed.Count);

            // ── Step 4: one frame so Unity propagates the new transforms ─
            yield return null;

            // ── Step 5: check for chain-reaction rings ───────────────────
            completed = sphericalGrid.CheckAllRings();

            if (completed.Count > 0)
                Debug.Log($"<color=yellow>[GameManager] Chain reaction! {completed.Count} new ring(s).</color>");
            else
                Debug.Log("<color=green>[GameManager] No further chain — pipeline complete.</color>");
        }

        isProcessingRings = false;
        Debug.Log($"[GameManager] Pipeline done after {chainDepth} pass(es).");
    }

    // ----------------------------------------------------------------
    //  INWARD SHIFT
    // ----------------------------------------------------------------

    private void ShiftRingTraversalChildrenInward(int ringCount)
    {
        if (ringTraversalContainer == null) return;
        if (ringCount <= 0) return;
        if (motherPlatform == null)
        {
            Debug.LogWarning("[GameManager] motherPlatform is null — cannot shift.");
            return;
        }

        Debug.Log($"[GameManager] Shifting RingTraversal children inward × {ringCount}.");

        Transform mp = motherPlatform.transform;

        // Snapshot — modifying hierarchy during iteration causes skips
        var children = new List<Transform>();
        foreach (Transform child in ringTraversalContainer)
            if (child != null) children.Add(child);

        foreach (Transform child in children)
        {
            if (child == null) continue;

            // LOCAL space — world space is incorrect after sphere rotation
            Vector3 localPos = mp.InverseTransformPoint(child.position);

            int nonZeroAxes = 0;
            if (Mathf.Abs(localPos.x) > ZERO_THRESHOLD) nonZeroAxes++;
            if (Mathf.Abs(localPos.y) > ZERO_THRESHOLD) nonZeroAxes++;
            if (Mathf.Abs(localPos.z) > ZERO_THRESHOLD) nonZeroAxes++;

            float step = (nonZeroAxes == 1) ? 1.000f : 0.707f;

            Vector3 localDelta = Vector3.zero;
            if (Mathf.Abs(localPos.x) > ZERO_THRESHOLD)
                localDelta.x = (localPos.x > 0f) ? -step : step;
            if (Mathf.Abs(localPos.y) > ZERO_THRESHOLD)
                localDelta.y = (localPos.y > 0f) ? -step : step;
            if (Mathf.Abs(localPos.z) > ZERO_THRESHOLD)
                localDelta.z = (localPos.z > 0f) ? -step : step;

            child.position += mp.TransformDirection(localDelta * ringCount);

            Debug.Log($"[GameManager]   Shifted '{child.name}' " +
                      $"localPos={localPos} delta={localDelta * ringCount} " +
                      $"(axes={nonZeroAxes}, step={step})");

            sphericalGrid.ShiftBlockInward(child.gameObject, ringCount);
        }

        foreach (Transform child in children)
        {
            if (child == null) continue;
            child.SetParent(mp, worldPositionStays: true);
            Debug.Log($"[GameManager]   '{child.name}' → motherPlatform");
        }
    }

    // ----------------------------------------------------------------
    //  OUTER COPLANAR CHILD SEARCH
    // ----------------------------------------------------------------

    private List<Transform> GetOuterCoplanarChildren(
        int plane,
        int ringRadiusIndex,
        HashSet<GameObject> excludeSet,
        HashSet<GameObject> alreadyMoved)
    {
        var result = new List<Transform>();

        if (motherPlatform == null)
        {
            Debug.LogWarning("[GameManager] motherPlatform is null.");
            return result;
        }

        Transform mp = motherPlatform.transform;

        foreach (Transform child in mp)
        {
            if (child == null) continue;
            GameObject go = child.gameObject;

            if (excludeSet.Contains(go)) continue;
            if (alreadyMoved.Contains(go)) continue;

            Vector3 localPos = mp.InverseTransformPoint(child.position);

            bool isOnPlane = plane switch
            {
                SphericalGrid.XY => Mathf.Abs(localPos.z) < PLANE_THRESHOLD,
                SphericalGrid.YZ => Mathf.Abs(localPos.x) < PLANE_THRESHOLD,
                SphericalGrid.XZ => Mathf.Abs(localPos.y) < PLANE_THRESHOLD,
                _                => false
            };

            if (!isOnPlane) continue;

            int blockRadiusIndex = sphericalGrid.GetRadiusIndexForBlock(go);

            if (blockRadiusIndex < 0)
            {
                Debug.LogWarning($"[GameManager] '{go.name}' not in grid — skipping.");
                continue;
            }

            // Smaller index = further from centre = outer = needs to shift in
            if (blockRadiusIndex >= ringRadiusIndex)
            {
                Debug.Log($"[GameManager]   Skip '{go.name}' (r={blockRadiusIndex} >= ringR={ringRadiusIndex})");
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

            if (Mathf.Round(a.x * 100f) == Mathf.Round(b.x * 100f) &&
                Mathf.Round(a.y * 100f) == Mathf.Round(b.y * 100f) &&
                Mathf.Round(a.z * 100f) == Mathf.Round(b.z * 100f))
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
        Instantiate(prefab, new Vector3(0f, 16.5f, 0f), Quaternion.identity);
    }
}