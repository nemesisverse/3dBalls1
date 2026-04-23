using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Replaces the 18 separate dimension lists with a single 3D grid:
///   grid[planeIndex][slotIndex][radiusIndex]
///
/// 3 great-circle planes (XY, YZ, XZ)
/// 8 angular slots per ring (every 45°)
/// 17 radius levels (outermost to innermost)
///
/// Ring completion = all 8 slots filled at one radius on one plane.
/// </summary>
public class SphericalGrid : MonoBehaviour
{
    // ---------- CONSTANTS ----------
    public const int PLANE_COUNT = 3;
    public const int SLOTS_PER_RING = 8;
    public const int RADIUS_LEVELS = 17; // matches your current list sizes

    // Plane indices
    public const int XY = 0;
    public const int YZ = 1;
    public const int XZ = 2;

    // Slot indices within each plane (every 45°, counterclockwise from the "first axis positive")
    // XY plane slots:  0=+X, 1=+X+Y, 2=+Y, 3=-X+Y, 4=-X, 5=-X-Y, 6=-Y, 7=+X-Y
    // YZ plane slots:  0=+Y, 1=+Y+Z, 2=+Z, 3=-Y+Z, 4=-Y, 5=-Y-Z, 6=-Z, 7=+Y-Z
    // XZ plane slots:  0=+X, 1=+X+Z, 2=+Z, 3=-X+Z, 4=-X, 5=-X-Z, 6=-Z, 7=+X-Z

    // ---------- GRID DATA ----------
    // grid[plane][slot][radius] = the GameObject occupying that cell, or null
    private GameObject[,,] grid;

    // Precomputed world positions for every cell
    // positions[plane][slot][radius] = world-space Vector3
    private Vector3[,,] positions;

    // ---------- RADIUS VALUES ----------
    // Diagonal radii (step 0.707) and cardinal radii (step 1.0) — matching your existing coordinates
    private float[] diagonalRadii;  // for slots 1,3,5,7 (45° diagonals)
    private float[] cardinalRadii;  // for slots 0,2,4,6 (axis-aligned)

    // ---------- DIRECTION UNIT VECTORS ----------
    // The unit direction for each (plane, slot) combination
    private static readonly Vector3[,] slotDirections = new Vector3[,]
    {
        // XY plane (normal = Z)
        {
            new Vector3( 1, 0, 0).normalized,   // 0: +X
            new Vector3( 1, 1, 0).normalized,   // 1: +X+Y
            new Vector3( 0, 1, 0).normalized,   // 2: +Y
            new Vector3(-1, 1, 0).normalized,   // 3: -X+Y
            new Vector3(-1, 0, 0).normalized,   // 4: -X
            new Vector3(-1,-1, 0).normalized,   // 5: -X-Y
            new Vector3( 0,-1, 0).normalized,   // 6: -Y
            new Vector3( 1,-1, 0).normalized,   // 7: +X-Y
        },
        // YZ plane (normal = X)
        {
            new Vector3(0,  1, 0).normalized,   // 0: +Y
            new Vector3(0,  1, 1).normalized,   // 1: +Y+Z
            new Vector3(0,  0, 1).normalized,   // 2: +Z
            new Vector3(0, -1, 1).normalized,   // 3: -Y+Z
            new Vector3(0, -1, 0).normalized,   // 4: -Y
            new Vector3(0, -1,-1).normalized,   // 5: -Y-Z
            new Vector3(0,  0,-1).normalized,   // 6: -Z
            new Vector3(0,  1,-1).normalized,   // 7: +Y-Z
        },
        // XZ plane (normal = Y)
        {
            new Vector3( 1, 0, 0).normalized,   // 0: +X
            new Vector3( 1, 0, 1).normalized,   // 1: +X+Z
            new Vector3( 0, 0, 1).normalized,   // 2: +Z
            new Vector3(-1, 0, 1).normalized,   // 3: -X+Z
            new Vector3(-1, 0, 0).normalized,   // 4: -X
            new Vector3(-1, 0,-1).normalized,   // 5: -X-Z
            new Vector3( 0, 0,-1).normalized,   // 6: -Z
            new Vector3( 1, 0,-1).normalized,   // 7: +X-Z
        }
    };

    // Plane normals for orientation detection
    private static readonly Vector3[] planeNormals = new Vector3[]
    {
        Vector3.forward, // XY plane normal = Z
        Vector3.right,   // YZ plane normal = X
        Vector3.up       // XZ plane normal = Y
    };

    void Awake()
    {
        BuildRadii();
        BuildPositions();
        grid = new GameObject[PLANE_COUNT, SLOTS_PER_RING, RADIUS_LEVELS];
        // All entries default to null — empty grid
    }

    // ================================================================
    //  SETUP
    // ================================================================

    void BuildRadii()
    {
        // Diagonal slots use step=0.707, from 13.079 down to ~1.767
        var diagList = new List<float>();
        for (float v = 13.079f; v >= 1.767f - 0.0001f; v -= 0.707f)
            diagList.Add(v);
        diagonalRadii = diagList.ToArray();

        // Cardinal slots use step=1.0, from 18.5 down to 2.5
        var cardList = new List<float>();
        for (float v = 18.5f; v >= 2.5f; v -= 1f)
            cardList.Add(v);
        cardinalRadii = cardList.ToArray();

        // Sanity check: both should produce the same count
        if (diagonalRadii.Length != RADIUS_LEVELS || cardinalRadii.Length != RADIUS_LEVELS)
        {
            Debug.LogWarning($"Radius count mismatch! Diagonal={diagonalRadii.Length}, Cardinal={cardinalRadii.Length}, Expected={RADIUS_LEVELS}. Adjust RADIUS_LEVELS.");
        }
    }

    void BuildPositions()
    {
        positions = new Vector3[PLANE_COUNT, SLOTS_PER_RING, RADIUS_LEVELS];

        for (int p = 0; p < PLANE_COUNT; p++)
        {
            for (int s = 0; s < SLOTS_PER_RING; s++)
            {
                bool isDiagonal = (s % 2 == 1); // slots 1,3,5,7 are 45° diagonals
                float[] radii = isDiagonal ? diagonalRadii : cardinalRadii;
                Vector3 dir = slotDirections[p, s];

                for (int r = 0; r < RADIUS_LEVELS; r++)
                {
                    // For diagonal directions, the component values equal the radius
                    // e.g., direction (1,1,0).normalized * radius gives (r/√2, r/√2, 0)
                    // But your original code uses (v, v, 0) not (v/√2, v/√2, 0)
                    // So we use the raw component value, not normalized * scalar
                    if (isDiagonal)
                    {
                        // Your coordinates: (-v, v, 0) means each component = v
                        float v = radii[r];
                        Vector3 rawDir = GetRawDirection(p, s);
                        positions[p, s, r] = new Vector3(rawDir.x * v, rawDir.y * v, rawDir.z * v);
                    }
                    else
                    {
                        // Cardinal: e.g., (v, 0, 0) or (0, v, 0)
                        float v = radii[r];
                        Vector3 rawDir = GetRawDirection(p, s);
                        positions[p, s, r] = new Vector3(rawDir.x * v, rawDir.y * v, rawDir.z * v);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Returns the non-normalized direction so positions match your original coordinates.
    /// For cardinal slots the direction is already unit length on one axis.
    /// For diagonal slots, components are (1,1,0), (-1,1,0), etc. — NOT normalized.
    /// </summary>
    Vector3 GetRawDirection(int plane, int slot)
    {
        // Same order as slotDirections but without .normalized for diagonals
        Vector3[,] raw = new Vector3[,]
        {
            // XY
            { new Vector3(1,0,0), new Vector3(1,1,0), new Vector3(0,1,0), new Vector3(-1,1,0),
              new Vector3(-1,0,0), new Vector3(-1,-1,0), new Vector3(0,-1,0), new Vector3(1,-1,0) },
            // YZ
            { new Vector3(0,1,0), new Vector3(0,1,1), new Vector3(0,0,1), new Vector3(0,-1,1),
              new Vector3(0,-1,0), new Vector3(0,-1,-1), new Vector3(0,0,-1), new Vector3(0,1,-1) },
            // XZ
            { new Vector3(1,0,0), new Vector3(1,0,1), new Vector3(0,0,1), new Vector3(-1,0,1),
              new Vector3(-1,0,0), new Vector3(-1,0,-1), new Vector3(0,0,-1), new Vector3(1,0,-1) }
        };
        return raw[plane, slot];
    }

    // ================================================================
    //  CORE API — Use these instead of the 18 lists
    // ================================================================

    /// <summary>
    /// Place a block into the grid at a specific cell.
    /// </summary>
    public void PlaceBlock(int plane, int slot, int radiusIndex, GameObject block)
    {
        if (!IsValidCell(plane, slot, radiusIndex)) return;
        grid[plane, slot, radiusIndex] = block;
    }

    /// <summary>
    /// Get the block at a specific cell (null if empty).
    /// </summary>
    public GameObject GetBlock(int plane, int slot, int radiusIndex)
    {
        if (!IsValidCell(plane, slot, radiusIndex)) return null;
        return grid[plane, slot, radiusIndex];
    }

    /// <summary>
    /// Remove a block from a cell.
    /// </summary>
    public void ClearCell(int plane, int slot, int radiusIndex)
    {
        if (!IsValidCell(plane, slot, radiusIndex)) return;
        grid[plane, slot, radiusIndex] = null;
    }

    /// <summary>
    /// Get the precomputed world position for a cell.
    /// </summary>
    public Vector3 GetPosition(int plane, int slot, int radiusIndex)
    {
        return positions[plane, slot, radiusIndex];
    }

    /// <summary>
    /// Check if a cell is occupied.
    /// </summary>
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
    //  RING DETECTION — replaces checkRingToDestroy, checkYZRing, checkXZRing
    // ================================================================

    /// <summary>
    /// Check ALL rings on ALL planes. Returns a list of completed rings.
    /// Call this once after a block is placed instead of calling 3 separate methods.
    /// </summary>
    public List<CompletedRing> CheckAllRings()
    {
        var completed = new List<CompletedRing>();

        for (int p = 0; p < PLANE_COUNT; p++)
        {
            for (int r = 0; r < RADIUS_LEVELS; r++)
            {
                if (IsRingComplete(p, r))
                {
                    completed.Add(new CompletedRing(p, r));
                }
            }
        }
        return completed;
    }

    /// <summary>
    /// Check if a specific ring is fully filled.
    /// A ring = all 8 slots at one radius level on one plane.
    /// </summary>
    public bool IsRingComplete(int plane, int radiusIndex)
    {
        for (int s = 0; s < SLOTS_PER_RING; s++)
        {
            if (grid[plane, s, radiusIndex] == null)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Destroy all blocks in a completed ring, clear the grid cells,
    /// and optionally shift outer blocks inward to fill the gap.
    /// </summary>
    public void DestroyRing(int plane, int radiusIndex)
    {
        for (int s = 0; s < SLOTS_PER_RING; s++)
        {
            GameObject block = grid[plane, s, radiusIndex];
            if (block != null)
            {
                Destroy(block);
                grid[plane, s, radiusIndex] = null;
            }
        }

        // Optional: shift blocks inward to fill gap (gravity)
        // ShiftBlocksInward(plane, radiusIndex);
    }

    /// <summary>
    /// After destroying a ring at radiusIndex, move all blocks at smaller
    /// indices (= farther from center) one step inward.
    /// </summary>
    public void ShiftBlocksInward(int plane, int destroyedRadius)
    {
        // Radius index 0 = outermost, RADIUS_LEVELS-1 = innermost
        // Shift everything from destroyedRadius-1 down to 0, one step toward center
        for (int r = destroyedRadius; r > 0; r--)
        {
            for (int s = 0; s < SLOTS_PER_RING; s++)
            {
                grid[plane, s, r] = grid[plane, s, r - 1];
                grid[plane, s, r - 1] = null;

                // Move the actual GameObject to its new position
                if (grid[plane, s, r] != null)
                {
                    grid[plane, s, r].transform.position = positions[plane, s, r];
                }
            }
        }
    }

    // ================================================================
    //  BLOCK PLACEMENT HELPER — replaces leftflagRadius / rightflagRadius / verticalflagRadius
    // ================================================================

    /// <summary>
    /// Given a world position, find which (plane, slot, radiusIndex) it belongs to.
    /// Returns true if a match was found.
    /// </summary>
    public bool FindCell(Vector3 worldPos, out int plane, out int slot, out int radiusIndex, float tolerance = 0.05f)
    {
        for (int p = 0; p < PLANE_COUNT; p++)
        {
            for (int s = 0; s < SLOTS_PER_RING; s++)
            {
                for (int r = 0; r < RADIUS_LEVELS; r++)
                {
                    if (Vector3.Distance(worldPos, positions[p, s, r]) < tolerance)
                    {
                        plane = p;
                        slot = s;
                        radiusIndex = r;
                        return true;
                    }
                }
            }
        }
        plane = slot = radiusIndex = -1;
        return false;
    }

    /// <summary>
    /// Smarter placement: given the motherPlatform's orientation and a radius index,
    /// determine which planes the block should be registered on, and find the first
    /// empty slot on those planes.
    ///
    /// This replaces the entire leftflagRadius/rightflagRadius/verticalflagRadius logic.
    /// </summary>
    public bool PlaceBlockAtRadius(int radiusIndex, GameObject block, Transform motherPlatform)
    {
        // Determine which 2 planes intersect the block's falling path
        // based on the motherPlatform's current orientation
        int[] activePlanes = GetActivePlanes(motherPlatform);

        foreach (int p in activePlanes)
        {
            // Find first empty slot at this radius on this plane
            for (int s = 0; s < SLOTS_PER_RING; s++)
            {
                if (grid[p, s, radiusIndex] == null)
                {
                    grid[p, s, radiusIndex] = block;
                    return true;
                }
            }
        }

        Debug.LogWarning($"No empty slot found at radius {radiusIndex}!");
        return false;
    }

    /// <summary>
    /// For a vertical block (2 pieces), place both at adjacent radius levels.
    /// </summary>
    public bool PlaceVerticalBlock(int radiusIndex, GameObject block0, GameObject block1, Transform motherPlatform)
    {
        int[] activePlanes = GetActivePlanes(motherPlatform);

        foreach (int p in activePlanes)
        {
            for (int s = 0; s < SLOTS_PER_RING; s++)
            {
                // Need TWO consecutive slots at radius i and i-1
                if (radiusIndex - 1 >= 0 &&
                    grid[p, s, radiusIndex] == null &&
                    grid[p, s, radiusIndex - 1] == null)
                {
                    grid[p, s, radiusIndex] = block0;
                    grid[p, s, radiusIndex - 1] = block1;
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Determine which great-circle planes are currently "active" (facing the camera / aligned
    /// with the global XY plane) based on the motherPlatform's rotation.
    ///
    /// This replaces ALL the dot-product if-else chains in flagRadius.
    /// </summary>
    int[] GetActivePlanes(Transform motherPlatform)
    {
        Vector3 globalZ = Vector3.forward;
        Vector3 globalX = Vector3.right;

        // Check which local axis aligns with global Z (the "screen normal")
        Vector3[] localAxes = {
            motherPlatform.right,    // local X
            motherPlatform.up,       // local Y
            motherPlatform.forward   // local Z
        };

        // Map: if local axis i aligns with globalZ, which plane's normal is that?
        // local Z (forward) → XY plane (normal = Z)  → plane index 0
        // local X (right)   → YZ plane (normal = X)  → plane index 1
        // local Y (up)      → XZ plane (normal = Y)  → plane index 2
        int[] axisToPlane = { 1, 2, 0 }; // localX→YZ, localY→XZ, localZ→XY

        var result = new List<int>();

        for (int i = 0; i < 3; i++)
        {
            // If this local axis is aligned with globalZ or globalX, its plane is active
            if (Mathf.Abs(Vector3.Dot(localAxes[i], globalZ)) > 0.99f ||
                Mathf.Abs(Vector3.Dot(localAxes[i], globalX)) > 0.99f)
            {
                int planeIdx = axisToPlane[i];
                if (!result.Contains(planeIdx))
                    result.Add(planeIdx);
            }
        }

        return result.ToArray();
    }

    // ================================================================
    //  POSITION LOOKUP FOR MOVEMENT — replaces coordinate lists
    // ================================================================

    /// <summary>
    /// Get the falling path positions for a specific (plane, slot).
    /// Returns all radius-level positions from outermost to innermost.
    /// Use this instead of leftDiagonalCoordinates / rightDiagonalCoordinates / verticalCoordinates.
    /// </summary>
    public Vector3[] GetFallingPath(int plane, int slot)
    {
        var path = new Vector3[RADIUS_LEVELS];
        for (int r = 0; r < RADIUS_LEVELS; r++)
            path[r] = positions[plane, slot, r];
        return path;
    }

    // ================================================================
    //  DEBUG
    // ================================================================

    /// <summary>
    /// Log the state of all rings for debugging.
    /// </summary>
    public void DebugLogGridState()
    {
        string[] planeNames = { "XY", "YZ", "XZ" };
        for (int p = 0; p < PLANE_COUNT; p++)
        {
            for (int r = 0; r < RADIUS_LEVELS; r++)
            {
                int filled = 0;
                for (int s = 0; s < SLOTS_PER_RING; s++)
                    if (grid[p, s, r] != null) filled++;

                if (filled > 0)
                    Debug.Log($"[{planeNames[p]}] Radius {r}: {filled}/{SLOTS_PER_RING} slots filled");
            }
        }
    }
}

/// <summary>
/// Simple data struct for a completed ring.
/// </summary>
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
        return $"Ring on {names[Plane]} plane at radius index {RadiusIndex}";
    }
}