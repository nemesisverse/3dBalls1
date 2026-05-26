using UnityEngine;

public class BlockTInstantiator : MonoBehaviour
{
    [Header("Block Prefabs")]
    public GameObject tBlockPrefab;
    public GameObject t1BlockPrefab;
    public GameObject t2BlockPrefab;

    [Header("Spawn Settings")]
    public Vector3 spawnPosition = new Vector3(0f, 20f, 0f);
    public float spawnInterval = 2f;

    [Header("Debug")]
    public bool logSpawnInfo = true;

    private GameObject _currentBlock;
    private int        _currentTypeIndex;
    private float      _timer;

    private static readonly BlockType[] _cycleOrder =
    {
        BlockType.T1Block,
        BlockType.T2Block,
        BlockType.TBlock,
    };

    // ─────────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────────────

    private void Start()
    {
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
    //  Tap handler
    // ─────────────────────────────────────────────────────────────

    private void HandleTap(Vector2 screenPosition)
    {
        if (_currentBlock == null) return;

        _currentTypeIndex = (_currentTypeIndex + 1) % _cycleOrder.Length;

        if (logSpawnInfo)
            Debug.Log($"[BlockInstantiator] Tap → cycling to: {_cycleOrder[_currentTypeIndex]}");

        SwapCurrentBlock(_cycleOrder[_currentTypeIndex]);
    }

    // ─────────────────────────────────────────────────────────────
    //  Spawn / swap
    // ─────────────────────────────────────────────────────────────

    public void SpawnNextBlock()
    {
        _currentTypeIndex = UnityEngine.Random.Range(0, _cycleOrder.Length);
        BlockType chosen  = _cycleOrder[_currentTypeIndex];

        if (logSpawnInfo)
            Debug.Log($"[BlockInstantiator] Randomiser chose: {chosen}");

        // Fresh spawn always starts at index 2
        InstantiateBlock(chosen, spawnPosition, startIndex: 2);
    }

    private void SwapCurrentBlock(BlockType newType)
    {
        // ── 1. Save the current falling index BEFORE destroying ──
        int savedIndex = 2;
        if (_currentBlock != null)
        {
            IndexManager oldIndexManager = _currentBlock.GetComponent<IndexManager>();
            if (oldIndexManager != null)
            {
                savedIndex = oldIndexManager.indexCount;
                if (logSpawnInfo)
                    Debug.Log($"[BlockInstantiator] Swap — preserving indexCount: {savedIndex}");
            }
        }

        // ── 2. Preserve world position ──
        Vector3 preservedPosition = _currentBlock != null
            ? _currentBlock.transform.position
            : spawnPosition;

        // ── 3. Kill the old block ──
        _currentBlock.SetActive(false);
        Destroy(_currentBlock);

        // ── 4. Spawn new block, injecting the saved index ──
        InstantiateBlock(newType, preservedPosition, savedIndex);
    }

    /// <summary>
    /// Instantiates a block and sets its IndexManager.indexCount
    /// BEFORE Start() runs so the coroutine loop begins at the right step.
    /// </summary>
    private void InstantiateBlock(BlockType type, Vector3 pos, int startIndex)
    {
        GameObject prefab = PrefabForType(type);

        if (prefab == null)
        {
            Debug.LogError($"[BlockInstantiator] Prefab for {type} is not assigned!");
            return;
        }

        _currentBlock = Instantiate(prefab, pos, Quaternion.identity);

        // Awake() has already run at this point, so index is found.
        // Start() hasn't run yet — safe to set the index here.
        IndexManager idx = _currentBlock.GetComponent<IndexManager>();
        if (idx != null)
        {
            idx.indexCount = startIndex;
            if (logSpawnInfo)
                Debug.Log($"[BlockInstantiator] Injected indexCount={startIndex} into new {type}");
        }
        else
        {
            Debug.LogWarning($"[BlockInstantiator] No IndexManager found on {type} prefab!");
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Registry
    // ─────────────────────────────────────────────────────────────

    private GameObject PrefabForType(BlockType type)
    {
        switch (type)
        {
            case BlockType.TBlock:  return tBlockPrefab;
            case BlockType.T1Block: return t1BlockPrefab;
            case BlockType.T2Block: return t2BlockPrefab;
            default:
                Debug.LogWarning($"[BlockInstantiator] Unhandled BlockType: {type}");
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