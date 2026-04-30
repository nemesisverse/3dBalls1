using System.Collections.Generic;
using UnityEngine;

public class SphericalGrid : MonoBehaviour
{
    public const int PLANE_COUNT = 3;
    public const int SLOTS_PER_RING = 8;
    public const int RADIUS_LEVELS = 17;

    public const int XY = 0;
    public const int YZ = 1;
    public const int XZ = 2;

    private GameObject[,,] grid;
    private Vector3[,,] positions;

    private float[] diagonalRadii;
    private float[] cardinalRadii;

    // Raw (non-normalized) directions for each (plane, slot)
    private static readonly Vector3[,] rawDirections = new Vector3[,]
    {
        // XY plane (normal = Z)
        {
            new Vector3( 1, 0, 0),   // 0: +X
            new Vector3( 1, 1, 0),   // 1: +X+Y
            new Vector3( 0, 1, 0),   // 2: +Y
            new Vector3(-1, 1, 0),   // 3: -X+Y
            new Vector3(-1, 0, 0),   // 4: -X
            new Vector3(-1,-1, 0),   // 5: -X-Y
            new Vector3( 0,-1, 0),   // 6: -Y
            new Vector3( 1,-1, 0),   // 7: +X-Y
        },
        // YZ plane (normal = X)
        {
            new Vector3(0,  1, 0),   // 0: +Y
            new Vector3(0,  1, 1),   // 1: +Y+Z
            new Vector3(0,  0, 1),   // 2: +Z
            new Vector3(0, -1, 1),   // 3: -Y+Z
            new Vector3(0, -1, 0),   // 4: -Y
            new Vector3(0, -1,-1),   // 5: -Y-Z
            new Vector3(0,  0,-1),   // 6: -Z
            new Vector3(0,  1,-1),   // 7: +Y-Z
        },
        // XZ plane (normal = Y)
        {
            new Vector3( 1, 0, 0),   // 0: +X
            new Vector3( 1, 0, 1),   // 1: +X+Z
            new Vector3( 0, 0, 1),   // 2: +Z
            new Vector3(-1, 0, 1),   // 3: -X+Z
            new Vector3(-1, 0, 0),   // 4: -X
            new Vector3(-1, 0,-1),   // 5: -X-Z
            new Vector3( 0, 0,-1),   // 6: -Z
            new Vector3( 1, 0,-1),   // 7: +X-Z
        }
    };

    // Pre-normalized for fast dot-product matching
    private Vector3[,] normalizedDirections;

    void Awake()
    {
        BuildRadii();
        BuildNormalizedDirections();
        BuildPositions();
        grid = new GameObject[PLANE_COUNT, SLOTS_PER_RING, RADIUS_LEVELS];
    }

    // ================================================================
    //  SETUP
    // ================================================================

    void BuildRadii()
    {
        var diagList = new List<float>();
        for (float v = 13.079f; v >= 1.767f - 0.0001f; v -= 0.707f)
            diagList.Add(v);
        diagonalRadii = diagList.ToArray();

        var cardList = new List<float>();
        for (float v = 18.5f; v >= 2.5f; v -= 1f)
            cardList.Add(v);
        cardinalRadii = cardList.ToArray();

        if (diagonalRadii.Length != RADIUS_LEVELS || cardinalRadii.Length != RADIUS_LEVELS)
            Debug.LogWarning($"Radius count mismatch! Diagonal={diagonalRadii.Length}, Cardinal={cardinalRadii.Length}, Expected={RADIUS_LEVELS}");
    }

    void BuildNormalizedDirections()
    {
        normalizedDirections = new Vector3[PLANE_COUNT, SLOTS_PER_RING];
        for (int p = 0; p < PLANE_COUNT; p++)
            for (int s = 0; s < SLOTS_PER_RING; s++)
                normalizedDirections[p, s] = rawDirections[p, s].normalized;
    }

    void BuildPositions()
    {
        positions = new Vector3[PLANE_COUNT, SLOTS_PER_RING, RADIUS_LEVELS];

        for (int p = 0; p < PLANE_COUNT; p++)
        {
            for (int s = 0; s < SLOTS_PER_RING; s++)
            {
                bool isDiagonal = (s % 2 == 1);
                float[] radii = isDiagonal ? diagonalRadii : cardinalRadii;
                Vector3 rawDir = rawDirections[p, s];

                for (int r = 0; r < RADIUS_LEVELS; r++)
                {
                    float v = radii[r];
                    positions[p, s, r] = new Vector3(rawDir.x * v, rawDir.y * v, rawDir.z * v);
                }
            }
        }
    }

    // ================================================================
    //  CORE API
    // ================================================================

    public GameObject GetBlock(int plane, int slot, int radiusIndex)
    {
        if (!IsValidCell(plane, slot, radiusIndex)) return null;
        return grid[plane, slot, radiusIndex];
    }

    public void ClearCell(int plane, int slot, int radiusIndex)
    {
        if (!IsValidCell(plane, slot, radiusIndex)) return;
        grid[plane, slot, radiusIndex] = null;
    }

    public Vector3 GetPosition(int plane, int slot, int radiusIndex)
    {
        return positions[plane, slot, radiusIndex];
    }

    public bool IsOccupied(int plane, int slot, int radiusIndex)
    {
        if (!IsValidCell(plane, slot, radiusIndex)) return false;
        return grid[plane, slot, radiusIndex] != null;
    }

    bool IsValidCell(int p, int s, int r)
    {
        return p >= 0 && p < PLANE_COUNT &&
               s >= 0 && s < SLOTS_PER_RING &&
               r >= 0 && r < RADIUS_LEVELS;
    }

    // ================================================================
    //  POSITION-AWARE PLACEMENT
    // ================================================================

    public bool PlaceBlockByWorldPosition(Vector3 blockWorldPos, int radiusIndex,
        GameObject block, Transform motherPlatform)
    {
        if (!IsValidCell(0, 0, radiusIndex)) return false;

        Vector3 localPos = motherPlatform.InverseTransformPoint(blockWorldPos);
        Vector3 localDir = localPos.normalized;

        bool placedAny = false;
        string[] planeNames = { "XY", "YZ", "XZ" };

        for (int p = 0; p < PLANE_COUNT; p++)
        {
            float bestDot = -1f;
            int bestSlot = -1;

            for (int s = 0; s < SLOTS_PER_RING; s++)
            {
                float dot = Vector3.Dot(localDir, normalizedDirections[p, s]);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestSlot = s;
                }
            }

            if (bestDot > 0.95f && bestSlot >= 0 && grid[p, bestSlot, radiusIndex] == null)
            {
                grid[p, bestSlot, radiusIndex] = block;
                placedAny = true;
                Debug.Log($"[Grid] Placed on {planeNames[p]} slot {bestSlot} radius {radiusIndex} (dot={bestDot:F3})");
            }
        }

        if (!placedAny)
            Debug.LogWarning($"[Grid] FAILED to place at radius {radiusIndex}, localDir={localDir}");

        return placedAny;
    }

    public bool PlaceVerticalBlockByPosition(Vector3 block0WorldPos, Vector3 block1WorldPos,
        int radiusIndex, GameObject block0, GameObject block1, Transform motherPlatform)
    {
        bool placed0 = PlaceBlockByWorldPosition(block0WorldPos, radiusIndex, block0, motherPlatform);
        bool placed1 = PlaceBlockByWorldPosition(block1WorldPos, radiusIndex - 1, block1, motherPlatform);
        return placed0 && placed1;
    }

    // ================================================================
    //  RING DETECTION
    // ================================================================

    public List<CompletedRing> CheckAllRings()
    {
        var completed = new List<CompletedRing>();
        string[] planeNames = { "XY", "YZ", "XZ" };

        for (int p = 0; p < PLANE_COUNT; p++)
        {
            for (int r = 0; r < RADIUS_LEVELS; r++)
            {
                int filled = 0;
                for (int s = 0; s < SLOTS_PER_RING; s++)
                    if (grid[p, s, r] != null) filled++;

                if (filled > 0)
                    Debug.Log($"[Ring] {planeNames[p]} radius {r}: {filled}/{SLOTS_PER_RING}");

                if (filled == SLOTS_PER_RING)
                    completed.Add(new CompletedRing(p, r));
            }
        }

        return completed;
    }

    public bool IsRingComplete(int plane, int radiusIndex)
    {
        for (int s = 0; s < SLOTS_PER_RING; s++)
            if (grid[plane, s, radiusIndex] == null)
                return false;
        return true;
    }

    // ================================================================
    //  RING COLLECTION — reparenting variant (no Destroy)
    //
    //  Gathers unique blocks from the completed ring, clears ALL
    //  grid entries referencing those blocks (including shared cardinal
    //  slots on other planes), and returns the block list so the
    //  caller can reparent them to "DeletedRing".
    // ================================================================

    /// <summary>
    /// Collects the unique GameObjects that fill the ring at [plane, radiusIndex],
    /// removes every grid reference to those objects (across all planes),
    /// and returns the list for reparenting.
    /// Does NOT destroy anything — the caller decides what to do next.
    /// </summary>
    public List<GameObject> CollectRingBlocks(int plane, int radiusIndex)
    {
        // Step 1: gather unique blocks from this ring
        var uniqueBlocks = new HashSet<GameObject>();
        for (int s = 0; s < SLOTS_PER_RING; s++)
        {
            GameObject block = grid[plane, s, radiusIndex];
            if (block != null)
                uniqueBlocks.Add(block);   // HashSet deduplicates shared cardinal blocks
        }

        // Step 2: clear every grid cell that references any of these blocks
        // (cardinal blocks appear in 2 planes, so we must sweep all planes)
        foreach (GameObject block in uniqueBlocks)
            ClearBlockFromAllPlanes(block);

        Debug.Log($"[Grid] CollectRingBlocks — plane={plane} radius={radiusIndex} → {uniqueBlocks.Count} unique blocks removed from grid");

        return new List<GameObject>(uniqueBlocks);
    }

    // ================================================================
    //  RING DESTRUCTION (kept for reference — uses Destroy)
    // ================================================================

    public void DestroyRing(int plane, int radiusIndex)
    {
        for (int s = 0; s < SLOTS_PER_RING; s++)
        {
            GameObject block = grid[plane, s, radiusIndex];
            if (block != null)
            {
                ClearBlockFromAllPlanes(block);
                Destroy(block);
            }
        }
    }

    void ClearBlockFromAllPlanes(GameObject block)
    {
        for (int p = 0; p < PLANE_COUNT; p++)
            for (int s = 0; s < SLOTS_PER_RING; s++)
                for (int r = 0; r < RADIUS_LEVELS; r++)
                    if (grid[p, s, r] == block)
                        grid[p, s, r] = null;
    }

    public void ShiftBlocksInward(int plane, int destroyedRadius)
    {
        for (int r = destroyedRadius; r > 0; r--)
        {
            for (int s = 0; s < SLOTS_PER_RING; s++)
            {
                grid[plane, s, r] = grid[plane, s, r - 1];
                grid[plane, s, r - 1] = null;

                if (grid[plane, s, r] != null)
                    grid[plane, s, r].transform.position = positions[plane, s, r];
            }
        }
    }

    // ================================================================
    //  DEBUG
    // ================================================================

    public void DebugLogGridState()
    {
        string[] planeNames = { "XY", "YZ", "XZ" };
        for (int p = 0; p < PLANE_COUNT; p++)
        {
            for (int r = 0; r < RADIUS_LEVELS; r++)
            {
                int filled = 0;
                string slotInfo = "";
                for (int s = 0; s < SLOTS_PER_RING; s++)
                {
                    if (grid[p, s, r] != null) { filled++; slotInfo += $"[{s}]"; }
                    else slotInfo += "[ ]";
                }

                if (filled > 0)
                    Debug.Log($"[Grid] {planeNames[p]} r={r}: {slotInfo} ({filled}/8)");
            }
        }
    }
}

public struct CompletedRing
{
    public int Plane;
    public int RadiusIndex;

    public CompletedRing(int plane, int radiusIndex)
    {
        Plane = plane;
        RadiusIndex = radiusIndex;
    }

    public override string ToString()
    {
        string[] names = { "XY", "YZ", "XZ" };
        return $"Ring on {names[Plane]} plane at radius {RadiusIndex}";
    }
}