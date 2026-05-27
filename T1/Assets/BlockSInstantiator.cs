using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockSInstantiator : MonoBehaviour
{
    List<Vector3> leftDiagonalCoordinates  = new List<Vector3>();
    List<Vector3> rightDiagonalCoordinates = new List<Vector3>();
    List<Vector3> verticalCoordinates      = new List<Vector3>();

    public GameObject motherPlatform;

    [Header("Block Prefabs")]
    public GameObject sBlockPrefab;
    public GameObject s1BlockPrefab;

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
    // Movement scripts should check this exactly like gameManager.isRotating:
    //   while (blockSInstantiator.isCheckingSwap) yield return null;
    [HideInInspector] public bool isCheckingSwap = false;

    // ── cycle definition ─────────────────────────────────────────
    private static readonly BlockType[] _cycleOrder =
    {
        BlockType.SBlock,
        BlockType.S1Block,
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
    //  Tap handler — now with collision guard
    // ─────────────────────────────────────────────────────────────

    private void HandleTap(Vector2 screenPosition)
    {
        if (_currentBlock == null) return;

        // Don't allow tap while already checking
        if (isCheckingSwap) return;

        // Figure out what the NEXT type would be
        int nextIndex    = (_currentTypeIndex + 1) % _cycleOrder.Length;
        BlockType nextType = _cycleOrder[nextIndex];

        // ── PAUSE movement, run collision check ──
        isCheckingSwap = true;

        PreviewData preview = GetPreviewForType(nextType);

        if (preview != null && IsCollidingWithPlatform(preview))
        {
            // ── BLOCKED — don't swap, keep current block falling ──
            isCheckingSwap = false;

            if (logSpawnInfo)
                Debug.Log($"[BlockSInstantiator] Swap to {nextType} BLOCKED — " +
                          $"preview collides with motherPlatform child.");
            return;
        }

        // ── SAFE — proceed with swap ──
        _currentTypeIndex = nextIndex;

        if (logSpawnInfo)
            Debug.Log($"[BlockSInstantiator] Tap → cycling to: {nextType}");

        SwapCurrentBlock(nextType);

        isCheckingSwap = false;
    }

    // ─────────────────────────────────────────────────────────────
    //  Collision check against motherPlatform children
    //  Compares world positions rounded to 1 decimal place.
    // ─────────────────────────────────────────────────────────────

    private bool IsCollidingWithPlatform(PreviewData preview)
    {
        if (motherPlatform == null) return false;

        // Cache all motherPlatform child positions (rounded)
        Transform platformTransform = motherPlatform.transform;
        int childCount = platformTransform.childCount;

        // Early exit
        if (childCount == 0) return false;

        // Build a HashSet of rounded platform positions for O(1) lookup
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
                    Debug.Log($"[BlockSInstantiator] Collision at {rounded}");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Rounds each component to 1 decimal place.
    /// e.g. (13.079, 13.079, 0) → (13.1, 13.1, 0.0)
    /// </summary>
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
            Debug.Log($"[BlockSInstantiator] Randomiser chose: {chosen}");

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
            Debug.LogError($"[BlockSInstantiator] Prefab for {type} is not assigned!");
            return;
        }

        _currentBlock = Instantiate(prefab, pos, Quaternion.identity);
        _activeType   = type;

        if (logSpawnInfo)
            Debug.Log($"[BlockSInstantiator] Instantiated {type} at {pos}");
    }

    // ─────────────────────────────────────────────────────────────
    //  PREVIEW SYSTEM
    //
    //  Index cross-sync recap:
    //    S  active → left arm drives indexCountLeft
    //                vertical arm drives indexCountVertical
    //                vertical arm SYNCS indexCountRight++
    //
    //    S1 active → left arm drives indexCountLeft
    //                right arm drives indexCountRight
    //                right arm SYNCS indexCountVertical++
    //
    //  So the "other" block's second-arm index is always ready.
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns preview for whichever block is NOT currently active.
    /// </summary>
    public PreviewData GetAlternatePreview()
    {
        if (_index == null) return null;

        if (_activeType == BlockType.SBlock)
            return BuildS1Preview();
        else
            return BuildSPreview();
    }

    /// <summary>
    /// Returns preview for a specific type (used by HandleTap
    /// to check the exact type we're about to swap TO).
    /// </summary>
    private PreviewData GetPreviewForType(BlockType type)
    {
        if (_index == null) return null;

        if (type == BlockType.S1Block)
            return BuildS1Preview();
        else
            return BuildSPreview();
    }

    /// <summary>
    /// S is falling → where would S1 be?
    /// </summary>
    private PreviewData BuildS1Preview()
    {
        int iL = _index.indexCountLeft;
        int iR = _index.indexCountRight;

        if (iL < 1 || iL >= leftDiagonalCoordinates.Count)  return null;
        if (iR < 1 || iR >= rightDiagonalCoordinates.Count) return null;

        return new PreviewData
        {
            previewBlockName = "S1",

            arm1Child0 = leftDiagonalCoordinates[iL],
            arm1Child1 = leftDiagonalCoordinates[iL - 1],
            arm1Label  = "LeftDiagonal",

            arm2Child0 = rightDiagonalCoordinates[iR],
            arm2Child1 = rightDiagonalCoordinates[iR - 1],
            arm2Label  = "RightDiagonal",
        };
    }

    /// <summary>
    /// S1 is falling → where would S be?
    /// </summary>
    private PreviewData BuildSPreview()
    {
        int iL = _index.indexCountLeft;
        int iV = _index.indexCountVertical;

        if (iL < 1 || iL >= leftDiagonalCoordinates.Count) return null;
        if (iV < 1 || iV >= verticalCoordinates.Count)     return null;

        return new PreviewData
        {
            previewBlockName = "S",

            arm1Child0 = leftDiagonalCoordinates[iL],
            arm1Child1 = leftDiagonalCoordinates[iL - 1],
            arm1Label  = "LeftDiagonal",

            arm2Child0 = verticalCoordinates[iV],
            arm2Child1 = verticalCoordinates[iV - 1],
            arm2Label  = "Vertical",
        };
    }

    // ─────────────────────────────────────────────────────────────
    //  Prefab registry
    // ─────────────────────────────────────────────────────────────

    private GameObject PrefabForType(BlockType type)
    {
        switch (type)
        {
            case BlockType.SBlock:  return sBlockPrefab;
            case BlockType.S1Block: return s1BlockPrefab;
            default:
                Debug.LogWarning($"[BlockSInstantiator] Unhandled BlockType: {type}");
                return null;
        }
    }

    private enum BlockType
    {
        SBlock  = 0,
        S1Block = 1,
    }
}

// ─────────────────────────────────────────────────────────────
//  Data container
// ─────────────────────────────────────────────────────────────

public class PreviewData
{
    public string previewBlockName;

    public Vector3 arm1Child0;
    public Vector3 arm1Child1;
    public string  arm1Label;

    public Vector3 arm2Child0;
    public Vector3 arm2Child1;
    public string  arm2Label;

    public Vector3[] AllPositions =>
        new[] { arm1Child0, arm1Child1, arm2Child0, arm2Child1 };
}