using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class BlockTInstantiator : MonoBehaviour
{
    List<Vector3> leftDiagonalCoordinates  = new List<Vector3>();
    List<Vector3> rightDiagonalCoordinates = new List<Vector3>();
    List<Vector3> verticalCoordinates      = new List<Vector3>();

    public GameObject motherPlatform;

    [Header("Block Prefabs")]
    public GameObject tBlockPrefab;
    public GameObject t1BlockPrefab;
    public GameObject t2BlockPrefab;

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

    // ── cycle definition ─────────────────────────────────────────
    private static readonly BlockType[] _cycleOrder =
    {
        BlockType.T1Block,   // 0
        BlockType.T2Block,   // 1
        BlockType.TBlock,    // 2
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

        Vector3[] preview = GetPreviewPositionsForType(nextType);

        if (preview == null || IsCollidingWithPlatform(preview))
        {
            // ── BLOCKED — keep current block, unfreeze, done ──
            if (logSpawnInfo)
                Debug.Log($"[BlockTInstantiator] Swap to {nextType} BLOCKED — " +
                          (preview == null
                              ? "preview could not be built (indices out of bounds)."
                              : "preview collides with motherPlatform child."));

            isCheckingSwap = false;
            _tapInProgress = false;
            yield break;
        }

        // ── SAFE — proceed with swap ──
        _currentTypeIndex = nextIndex;

        if (logSpawnInfo)
            Debug.Log($"[BlockTInstantiator] Tap → cycling to: {nextType}");

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
                    Debug.Log($"[BlockTInstantiator] Collision at {rounded}");
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
            Debug.Log($"[BlockTInstantiator] Randomiser chose: {chosen}");

        InstantiateBlock(chosen, spawnPosition);
    }

    private void SwapCurrentBlock(BlockType newType)
    {
        Vector3 preservedPosition = _currentBlock != null
            ? _currentBlock.transform.position
            : spawnPosition;

        _currentBlock.SetActive(false);
        Destroy(_currentBlock);

        InstantiateBlock(newType, preservedPosition);
    }

    private void InstantiateBlock(BlockType type, Vector3 pos)
    {
        GameObject prefab = PrefabForType(type);

        if (prefab == null)
        {
            Debug.LogError($"[BlockTInstantiator] Prefab for {type} is not assigned!");
            return;
        }

        _currentBlock = Instantiate(prefab, pos, Quaternion.identity);
        _activeType   = type;

        if (logSpawnInfo)
            Debug.Log($"[BlockTInstantiator] Instantiated {type} at {pos}");
    }

    // ─────────────────────────────────────────────────────────────
    //  PREVIEW SYSTEM
    //
    //  Builds world-position arrays for the NEXT block type
    //  using the live IndexManager counters.
    //
    //  T  has: leftDiag[iL],         rightDiag[iR],         vert[iV], vert[iV-1]
    //  T1 has: rightDiag[iR - 1],    vert[iV], vert[iV-1], vert[iV-2]
    //  T2 has: leftDiag[iL - 1],     rightDiag[iR - 1],    vert[iV], vert[iV-1]
    // ─────────────────────────────────────────────────────────────

    private Vector3[] GetPreviewPositionsForType(BlockType type)
    {
        if (_index == null) return null;

        switch (type)
        {
            case BlockType.TBlock:  return BuildTPreview();
            case BlockType.T1Block: return BuildT1Preview();
            case BlockType.T2Block: return BuildT2Preview();
            default: return null;
        }
    }

    /// <summary>
    /// T block positions:
    ///   left diagonal child  → leftDiagonalCoordinates[iL]
    ///   right diagonal child → rightDiagonalCoordinates[iR]
    ///   vertical child 0     → verticalCoordinates[iV]
    ///   vertical child 1     → verticalCoordinates[iV - 1]
    /// </summary>
    private Vector3[] BuildTPreview()
    {
        int iL = _index.indexCountLeft;
        int iR = _index.indexCountRight;
        int iV = _index.indexCountVertical;

        if (iL < 0 || iL >= leftDiagonalCoordinates.Count)   return null;
        if (iR < 0 || iR >= rightDiagonalCoordinates.Count)  return null;
        if (iV < 1 || iV >= verticalCoordinates.Count)       return null;

        return new Vector3[]
        {
            leftDiagonalCoordinates[iL],
            rightDiagonalCoordinates[iR],
            verticalCoordinates[iV],
            verticalCoordinates[iV - 1],
        };
    }

    /// <summary>
    /// T1 block positions (offset right diagonal, 3 vertical children):
    ///   right diagonal child → rightDiagonalCoordinates[iR - 1]
    ///   vertical child 0     → verticalCoordinates[iV]
    ///   vertical child 1     → verticalCoordinates[iV - 1]
    ///   vertical child 2     → verticalCoordinates[iV - 2]
    /// </summary>
    private Vector3[] BuildT1Preview()
    {
        int iR = _index.indexCountRight;
        int iV = _index.indexCountVertical;

        if (iR < 1 || iR >= rightDiagonalCoordinates.Count)  return null;
        if (iV < 2 || iV >= verticalCoordinates.Count)       return null;

        return new Vector3[]
        {
            rightDiagonalCoordinates[iR - 1],
            verticalCoordinates[iV],
            verticalCoordinates[iV - 1],
            verticalCoordinates[iV - 2],
        };
    }

    /// <summary>
    /// T2 block positions (offset left + right diagonals):
    ///   left diagonal child  → leftDiagonalCoordinates[iL - 1]
    ///   right diagonal child → rightDiagonalCoordinates[iR - 1]
    ///   vertical child 0     → verticalCoordinates[iV]
    ///   vertical child 1     → verticalCoordinates[iV - 1]
    /// </summary>
    private Vector3[] BuildT2Preview()
    {
        int iL = _index.indexCountLeft;
        int iR = _index.indexCountRight;
        int iV = _index.indexCountVertical;

        if (iL < 1 || iL >= leftDiagonalCoordinates.Count)   return null;
        if (iR < 1 || iR >= rightDiagonalCoordinates.Count)  return null;
        if (iV < 1 || iV >= verticalCoordinates.Count)       return null;

        return new Vector3[]
        {
            leftDiagonalCoordinates[iL - 1],
            rightDiagonalCoordinates[iR - 1],
            verticalCoordinates[iV],
            verticalCoordinates[iV - 1],
        };
    }

    // ─────────────────────────────────────────────────────────────
    //  Prefab registry
    // ─────────────────────────────────────────────────────────────

    private GameObject PrefabForType(BlockType type)
    {
        switch (type)
        {
            case BlockType.TBlock:  return tBlockPrefab;
            case BlockType.T1Block: return t1BlockPrefab;
            case BlockType.T2Block: return t2BlockPrefab;
            default:
                Debug.LogWarning($"[BlockTInstantiator] Unhandled BlockType: {type}");
                return null;
        }
    }

    private enum BlockType
    {
        T1Block = 0,
        T2Block = 1,
        TBlock  = 2,
    }
}