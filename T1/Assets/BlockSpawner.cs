using UnityEngine;

public class BlockSpawner : MonoBehaviour
{
    [Header("Block Prefabs")]
    public GameObject[] blockPrefabs = new GameObject[5];

    [Header("Spawn Settings")]
    public Vector3 spawnPosition = new Vector3(0, 20, 0);

    [Header("Audio")]
    public AudioClip spawnSound;

    private GameObject _currentBlock;
    private AudioSource _audioSource;
    private bool _isFirstSpawn = true;

    // ================================================================
    //  Unity lifecycle
    // ================================================================

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        SpawnRandomBlock();
    }

    void Update()
    {
        // Unity's overloaded == returns true when the GameObject has been
        // destroyed, so this fires exactly once after BlockTInstantiator
        // is destroyed by a landing block.
        if (_currentBlock == null)
        {
            SpawnRandomBlock();
        }
    }

    // ================================================================
    //  Spawn
    // ================================================================

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

        // Skip sound on first spawn (game load), play on all subsequent spawns
        if (_isFirstSpawn)
        {
            _isFirstSpawn = false;
        }
        else if (spawnSound != null)
        {
            _audioSource.PlayOneShot(spawnSound);
        }

        _currentBlock = Instantiate(prefab, spawnPosition, Quaternion.identity);
        Debug.Log($"[BlockSpawner] Spawned: {prefab.name} at {spawnPosition}");
        return _currentBlock;
    }
}