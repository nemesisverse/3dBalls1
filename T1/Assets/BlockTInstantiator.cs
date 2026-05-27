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
    public float spawnInterval = 2f;

    [Header("Debug")]
    public bool logSpawnInfo = true;

    private GameObject _currentBlock;
    private int        _currentTypeIndex;
    private float      _timer;

    private GameManager _gameManager;

    private static readonly BlockType[] _cycleOrder =
    {
        BlockType.T1Block,   // 0
        BlockType.T2Block,   // 1
        BlockType.TBlock,    // 2
    };

    private void Start()
    {
        _gameManager = FindFirstObjectByType<GameManager>();
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

    private void HandleTap(Vector2 screenPosition)
    {
        if (_currentBlock == null) return;

        int candidateIndex      = (_currentTypeIndex + 1) % _cycleOrder.Length;
        BlockType candidateType = _cycleOrder[candidateIndex];

        if (logSpawnInfo)
            Debug.Log($"[BlockInstantiator] Tap → trying candidate: {candidateType}");

        GameObject prefab = PrefabForType(candidateType);
        if (prefab == null)
        {
            Debug.LogError($"[BlockInstantiator] Prefab for {candidateType} not assigned.");
            return;
        }

        GameObject candidate = Instantiate(prefab, _currentBlock.transform.position, Quaternion.identity);
        candidate.SetActive(false);

        if (CheckCandidateOverlap(candidate))
        {
            // Blocked — discard candidate, current block keeps falling untouched
            if (logSpawnInfo)
                Debug.Log($"[BlockInstantiator] Candidate {candidateType} BLOCKED — keeping current block.");

            Destroy(candidate);
            return;
        }

        // Clear — commit the swap
        _currentTypeIndex = candidateIndex;

        if (logSpawnInfo)
            Debug.Log($"[BlockInstantiator] Candidate {candidateType} CLEAR — committing swap.");

        _currentBlock.SetActive(false);
        Destroy(_currentBlock);

        candidate.SetActive(true);
        _currentBlock = candidate;
    }

    private bool CheckCandidateOverlap(GameObject candidate)
    {
        if (_gameManager == null || _gameManager.motherPlatform == null)
            return false;

        foreach (Transform candidateChild in candidate.transform)
        {
            Vector3 cPos = candidateChild.position;

            foreach (Transform motherChild in _gameManager.motherPlatform.transform)
            {
                Vector3 mPos = motherChild.position;

                bool xMatch = Mathf.Round(cPos.x * 10f) == Mathf.Round(mPos.x * 10f);
                bool yMatch = Mathf.Round(cPos.y * 10f) == Mathf.Round(mPos.y * 10f);
                bool zMatch = Mathf.Round(cPos.z * 10f) == Mathf.Round(mPos.z * 10f);

                if (xMatch && yMatch && zMatch)
                    return true;
            }
        }

        return false;
    }

    public void SpawnNextBlock()
    {
        _currentTypeIndex = UnityEngine.Random.Range(0, _cycleOrder.Length);
        BlockType chosen  = _cycleOrder[_currentTypeIndex];

        if (logSpawnInfo)
            Debug.Log($"[BlockInstantiator] Randomiser chose: {chosen}");

        InstantiateBlock(chosen, spawnPosition);
    }

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