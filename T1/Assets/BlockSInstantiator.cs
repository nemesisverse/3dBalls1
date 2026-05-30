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
    // Movement scripts check this to freeze while collision check runs
    [HideInInspector] public bool isCheckingSwap = false;

    // Prevents double-tap while coroutine is in flight
    private bool _tapInProgress = false;

    // ★ Camera reference ──────────────────────────────────────────
    private CameraController _cam;

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

        PreviewData preview = GetPreviewForType(nextType);

        if (preview != null && IsCollidingWithPlatform(preview))
        {
            // ── BLOCKED — keep current block, unfreeze, done ──
            if (logSpawnInfo)
                Debug.Log($"[BlockSInstantiator] Swap to {nextType} BLOCKED — " +
                          $"preview collides with motherPlatform child.");

            isCheckingSwap = false;
            _tapInProgress = false;
            yield break;
        }

        // ── SAFE — proceed with swap ──
        _currentTypeIndex = nextIndex;

        if (logSpawnInfo)
            Debug.Log($"[BlockSInstantiator] Tap → cycling to: {nextType}");

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
                    Debug.Log($"[BlockSInstantiator] Collision at {rounded}");
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
            Debug.Log($"[BlockSInstantiator] Randomiser chose: {chosen}");

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
            Debug.LogError($"[BlockSInstantiator] Prefab for {type} is not assigned!");
            return;
        }

        _currentBlock = Instantiate(prefab, pos, Quaternion.identity);
        _activeType   = type;

        // ★ Tell the camera to track the new block
        _cam?.SetFallingBlock(_currentBlock.transform);

        if (logSpawnInfo)
            Debug.Log($"[BlockSInstantiator] Instantiated {type} at {pos}");
    }

    // ─────────────────────────────────────────────────────────────
    //  PREVIEW SYSTEM
    // ─────────────────────────────────────────────────────────────

    public PreviewData GetAlternatePreview()
    {
        if (_index == null) return null;

        if (_activeType == BlockType.SBlock)
            return BuildS1Preview();
        else
            return BuildSPreview();
    }

    private PreviewData GetPreviewForType(BlockType type)
    {
        if (_index == null) return null;

        if (type == BlockType.S1Block)
            return BuildS1Preview();
        else
            return BuildSPreview();
    }

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