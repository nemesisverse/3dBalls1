using UnityEngine;

public class BlockSInstantiator : MonoBehaviour
{
    [Header("Block Prefabs")]
    public GameObject sBlockPrefab;
    public GameObject s1BlockPrefab;

    [Header("Spawn Settings")]
    public Vector3 spawnPosition = new Vector3(0f, 20f, 0f);
    public float spawnInterval = 2f;

    [Header("References")]
    public GameManager gameManager;

    [Header("Debug")]
    public bool logSpawnInfo = true;

    // ── internal state ────────────────────────────────────────────
    private GameObject _currentBlock;
    private int        _currentTypeIndex;
    private float      _timer;

    // Position used when instantiating candidates for collision testing.
    // Far enough from the scene that no physics interaction can occur.
    private static readonly Vector3 _stagingPosition = new Vector3(99999f, 99999f, 99999f);

    // ── cycle definition ─────────────────────────────────────────
    private static readonly BlockType[] _cycleOrder =
    {
        BlockType.SBlock,
        BlockType.S1Block,
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

        int candidateTypeIndex = (_currentTypeIndex + 1) % _cycleOrder.Length;
        BlockType candidateType = _cycleOrder[candidateTypeIndex];

        // Capture where the live block currently is BEFORE we touch anything
        Vector3 preservedPosition = _currentBlock.transform.position;

        if (logSpawnInfo)
            Debug.Log($"[BlockSInstantiator] Tap → testing swap to: {candidateType} " +
                      $"at preserved pos {preservedPosition}");

        // ── Step 1: spawn candidate far away so physics never touches _currentBlock ──
        GameObject candidate = InstantiateBlockObject(candidateType, _stagingPosition);
        if (candidate == null) return;

        // ── Step 2: overlap check using localPosition offsets ──
        //    Because the candidate was spawned with Quaternion.identity, each child's
        //    localPosition is exactly the shape offset from the block root.
        //    The virtual world position of that child IF the block root were at
        //    preservedPosition is simply:  preservedPosition + child.localPosition
        if (CheckOverlapAtPosition(candidate.transform, preservedPosition))
        {
            // ── COLLISION: revert ────────────────────────────────────────────────
            if (logSpawnInfo)
                Debug.Log("[BlockSInstantiator] Swap REVERTED — candidate overlaps motherPlatform.");

            Destroy(candidate);
            // _currentBlock and _currentTypeIndex are completely untouched
        }
        else
        {
            // ── CLEAR: commit the swap ───────────────────────────────────────────
            if (logSpawnInfo)
                Debug.Log($"[BlockSInstantiator] Swap COMMITTED to: {candidateType}");

            // Move candidate into the real position BEFORE destroying the old block
            // so there is never a frame with zero live blocks.
            candidate.transform.position = preservedPosition;

            _currentBlock.SetActive(false);
            Destroy(_currentBlock);

            _currentTypeIndex = candidateTypeIndex;
            _currentBlock = candidate;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Overlap check — computes virtual world positions from localPosition
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if any child of <paramref name="blockTemplate"/>, when its root
    /// is imagined to be at <paramref name="targetWorldPos"/>, would share a grid cell
    /// with any child currently parented to motherPlatform.
    ///
    /// Uses child.localPosition (valid immediately after Instantiate with
    /// Quaternion.identity) — no Physics.SyncTransforms call needed, so the
    /// live falling block is never disturbed.
    /// </summary>
    private bool CheckOverlapAtPosition(Transform blockTemplate, Vector3 targetWorldPos)
    {
        if (gameManager == null || gameManager.motherPlatform == null)
        {
            Debug.LogWarning("[BlockSInstantiator] gameManager or motherPlatform is null — skipping overlap check.");
            return false;
        }

        Transform platform = gameManager.motherPlatform.transform;

        foreach (Transform candidateChild in blockTemplate)
        {
            // Virtual world position of this child at the target location
            Vector3 cp = targetWorldPos + candidateChild.localPosition;

            foreach (Transform platformChild in platform)
            {
                Vector3 pp = platformChild.position;

                bool xMatch = Mathf.Round(cp.x * 10f) == Mathf.Round(pp.x * 10f);
                bool yMatch = Mathf.Round(cp.y * 10f) == Mathf.Round(pp.y * 10f);
                bool zMatch = Mathf.Round(cp.z * 10f) == Mathf.Round(pp.z * 10f);

                if (xMatch && yMatch && zMatch)
                {
                    if (logSpawnInfo)
                        Debug.Log($"[BlockSInstantiator] Overlap: virtual child pos {cp} " +
                                  $"matches platform child {platformChild.name} at {pp}");
                    return true;
                }
            }
        }

        return false;
    }

    // ─────────────────────────────────────────────────────────────
    //  Core spawn helpers
    // ─────────────────────────────────────────────────────────────

    public void SpawnNextBlock()
    {
        _currentTypeIndex = UnityEngine.Random.Range(0, _cycleOrder.Length);
        BlockType chosen  = _cycleOrder[_currentTypeIndex];

        if (logSpawnInfo)
            Debug.Log($"[BlockSInstantiator] Randomiser chose: {chosen}");

        _currentBlock = InstantiateBlockObject(chosen, spawnPosition);
    }

    /// <summary>
    /// Instantiates the prefab and returns it. Never writes to _currentBlock —
    /// callers decide whether to commit.
    /// </summary>
    private GameObject InstantiateBlockObject(BlockType type, Vector3 pos)
    {
        GameObject prefab = PrefabForType(type);

        if (prefab == null)
        {
            Debug.LogError($"[BlockSInstantiator] Prefab for {type} is not assigned!");
            return null;
        }

        GameObject block = Instantiate(prefab, pos, Quaternion.identity);

        if (logSpawnInfo)
            Debug.Log($"[BlockSInstantiator] Instantiated {type} at {pos}");

        return block;
    }

    // ─────────────────────────────────────────────────────────────
    //  Registry
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