using UnityEngine;
using System;

public class BlockTInstantiator : MonoBehaviour
{
    [Header("Block Prefabs")]
    public GameObject tBlockPrefab;
    public GameObject t1BlockPrefab;
    public GameObject t2BlockPrefab;

    [Header("Spawn Settings")]
    public Vector3 spawnPosition = new Vector3(0f, 20f, 0f);
    public float spawnInterval = 2f;          // 0 = manual / GameManager-driven only

    [Header("Debug")]
    public bool logSpawnInfo = true;

    // ── internal state ────────────────────────────────────────────
    private GameObject _currentBlock;         // the block currently falling
    private int        _currentTypeIndex;     // index into _cycleOrder[]
    private float      _timer;

    // ── cycle definition ─────────────────────────────────────────
    // Tap walks forward through this array and wraps around.
    // Change the order here to change the tap-cycle order globally.
    private static readonly BlockType[] _cycleOrder =
    {
        BlockType.T1Block,   // 0
        BlockType.T2Block,   // 1
        BlockType.TBlock,    // 2
    };

    // ─────────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────────────

    private void Start()
    {
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
    //  Tap handler  — called by TapInput.OnTap event
    // ─────────────────────────────────────────────────────────────

    private void HandleTap(Vector2 screenPosition)
    {
        // No block alive yet — nothing to cycle
        if (_currentBlock == null) return;

        // Advance one step in the cycle (wraps automatically)
        _currentTypeIndex = (_currentTypeIndex + 1) % _cycleOrder.Length;

        if (logSpawnInfo)
            Debug.Log($"[BlockInstantiator] Tap → cycling to: {_cycleOrder[_currentTypeIndex]}");

        SwapCurrentBlock(_cycleOrder[_currentTypeIndex]);
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
            Debug.Log($"[BlockInstantiator] Randomiser chose: {chosen}");

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
            Debug.LogError($"[BlockInstantiator] Prefab for {type} is not assigned!");
            return;
        }

        _currentBlock = Instantiate(prefab, pos, Quaternion.identity);

        if (logSpawnInfo)
            Debug.Log($"[BlockInstantiator] Instantiated {type} at {pos}");
    }

    // ─────────────────────────────────────────────────────────────
    //  Randomiser / registry
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

    /// <summary>
    /// Tap cycle order is driven by the _cycleOrder array above.
    /// To add a new block: add an entry here + a prefab field + a PrefabForType case.
    /// </summary>
    private enum BlockType
    {
        T1Block = 0,
        T2Block = 1,
        TBlock  = 2,
    }
}