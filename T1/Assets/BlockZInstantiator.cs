using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockZInstantiator : MonoBehaviour
{
    List<Vector3> leftDiagonalCoordinates  = new List<Vector3>();
    List<Vector3> rightDiagonalCoordinates = new List<Vector3>();
    List<Vector3> verticalCoordinates      = new List<Vector3>();

    public GameObject motherPlatform;

    [Header("Block Prefabs")]
    public GameObject zBlockPrefab;
    public GameObject z1BlockPrefab;

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

    // ★ Camera reference ──────────────────────────────────────────
    private CameraController _cam;

    // ── cycle definition ─────────────────────────────────────────
    // Tap walks forward through this array and wraps around.
    // Change the order here to change the tap-cycle order globally.
    private static readonly BlockType[] _cycleOrder =
    {
        BlockType.ZBlock,    // 0
        BlockType.Z1Block,   // 1
    };

    // ─────────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
         if (motherPlatform == null) motherPlatform = GameObject.Find("mother");
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

        // ★ Cache the camera controller once
        _cam = Camera.main.GetComponent<CameraController>();

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

        PreviewData preview = GetPreviewForType(nextType);

        if (preview != null && IsCollidingWithPlatform(preview))
        {
            // ── BLOCKED — keep current block, unfreeze, done ──
            if (logSpawnInfo)
                Debug.Log($"[BlockZInstantiator] Swap to {nextType} BLOCKED — " +
                          $"preview collides with motherPlatform child.");

            isCheckingSwap = false;
            _tapInProgress = false;
            yield break;
        }

        // ── SAFE — proceed with swap ──
        _currentTypeIndex = nextIndex;

        if (logSpawnInfo)
            Debug.Log($"[BlockZInstantiator] Tap → cycling to: {nextType}");

        SwapCurrentBlock(nextType);

        // ── unfreeze movement ──
        isCheckingSwap = false;
        _tapInProgress = false;
    }

    // ─────────────────────────────────────────────────────────────
    //  Collision check against motherPlatform children
    //  Compares world positions rounded to 1 decimal place.
    // ─────────────────────────────────────────────────────────────

    private bool IsCollidingWithPlatform(PreviewData preview)
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

        // Check each of the 4 preview positions
        Vector3[] previewPositions = preview.AllPositions;
        for (int p = 0; p < previewPositions.Length; p++)
        {
            Vector3 rounded = RoundTo1Decimal(previewPositions[p]);
            if (platformPositions.Contains(rounded))
            {
                if (logSpawnInfo)
                    Debug.Log($"[BlockZInstantiator] Collision at {rounded}");
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
            Debug.Log($"[BlockZInstantiator] Randomiser chose: {chosen}");

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

        // ★ Unregister old block from camera before destroying it
        _cam?.ClearFallingBlock();

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
            Debug.LogError($"[BlockZInstantiator] Prefab for {type} is not assigned!");
            return;
        }

        _currentBlock = Instantiate(prefab, pos, Quaternion.identity);
        _activeType   = type;

        // ★ Tell the camera to track the new block
        _cam?.SetFallingBlock(_currentBlock.transform);

        if (logSpawnInfo)
            Debug.Log($"[BlockZInstantiator] Instantiated {type} at {pos}");
    }

    // ─────────────────────────────────────────────────────────────
    //  PREVIEW SYSTEM
    //
    //  Z block  has: left(1 child @ [iL-1]),
    //                right(1 child @ [iR]),
    //                vertical(2 children @ [iV] and [iV-1])
    //
    //  Z1 block has: left(2 children @ [iL-1] and [iL-2]),
    //                vertical(2 children @ [iV] and [iV-1])
    //
    //  Both totals = 4 positions → fits PreviewData's AllPositions.
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the preview for whichever type is NOT currently active.
    /// Callable from external scripts if needed.
    /// </summary>
    public PreviewData GetAlternatePreview()
    {
        if (_index == null) return null;

        if (_activeType == BlockType.ZBlock)
            return BuildZ1Preview();
        else
            return BuildZPreview();
    }

    private PreviewData GetPreviewForType(BlockType type)
    {
        if (_index == null) return null;

        if (type == BlockType.Z1Block)
            return BuildZ1Preview();
        else
            return BuildZPreview();
    }

    /// <summary>
    /// Predict where Z would appear using the shared IndexManager indices.
    /// Z has: left 1 child, right 1 child, vertical 2 children.
    /// </summary>
    private PreviewData BuildZPreview()
    {
        int iL = _index.indexCountLeft;
        int iR = _index.indexCountRight;
        int iV = _index.indexCountVertical;

        // Bounds safety — any arm out of range → cannot build preview
        if (iL < 1 || iL - 1 >= leftDiagonalCoordinates.Count)   return null;
        if (iR < 0 || iR >= rightDiagonalCoordinates.Count)      return null;
        if (iV < 1 || iV >= verticalCoordinates.Count)           return null;

        // arm1 = left diagonal (1 child) + right diagonal (1 child)
        // arm2 = vertical (2 children)
        return new PreviewData
        {
            previewBlockName = "Z",

            arm1Child0 = leftDiagonalCoordinates[iL - 1],
            arm1Child1 = rightDiagonalCoordinates[iR],
            arm1Label  = "LeftDiag + RightDiag",

            arm2Child0 = verticalCoordinates[iV],
            arm2Child1 = verticalCoordinates[iV - 1],
            arm2Label  = "Vertical",
        };
    }

    /// <summary>
    /// Predict where Z1 would appear using the shared IndexManager indices.
    /// Z1 has: left 2 children, vertical 2 children.
    /// </summary>
    private PreviewData BuildZ1Preview()
    {
        int iL = _index.indexCountLeft;
        int iV = _index.indexCountVertical;

        // Bounds safety — Z1 left arm needs [iL-1] and [iL-2]
        if (iL < 2 || iL - 1 >= leftDiagonalCoordinates.Count)  return null;
        if (iV < 1 || iV >= verticalCoordinates.Count)          return null;

        // arm1 = left diagonal (2 children)
        // arm2 = vertical (2 children)
        return new PreviewData
        {
            previewBlockName = "Z1",

            arm1Child0 = leftDiagonalCoordinates[iL - 1],
            arm1Child1 = leftDiagonalCoordinates[iL - 2],
            arm1Label  = "LeftDiagonal",

            arm2Child0 = verticalCoordinates[iV],
            arm2Child1 = verticalCoordinates[iV - 1],
            arm2Label  = "Vertical",
        };
    }

    // ─────────────────────────────────────────────────────────────
    //  Randomiser / registry
    // ─────────────────────────────────────────────────────────────

    private GameObject PrefabForType(BlockType type)
    {
        switch (type)
        {
            case BlockType.ZBlock:  return zBlockPrefab;
            case BlockType.Z1Block: return z1BlockPrefab;
            default:
                Debug.LogWarning($"[BlockZInstantiator] Unhandled BlockType: {type}");
                return null;
        }
    }

    /// <summary>
    /// Tap cycle order is driven by the _cycleOrder array above.
    /// To add a new block: add an entry here + a prefab field + a PrefabForType case.
    /// </summary>
    private enum BlockType
    {
        ZBlock  = 0,
        Z1Block = 1,
    }
}