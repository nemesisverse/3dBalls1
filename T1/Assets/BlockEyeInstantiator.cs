using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockEyeInstantiator : MonoBehaviour
{
    List<Vector3> leftDiagonalCoordinates  = new List<Vector3>();
    List<Vector3> rightDiagonalCoordinates = new List<Vector3>();   // ← NEW: needed for Eye2 preview
    List<Vector3> verticalCoordinates      = new List<Vector3>();

    public GameObject motherPlatform;
    [Header("Block Prefabs")]
    public GameObject eyeBlockPrefab;
    public GameObject eye1BlockPrefab;
    public GameObject eye2BlockPrefab;   // ← NEW

    [Header("Spawn Settings")]
    public Vector3 spawnPosition = new Vector3(0f, 20f, 0f);
    public float spawnInterval = 2f;

    [Header("Debug")]
    public bool logSpawnInfo = true;

    // ── internal state ────────────────────────────────────────────
    private GameObject _currentBlock;
    private int        _currentTypeIndex;
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
    // Eye → Eye1 → Eye2 → Eye  (3-way circular cycle)
    private static readonly BlockType[] _cycleOrder =
    {
        BlockType.EyeBlock,    // 0
        BlockType.Eye1Block,   // 1
        BlockType.Eye2Block,   // 2  ← NEW
    };

    // ─────────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (motherPlatform == null) motherPlatform = GameObject.Find("mother");

        // Eye1 / Eye2-left  fall along left-diagonal
        for (float v = 13.079f; v >= 1.767f - 0.0001f; v -= 0.707f)
            leftDiagonalCoordinates.Add(new Vector3(-v, v, 0f));

        // Eye2-right  falls along right-diagonal  ← NEW
        for (float v = 13.079f; v >= 1.767f - 0.0001f; v -= 0.707f)
            rightDiagonalCoordinates.Add(new Vector3(v, v, 0f));

        // Eye / Eye2-vertical  fall along the Y axis
        for (float v = 18.5f; v >= 2.5f; v -= 1f)
            verticalCoordinates.Add(new Vector3(0f, v, 0f));
    }

    private void Start()
    {
        _index = FindFirstObjectByType<IndexManager>();
        _timer = spawnInterval;

        // ★ Cache the camera controller once
        _cam = Camera.main.GetComponent<CameraController>();

        SpawnNextBlock();
    }

    private void OnEnable()  => TapInput.OnTap += HandleTap;
    private void OnDisable() => TapInput.OnTap -= HandleTap;

    private void Update()
    {
        if (spawnInterval <= 0f) return;
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

        Vector3[] preview = GetPreviewPositions(nextType);

        if (preview != null && IsCollidingWithPlatform(preview))
        {
            // ── BLOCKED — keep current block, unfreeze, done ──
            if (logSpawnInfo)
                Debug.Log($"[BlockEyeInstantiator] Swap to {nextType} BLOCKED — " +
                          $"preview collides with motherPlatform child.");

            isCheckingSwap = false;
            _tapInProgress = false;
            yield break;
        }

        // ── SAFE — proceed with swap ──
        _currentTypeIndex = nextIndex;

        if (logSpawnInfo)
            Debug.Log($"[BlockEyeInstantiator] Tap → cycling to: {nextType}");

        SwapCurrentBlock(nextType);

        // ── unfreeze movement ──
        isCheckingSwap = false;
        _tapInProgress = false;
    }

    // ─────────────────────────────────────────────────────────────
    //  Collision check against motherPlatform children
    //  Compares world positions rounded to 1 decimal place.
    // ─────────────────────────────────────────────────────────────

    private bool IsCollidingWithPlatform(Vector3[] previewPositions)
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
        for (int p = 0; p < previewPositions.Length; p++)
        {
            Vector3 rounded = RoundTo1Decimal(previewPositions[p]);
            if (platformPositions.Contains(rounded))
            {
                if (logSpawnInfo)
                    Debug.Log($"[BlockEyeInstantiator] Collision at {rounded}");
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

    public void SpawnNextBlock()
    {
        _currentTypeIndex = UnityEngine.Random.Range(0, _cycleOrder.Length);
        BlockType chosen  = _cycleOrder[_currentTypeIndex];

        if (logSpawnInfo)
            Debug.Log($"[BlockEyeInstantiator] Randomiser chose: {chosen}");

        InstantiateBlock(chosen, spawnPosition);
    }

    private void SwapCurrentBlock(BlockType newType)
    {
        Vector3 preservedPosition = _currentBlock != null
            ? _currentBlock.transform.position
            : spawnPosition;

        // ★ Unregister old block from camera before destroying it
        _cam?.ClearFallingBlock();

        _currentBlock.SetActive(false);
        Destroy(_currentBlock);

        InstantiateBlock(newType, preservedPosition);
    }

    private void InstantiateBlock(BlockType type, Vector3 pos)
    {
        GameObject prefab = PrefabForType(type);

        if (prefab == null)
        {
            Debug.LogError($"[BlockEyeInstantiator] Prefab for {type} is not assigned!");
            return;
        }

        _currentBlock = Instantiate(prefab, pos, Quaternion.identity);
        _activeType   = type;

        // ★ Tell the camera to track the new block
        _cam?.SetFallingBlock(_currentBlock.transform);

        if (logSpawnInfo)
            Debug.Log($"[BlockEyeInstantiator] Instantiated {type} at {pos}");
    }

    // ─────────────────────────────────────────────────────────────
    //  PREVIEW SYSTEM
    //
    //  Eye  — 3 blocks on vertical axis:        verticalCoordinates[iV], [iV-1], [iV-2]
    //  Eye1 — 3 blocks on left diagonal:         leftDiagonalCoordinates[iL], [iL-1], [iL-2]
    //  Eye2 — 1 block on each axis (independent): left[iL], right[iR], vertical[iV]
    //
    //  Eye and Eye1 keep all three IndexManager counters in lockstep,
    //  so whichever of them is currently falling, any index already
    //  tells us where the alternate block's children would appear.
    //
    //  Eye2 advances each index independently, but the initial values
    //  are always the same (reset to 2 on landing) so the prediction
    //  is still valid while the block has not yet started drifting.
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the world positions the given block type would
    /// occupy right now, based on the current IndexManager state.
    /// Returns null if indices are out of range (block cannot exist here).
    /// </summary>
    private Vector3[] GetPreviewPositions(BlockType type)
    {
        if (_index == null) return null;

        if (type == BlockType.EyeBlock)
            return BuildEyePreview();
        else if (type == BlockType.Eye1Block)
            return BuildEye1Preview();
        else
            return BuildEye2Preview();
    }

    /// <summary>
    /// Predict where Eye (vertical, 3 children) would be right now.
    /// Uses verticalCoordinates at indices [iV], [iV-1], [iV-2].
    /// </summary>
    private Vector3[] BuildEyePreview()
    {
        int iV = _index.indexCountVertical;

        if (iV < 2 || iV >= verticalCoordinates.Count) return null;

        return new Vector3[]
        {
            verticalCoordinates[iV],
            verticalCoordinates[iV - 1],
            verticalCoordinates[iV - 2],
        };
    }

    /// <summary>
    /// Predict where Eye1 (left-diagonal, 3 children) would be right now.
    /// Uses leftDiagonalCoordinates at indices [iL], [iL-1], [iL-2].
    /// </summary>
    private Vector3[] BuildEye1Preview()
    {
        int iL = _index.indexCountLeft;

        if (iL < 2 || iL >= leftDiagonalCoordinates.Count) return null;

        return new Vector3[]
        {
            leftDiagonalCoordinates[iL],
            leftDiagonalCoordinates[iL - 1],
            leftDiagonalCoordinates[iL - 2],
        };
    }

    /// <summary>
    /// Predict where Eye2's 3 independent children would be right now.
    /// One child on each axis: left diagonal, right diagonal, vertical.
    /// Each uses the current value of its own IndexManager counter.
    /// </summary>
    private Vector3[] BuildEye2Preview()   // ← NEW
    {
        int iL = _index.indexCountLeft;
        int iR = _index.indexCountRight;
        int iV = _index.indexCountVertical;

        if (iL >= leftDiagonalCoordinates.Count  ||
            iR >= rightDiagonalCoordinates.Count  ||
            iV >= verticalCoordinates.Count) return null;

        return new Vector3[]
        {
            leftDiagonalCoordinates[iL],
            rightDiagonalCoordinates[iR],
            verticalCoordinates[iV],
        };
    }

    /// <summary>
    /// Public accessor so external systems can query the NEXT block's
    /// predicted positions (mirrors BlockSInstantiator.GetAlternatePreview).
    /// Follows the 3-way cycle: Eye → Eye1 → Eye2 → Eye.
    /// </summary>
    public Vector3[] GetAlternatePreviewPositions()
    {
        if (_index == null) return null;

        if (_activeType == BlockType.EyeBlock)
            return BuildEye1Preview();
        else if (_activeType == BlockType.Eye1Block)
            return BuildEye2Preview();
        else                                           // Eye2Block → Eye
            return BuildEyePreview();
    }

    // ─────────────────────────────────────────────────────────────
    //  Prefab registry
    // ─────────────────────────────────────────────────────────────

    private GameObject PrefabForType(BlockType type)
    {
        switch (type)
        {
            case BlockType.EyeBlock:  return eyeBlockPrefab;
            case BlockType.Eye1Block: return eye1BlockPrefab;
            case BlockType.Eye2Block: return eye2BlockPrefab;   // ← NEW
            default:
                Debug.LogWarning($"[BlockEyeInstantiator] Unhandled BlockType: {type}");
                return null;
        }
    }

    private enum BlockType
    {
        EyeBlock  = 0,
        Eye1Block = 1,
        Eye2Block = 2,   // ← NEW
    }
}