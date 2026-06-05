using UnityEngine;

public class BlockSpawner : MonoBehaviour
{
    [Header("Block Prefabs")]
    public GameObject[] blockPrefabs = new GameObject[5];

    [Header("Spawn Settings")]
    public Vector3 spawnPosition = new Vector3(0, 20, 0);

    private GameObject _currentBlock;

    void Start()
    {
        SpawnRandomBlock();
    }

    void Update()
    {
        if (_currentBlock == null)
        {
            SpawnRandomBlock();
        }
    }

    public GameObject SpawnRandomBlock()
    {
        if (blockPrefabs.Length == 0)
        {
            Debug.LogWarning("BlockSpawner: No prefabs assigned!");
            return null;
        }

        int randomIndex = Random.Range(0, blockPrefabs.Length);
        GameObject prefab = blockPrefabs[randomIndex];

        if (prefab == null)
        {
            Debug.LogWarning($"BlockSpawner: Prefab at index {randomIndex} is null!");
            return null;
        }

        _currentBlock = Instantiate(prefab, spawnPosition, Quaternion.identity);
        Debug.Log($"[BlockSpawner] Spawned: {prefab.name} at {spawnPosition}");
        return _currentBlock;
    }
}