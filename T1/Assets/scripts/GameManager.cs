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
    public Transform deletedRingContainer;   // "DeletedRing"  empty GameObject
    public Transform ringTraversalContainer; // "RingTraversal" empty GameObject

    public bool isRotating = false;

    // ── Thresholds ───────────────────────────────────────────────────

    // Local-space flat-axis tolerance (grid values are exact, 0.1 is safe)
    private const float PLANE_THRESHOLD = 0.1f;

    // A world-space coordinate must exceed this to count as non-zero
    // when deciding cardinal vs diagonal. Min real coord = 1.767, so 0.5 is safe.
    private const float COORD_NONZERO_THRESHOLD = 0.5f;

    private const float CARDINAL_STEP = 1.000f;
    private const float DIAGONAL_STEP = 0.707f;

    // ================================================================
    //  INIT
    // ================================================================

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (sphericalGrid == null)
            sphericalGrid = FindFirstObjectByType<SphericalGrid>();

        AutoFind(ref deletedRingContainer,   "DeletedRing");
        AutoFind(ref ringTraversalContainer, "RingTraversal");
    }

    void AutoFind(ref Transform field, string goName)
    {
        if (field != null) return;
        GameObject go = GameObject.Find(goName);
        if (go != null) field = go.transform;
        else Debug.LogWarning($"[GameManager] '{goName}' not found in scene.");
    }

    // ================================================================
    //  RING DETECTION + REPARENTING + INWARD MOVEMENT
    // ================================================================

    /// <summary>
    /// Call after any block is placed.
    ///
    /// Per completed ring:
    ///   • Ring blocks      → DeletedRing     (cleared from grid)
    ///   • Co-planar blocks → RingTraversal   (cleared from grid)
    ///
    /// Then ALL RingTraversal children (new AND previously reparented)
    /// are shifted inward for every ring whose plane they match and
    /// whose radius is smaller than their own distance from origin.
    ///
    ///   Cardinal slot (one non-zero active coord)  → ±1.000 world units
    ///   Diagonal slot (two non-zero active coords) → ±0.707 world units
    ///   Sign: −sign(coord)  →  always moves toward origin
    /// </summary>
    public void CheckAndDestroyRings()
    {
        if (deletedRingContainer == null)
        {
            Debug.LogError("[GameManager] deletedRingContainer is null — aborting.");
            return;
        }

        List<CompletedRing> completed = sphericalGrid.CheckAllRings();
        if (completed.Count == 0) return;

        var alreadyRerouted = new HashSet<GameObject>();

        foreach (var ring in completed)
        {
            Debug.Log($"<color=green>[GameManager] Ring COMPLETE:</color> {ring}");

            // ── Collect ring blocks, clear from grid ─────────────────
            List<GameObject> ringBlocks = sphericalGrid.CollectRingBlocks(ring.Plane, ring.RadiusIndex);
            var ringBlockSet = new HashSet<GameObject>(ringBlocks);

            // ── Find co-planar siblings still in motherPlatform ──────
            List<Transform> coplanar = GetCoplanarChildren(ring.Plane, ringBlockSet, alreadyRerouted);

            // ── Ring blocks → DeletedRing ────────────────────────────
            foreach (GameObject block in ringBlocks)
            {
                if (block == null) continue;
                block.transform.SetParent(deletedRingContainer, worldPositionStays: true);
                Debug.Log($"[GameManager]   '{block.name}' → DeletedRing");
            }

            // ── Co-planar blocks → RingTraversal ─────────────────────
            if (ringTraversalContainer != null)
            {
                foreach (Transform t in coplanar)
                {
                    if (t == null) continue;
                    sphericalGrid.ClearBlockFromGrid(t.gameObject); // prevent ghost rings
                    t.SetParent(ringTraversalContainer, worldPositionStays: true);
                    alreadyRerouted.Add(t.gameObject);
                    Debug.Log($"[GameManager]   '{t.name}' → RingTraversal");
                }
            }
            else
            {
                Debug.LogWarning("[GameManager] ringTraversalContainer is null.");
            }
        }

        // ── Shift inward: ALL current RingTraversal children ─────────
        // This includes blocks reparented just now AND blocks already
        // sitting in RingTraversal from earlier ring deletions.
        // The previous approach only tracked the current batch in a
        // local Dictionary, so older residents were never moved — fixed here.
        ApplyRingTraversalMovement(completed);
    }

    // ================================================================
    //  INWARD MOVEMENT  —  operates on ALL RingTraversal children
    // ================================================================

    /// <summary>
    /// For every completed ring, shifts every child of ringTraversalContainer
    /// that satisfies BOTH conditions:
    ///
    ///   1. Lies on the same plane as the ring
    ///      (checked in motherPlatform local-space via InverseTransformPoint
    ///       so it stays correct even when motherPlatform is rotated)
    ///
    ///   2. Is farther from origin than the deleted ring's radius
    ///      (blocks closer to centre than the gap are unaffected)
    ///
    /// Steps from multiple qualifying rings accumulate before the position
    /// is written, so a block outside two deleted rings moves two steps.
    /// </summary>
    private void ApplyRingTraversalMovement(List<CompletedRing> completedRings)
    {
        if (ringTraversalContainer == null || ringTraversalContainer.childCount == 0) return;
        if (motherPlatform == null) return;

        Transform mp = motherPlatform.transform;

        // Snapshot the child list so the loop is safe even if something
        // else modifies the hierarchy during iteration.
        var children = new List<Transform>();
        foreach (Transform child in ringTraversalContainer)
            if (child != null) children.Add(child);

        if (children.Count == 0) return;

        // Accumulate per-child movement vectors before writing positions.
        var movementMap = new Dictionary<Transform, Vector3>(children.Count);
        foreach (Transform child in children)
            movementMap[child] = Vector3.zero;

        foreach (var ring in completedRings)
        {
            // Actual 3-D distance from origin at this radius index.
            // Cardinal and diagonal magnitudes are equal at the same index
            // by construction, so one value covers both slot types.
            float ringRadius = sphericalGrid.GetRingRadius(ring.RadiusIndex);

            foreach (Transform child in children)
            {
                // ── Plane check (rotation-safe) ──────────────────────
                // Convert world position back to motherPlatform local space.
                // This is identical to what GetCoplanarChildren does, so the
                // same block that qualified there will qualify here.
                Vector3 local = mp.InverseTransformPoint(child.position);

                bool isOnPlane = ring.Plane switch
                {
                    SphericalGrid.XY => Mathf.Abs(local.z) < PLANE_THRESHOLD,
                    SphericalGrid.YZ => Mathf.Abs(local.x) < PLANE_THRESHOLD,
                    SphericalGrid.XZ => Mathf.Abs(local.y) < PLANE_THRESHOLD,
                    _                => false
                };
                if (!isOnPlane) continue;

                // ── Radial filter ─────────────────────────────────────
                // position.magnitude is the true distance from origin.
                // motherPlatform only rotates (never translates from origin),
                // so world-space magnitude equals spherical radius correctly.
                float blockRadius = child.position.magnitude;
                if (blockRadius <= ringRadius)
                {
                    Debug.Log($"[GameManager]   Skip '{child.name}' — " +
                              $"blockR={blockRadius:F3} ≤ ringR={ringRadius:F3}");
                    continue;
                }

                // ── Accumulate one inward step ────────────────────────
                Vector3 step = ComputeInwardStep(child.position, ring.Plane);
                movementMap[child] += step;

                Debug.Log($"[GameManager]   '{child.name}' +step {step} " +
                          $"(plane={ring.Plane} blockR={blockRadius:F2} ringR={ringRadius:F2})");
            }
        }

        // ── Write positions ───────────────────────────────────────────
        foreach (var kvp in movementMap)
        {
            if (kvp.Value == Vector3.zero) continue;
            kvp.Key.position += kvp.Value;
            Debug.Log($"[GameManager]   '{kvp.Key.name}' final → {kvp.Key.position}");
        }
    }

    /// <summary>
    /// One inward step for a block at <paramref name="worldPos"/>
    /// lying on <paramref name="plane"/>.
    ///
    /// Active axes per plane:
    ///   XY → X, Y  (Z stays 0)
    ///   YZ → Y, Z  (X stays 0)
    ///   XZ → X, Z  (Y stays 0)
    ///
    /// Step size per active axis:
    ///   1 non-zero coord (cardinal) → CARDINAL_STEP (1.000)
    ///   2 non-zero coords (diagonal) → DIAGONAL_STEP (0.707)
    ///
    /// Direction: −sign(coord) × step  →  always toward origin.
    /// </summary>
    private Vector3 ComputeInwardStep(Vector3 worldPos, int plane)
    {
        float a0 = 0f, a1 = 0f;
        switch (plane)
        {
            case SphericalGrid.XY: a0 = worldPos.x; a1 = worldPos.y; break;
            case SphericalGrid.YZ: a0 = worldPos.y; a1 = worldPos.z; break;
            case SphericalGrid.XZ: a0 = worldPos.x; a1 = worldPos.z; break;
        }

        bool a0Big = Mathf.Abs(a0) > COORD_NONZERO_THRESHOLD;
        bool a1Big = Mathf.Abs(a1) > COORD_NONZERO_THRESHOLD;

        int   nonZero  = (a0Big ? 1 : 0) + (a1Big ? 1 : 0);
        float stepSize = (nonZero == 1) ? CARDINAL_STEP : DIAGONAL_STEP;

        float d0 = a0Big ? -Mathf.Sign(a0) * stepSize : 0f;
        float d1 = a1Big ? -Mathf.Sign(a1) * stepSize : 0f;

        return plane switch
        {
            SphericalGrid.XY => new Vector3(d0, d1, 0f),
            SphericalGrid.YZ => new Vector3(0f, d0, d1),
            SphericalGrid.XZ => new Vector3(d0, 0f, d1),
            _                => Vector3.zero
        };
    }

    // ================================================================
    //  CO-PLANAR CHILD SEARCH
    // ================================================================

    private List<Transform> GetCoplanarChildren(
        int                 plane,
        HashSet<GameObject> excludeSet,
        HashSet<GameObject> alreadyMoved)
    {
        var result = new List<Transform>();
        if (motherPlatform == null) return result;

        Transform mp = motherPlatform.transform;

        foreach (Transform child in mp)
        {
            if (child == null) continue;
            var go = child.gameObject;
            if (excludeSet.Contains(go) || alreadyMoved.Contains(go)) continue;

            Vector3 local = mp.InverseTransformPoint(child.position);

            bool onPlane = plane switch
            {
                SphericalGrid.XY => Mathf.Abs(local.z) < PLANE_THRESHOLD,
                SphericalGrid.YZ => Mathf.Abs(local.x) < PLANE_THRESHOLD,
                SphericalGrid.XZ => Mathf.Abs(local.y) < PLANE_THRESHOLD,
                _                => false
            };

            if (onPlane) result.Add(child);
        }

        return result;
    }

    // ================================================================
    //  COLLISION CHECK
    // ================================================================

    public bool HasChildAtPosition(Transform parent, Vector3 targetPosition)
    {
        foreach (Transform child in parent)
        {
            Vector3 a = child.position, b = targetPosition;
            if (Mathf.Round(a.x * 100f) == Mathf.Round(b.x * 100f) &&
                Mathf.Round(a.y * 100f) == Mathf.Round(b.y * 100f) &&
                Mathf.Round(a.z * 100f) == Mathf.Round(b.z * 100f))
                return true;
        }
        return false;
    }

    // ================================================================
    //  SPAWNING
    // ================================================================

    public void SpawnRandomObject()
    {
        if (objectsToSpawn.Count == 0) return;
        int idx = UnityEngine.Random.Range(0, objectsToSpawn.Count);
        Instantiate(objectsToSpawn[idx], new Vector3(0f, 16.5f, 0f), Quaternion.identity);
    }
}