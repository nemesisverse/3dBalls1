using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockLInstantiator : MonoBehaviour
{
    List<Vector3> leftDiagonalCoordinates  = new List<Vector3>();
    List<Vector3> rightDiagonalCoordinates = new List<Vector3>();
    List<Vector3> verticalCoordinates      = new List<Vector3>();

    public GameObject motherPlatform;
    [Header("Block Prefabs")]
    public GameObject lBlockPrefab;
    public GameObject l1BlockPrefab;
    public GameObject l2BlockPrefab;
    public GameObject l3BlockPrefab;

    [Header("Spawn Settings")]
    public Vector3 spawnPosition = new Vector3(0f, 20f, 0f);
    public float spawnInterval = 2f;          // 0 = manual / GameManager-driven only

    [Header("Debug")]
    public bool logSpawnInfo = true;

    // ── internal state ────────────────────────────────────────────
    private GameObject _currentBlock;         // the block currently falling
    private int        _currentTypeIndex;     // index into _cycleOrder[]
    private float      _timer;

    // ── preview state ─────────────────────────────────────────────
    private IndexManager _index;
    private BlockType    _activeType;

    // ── swap-check pause flag ─────────────────────────────────────
    // Movement scripts check this to freeze while collision check runs
    [HideInInspector] public bool isCheckingSwap = false;

    // Prevents double-tap while coroutine is in flight
    private bool _tapInProgress = false;

    // ── cycle definition ─────────────────────────────────────────
    // Tap walks forward through this array and wraps around.
    // Change the order here to change the tap-cycle order globally.
    private static readonly BlockType[] _cycleOrder =
    {
        BlockType.LBlock,    // 0
        BlockType.L1Block,   // 1
        BlockType.L2Block,   // 2
        BlockType.L3Block,   // 3
    };

    // ─────────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        for (float v = 13.079f; v >= 1.767f - 0.0001f; v -= 0.707f)
            leftDiagonalCoordinates.Add(new Vector3(-v, v, 0f));
        for (float v = 13.079f; v >= 1.767f - 0.0001f; v -= 0.707f)
            rightDiagonalCoordinates.Add(new Vector3(v, v, 0f));
        for (float v = 18.5f; v >= 2.5f; v -= 1f)
            verticalCoordinates.Add(new Vector3(0f, v, 0f));
    }

    private void Start()
    {
        _index = FindFirstObjectByType<IndexManager>();
        _timer = spawnInterval;
        SpawnNextBlock();                       // spawn one immediately on Start
    }

    private void OnEnable()  => TapInput.OnTap += HandleTap;
    private void OnDisable() => TapInput.OnTap -= HandleTap;

    private void Update()
    {
        if (spawnInterval <= 0f) return;        // manual-only mode

        // Only tick down while there is no live block
        if (_currentBlock != null) return;

        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            SpawnNextBlock();
            _timer = spawnInterval;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Tap handler — kicks off the coroutine
    // ─────────────────────────────────────────────────────────────

    private void HandleTap(Vector2 screenPosition)
    {
        // No block alive yet — nothing to cycle
        if (_currentBlock == null) return;
        if (_tapInProgress) return;

        StartCoroutine(HandleTapCoroutine());
    }

    // ─────────────────────────────────────────────────────────────
    //  The actual swap logic — runs across frames so the pause
    //  flag is visible to every movement coroutine.
    //
    //  Frame 0:  set isCheckingSwap = true
    //  Frame 1:  all movement coroutines have paused → safe to check
    //  Frame 1:  collision check runs, swap or reject
    //  Frame 1:  set isCheckingSwap = false → movement resumes
    // ─────────────────────────────────────────────────────────────

    private IEnumerator HandleTapCoroutine()
    {
        _tapInProgress = true;

        // ── FRAME 0: raise the flag ──
        isCheckingSwap = true;

        // ── wait one frame so every movement coroutine hits
        //    "while (isCheckingSwap) yield return null" and freezes ──
        yield return null;

        // ── FRAME 1: all blocks are now frozen — safe to read indices ──

        int nextIndex      = (_currentTypeIndex + 1) % _cycleOrder.Length;
        BlockType nextType = _cycleOrder[nextIndex];

        List<Vector3> previewPositions = GetPreviewForType(nextType);

        if (previewPositions != null && IsCollidingWithPlatform(previewPositions))
        {
            // ── BLOCKED — keep current block, unfreeze, done ──
            if (logSpawnInfo)
                Debug.Log($"[BlockLInstantiator] Swap to {nextType} BLOCKED — " +
                          $"preview collides with motherPlatform child.");

            isCheckingSwap = false;
            _tapInProgress = false;
            yield break;
        }

        // ── SAFE — proceed with swap ──
        _currentTypeIndex = nextIndex;

        if (logSpawnInfo)
            Debug.Log($"[BlockLInstantiator] Tap → cycling to: {nextType}");

        SwapCurrentBlock(nextType);

        // ── unfreeze movement ──
        isCheckingSwap = false;
        _tapInProgress = false;
    }

    // ─────────────────────────────────────────────────────────────
    //  Collision check against motherPlatform children
    //  Compares world positions rounded to 1 decimal place.
    // ─────────────────────────────────────────────────────────────

    private bool IsCollidingWithPlatform(List<Vector3> previewPositions)
    {
        if (motherPlatform == null) return false;

        Transform platformTransform = motherPlatform.transform;
        int childCount = platformTransform.childCount;

        if (childCount == 0) return false;

        // Build HashSet of rounded platform positions for O(1) lookup
        HashSet<Vector3> platformPositions = new HashSet<Vector3>();
        for (int c = 0; c < childCount; c++)
        {
            Transform child = platformTransform.GetChild(c);
            if (child == null || child.gameObject == null) continue;
            platformPositions.Add(RoundTo1Decimal(child.position));
        }

        // Check each preview position
        for (int p = 0; p < previewPositions.Count; p++)
        {
            Vector3 rounded = RoundTo1Decimal(previewPositions[p]);
            if (platformPositions.Contains(rounded))
            {
                if (logSpawnInfo)
                    Debug.Log($"[BlockLInstantiator] Collision at {rounded}");
                return true;
            }
        }

        return false;
    }

    private Vector3 RoundTo1Decimal(Vector3 v)
    {
        return new Vector3(
            Mathf.Round(v.x * 10f) / 10f,
            Mathf.Round(v.y * 10f) / 10f,
            Mathf.Round(v.z * 10f) / 10f
        );
    }

    // ─────────────────────────────────────────────────────────────
    //  Core spawn / swap helpers
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Picks a random block type, sets the cycle index to match,
    /// then instantiates it. Call from GameManager for manual spawning.
    /// </summary>
    public void SpawnNextBlock()
    {
        // Randomiser picks the starting type for this piece
        _currentTypeIndex = UnityEngine.Random.Range(0, _cycleOrder.Length);
        BlockType chosen  = _cycleOrder[_currentTypeIndex];

        if (logSpawnInfo)
            Debug.Log($"[BlockLInstantiator] Randomiser chose: {chosen}");

        InstantiateBlock(chosen, spawnPosition);
    }

    /// <summary>
    /// Destroys the current block and spawns the given type
    /// at the same world position the old block occupied.
    /// </summary>
    private void SwapCurrentBlock(BlockType newType)
    {
        Vector3 preservedPosition = _currentBlock != null
            ? _currentBlock.transform.position
            : spawnPosition;

        // SetActive(false) stops the old block's scripts + renderer IMMEDIATELY
        // Destroy() then cleans up the GameObject at end-of-frame
        _currentBlock.SetActive(false);
        Destroy(_currentBlock);

        InstantiateBlock(newType, preservedPosition);
    }

    /// <summary>
    /// Instantiates the prefab for <paramref name="type"/> at <paramref name="pos"/>
    /// and stores it as the current live block.
    /// </summary>
    private void InstantiateBlock(BlockType type, Vector3 pos)
    {
        GameObject prefab = PrefabForType(type);

        if (prefab == null)
        {
            Debug.LogError($"[BlockLInstantiator] Prefab for {type} is not assigned!");
            return;
        }

        _currentBlock = Instantiate(prefab, pos, Quaternion.identity);
        _activeType   = type;

        if (logSpawnInfo)
            Debug.Log($"[BlockLInstantiator] Instantiated {type} at {pos}");
    }

    // ─────────────────────────────────────────────────────────────
    //  PREVIEW SYSTEM  —  predicts world positions of the NEXT
    //  block type based on the current IndexManager indices.
    //
    //  Each builder mirrors the position-assignment lines from
    //  the corresponding movement coroutine exactly.
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the predicted world positions for the alternate
    /// block (the one that would appear on next tap).
    /// Useful if external systems need the preview.
    /// </summary>
    public List<Vector3> GetAlternatePreview()
    {
        if (_index == null) return null;

        int nextIndex      = (_currentTypeIndex + 1) % _cycleOrder.Length;
        BlockType nextType = _cycleOrder[nextIndex];
        return GetPreviewForType(nextType);
    }

    private List<Vector3> GetPreviewForType(BlockType type)
    {
        if (_index == null) return null;

        switch (type)
        {
            case BlockType.LBlock:  return BuildLPreview();
            case BlockType.L1Block: return BuildL1Preview();
            case BlockType.L2Block: return BuildL2Preview();
            case BlockType.L3Block: return BuildL3Preview();
            default:                return null;
        }
    }

    // ── L BLOCK ──────────────────────────────────────────────────
    //  Left diagonal : 3 children  → [iL], [iL-1], [iL-2]
    //  Vertical      : 1 child     → [iV]
    // ─────────────────────────────────────────────────────────────

    private List<Vector3> BuildLPreview()
    {
        int iL = _index.indexCountLeft;
        int iV = _index.indexCountVertical;

        // bounds: need iL >= 2 for three blocks, iV >= 0 for one block
        if (iL < 2 || iL >= leftDiagonalCoordinates.Count)  return null;
        if (iV < 0 || iV >= verticalCoordinates.Count)      return null;

        return new List<Vector3>
        {
            leftDiagonalCoordinates[iL],
            leftDiagonalCoordinates[iL - 1],
            leftDiagonalCoordinates[iL - 2],
            verticalCoordinates[iV],
        };
    }

    // ── L1 BLOCK ─────────────────────────────────────────────────
    //  Left diagonal : 1 child     → [iL]
    //  Right diagonal: 2 children  → [iR], [iR-1]
    //  Vertical      : 1 child     → [iV]
    // ─────────────────────────────────────────────────────────────

    private List<Vector3> BuildL1Preview()
    {
        int iL = _index.indexCountLeft;
        int iR = _index.indexCountRight;
        int iV = _index.indexCountVertical;

        if (iL < 0 || iL >= leftDiagonalCoordinates.Count)  return null;
        if (iR < 1 || iR >= rightDiagonalCoordinates.Count) return null;
        if (iV < 0 || iV >= verticalCoordinates.Count)      return null;

        return new List<Vector3>
        {
            leftDiagonalCoordinates[iL],
            rightDiagonalCoordinates[iR],
            rightDiagonalCoordinates[iR - 1],
            verticalCoordinates[iV],
        };
    }

    // ── L2 BLOCK ─────────────────────────────────────────────────
    //  Left diagonal : 2 children  → [iL], [iL-1]
    //  Right diagonal: 1 child     → [iR-1]   (offset pattern)
    //  Vertical      : 1 child     → [iV-1]   (offset pattern)
    // ─────────────────────────────────────────────────────────────

    private List<Vector3> BuildL2Preview()
    {
        int iL = _index.indexCountLeft;
        int iR = _index.indexCountRight;
        int iV = _index.indexCountVertical;

        if (iL < 1 || iL >= leftDiagonalCoordinates.Count)  return null;
        if (iR < 1 || iR >= rightDiagonalCoordinates.Count) return null;
        if (iV < 1 || iV >= verticalCoordinates.Count)      return null;

        return new List<Vector3>
        {
            leftDiagonalCoordinates[iL],
            leftDiagonalCoordinates[iL - 1],
            rightDiagonalCoordinates[iR - 1],
            verticalCoordinates[iV - 1],
        };
    }

    // ── L3 BLOCK ─────────────────────────────────────────────────
    //  Left diagonal : 1 child     → [iL-2]   (offset pattern)
    //  Vertical      : 3 children  → [iV], [iV-1], [iV-2]
    // ─────────────────────────────────────────────────────────────

    private List<Vector3> BuildL3Preview()
    {
        int iL = _index.indexCountLeft;
        int iV = _index.indexCountVertical;

        // need iL >= 2 for [iL-2], need iV >= 2 for three vertical blocks
        if (iL < 2 || iL >= leftDiagonalCoordinates.Count) return null;
        if (iV < 2 || iV >= verticalCoordinates.Count)     return null;

        return new List<Vector3>
        {
            leftDiagonalCoordinates[iL - 2],
            verticalCoordinates[iV],
            verticalCoordinates[iV - 1],
            verticalCoordinates[iV - 2],
        };
    }

    // ─────────────────────────────────────────────────────────────
    //  Randomiser / registry
    // ─────────────────────────────────────────────────────────────

    private GameObject PrefabForType(BlockType type)
    {
        switch (type)
        {
            case BlockType.LBlock:  return lBlockPrefab;
            case BlockType.L1Block: return l1BlockPrefab;
            case BlockType.L2Block: return l2BlockPrefab;
            case BlockType.L3Block: return l3BlockPrefab;
            default:
                Debug.LogWarning($"[BlockLInstantiator] Unhandled BlockType: {type}");
                return null;
        }
    }

    /// <summary>
    /// Tap cycle order is driven by the _cycleOrder array above.
    /// To add a new block: add an entry here + a prefab field + a PrefabForType case.
    /// </summary>
    private enum BlockType
    {
        LBlock  = 0,
        L1Block = 1,
        L2Block = 2,
        L3Block = 3,
    }
}