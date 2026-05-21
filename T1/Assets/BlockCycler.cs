// BlockCycler.cs
// Requires: Window > Package Manager > Input System (installed & enabled)
// Player Settings > Active Input Handling = "Input System Package" or "Both"

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public enum BlockType
{
    TBlock  = 0,
    T1Block = 1,
    T2Block = 2
}

public class BlockCycler : MonoBehaviour
{
    // ----------------------------------------------------------------
    //  Inspector fields
    // ----------------------------------------------------------------
    [Header("Block Prefabs")]
    public GameObject tBlockPrefab;
    public GameObject t1BlockPrefab;
    public GameObject t2BlockPrefab;

    [Header("References")]
    [Tooltip("World-space point where new blocks are spawned.")]
    public Transform spawnPoint;
    public GameManager gameManager;

    [Header("Tap Detection")]
    [Tooltip("Max seconds a touch may last to be counted as a tap.")]
    public float tapMaxDuration = 0.25f;
    [Tooltip("Max pixels a touch may travel to be counted as a tap.")]
    public float tapMaxMovePx   = 25f;

    // ----------------------------------------------------------------
    //  Private state
    // ----------------------------------------------------------------
    BlockType     _currentType;
    GameObject    _currentBlockGO;
    IFallingBlock _currentBlock;

    // tap tracking
    float   _touchStartTime;
    Vector2 _touchStartPos;
    bool    _tracking;

    // ----------------------------------------------------------------
    //  Unity messages
    // ----------------------------------------------------------------
    void Awake()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();
    }

    void OnEnable()
    {
        // EnhancedTouch must be explicitly enabled for Touch.activeTouches to work
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Start()
    {
        _currentType = (BlockType)Random.Range(0, 3);
        Debug.Log($"[BlockCycler] Starting block: {_currentType}");
        SpawnBlock(_currentType, startIndex: 2);
    }

    void Update()
    {
        HandleTapInput();
    }

    // ----------------------------------------------------------------
    //  Tap detection — new Input System
    //  Works on device (EnhancedTouch) and in Editor (Mouse)
    // ----------------------------------------------------------------
    void HandleTapInput()
    {
        // ── Mobile / device touches ──────────────────────────────────
        foreach (var touch in Touch.activeTouches)
        {
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                _touchStartTime = Time.time;
                _touchStartPos  = touch.screenPosition;
                _tracking       = true;
            }
            else if (_tracking &&
                     (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                      touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled))
            {
                _tracking = false;
                float duration = Time.time - _touchStartTime;
                float distance = Vector2.Distance(touch.screenPosition, _touchStartPos);
                if (duration < tapMaxDuration && distance < tapMaxMovePx)
                    CycleBlock();
            }
            // Multi-touch cancels tap tracking
            else if (_tracking && Touch.activeTouches.Count > 1)
            {
                _tracking = false;
            }
        }

        // ── Editor / mouse fallback ──────────────────────────────────
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            _touchStartTime = Time.time;
            _touchStartPos  = mouse.position.ReadValue();
            _tracking       = true;
        }

        if (_tracking && mouse.leftButton.wasReleasedThisFrame)
        {
            _tracking = false;
            float duration = Time.time - _touchStartTime;
            float distance = Vector2.Distance(mouse.position.ReadValue(), _touchStartPos);
            if (duration < tapMaxDuration && distance < tapMaxMovePx)
                CycleBlock();
        }
    }

    // ----------------------------------------------------------------
    //  Cycle logic
    // ----------------------------------------------------------------
    void CycleBlock()
    {
        int handoffIndex = 2;

        if (_currentBlock != null)
        {
            handoffIndex = _currentBlock.CurrentIndex;
            _currentBlock.StopMovement();
        }

        if (_currentBlockGO != null)
        {
            Destroy(_currentBlockGO);
            _currentBlockGO = null;
            _currentBlock   = null;
        }

        // Advance enum: 0 → 1 → 2 → 0 → …
        _currentType = (BlockType)(((int)_currentType + 1) % 3);
        Debug.Log($"[BlockCycler] Cycling to {_currentType}  (handoff index {handoffIndex})");
        SpawnBlock(_currentType, handoffIndex);
    }

    // ----------------------------------------------------------------
    //  Spawning
    // ----------------------------------------------------------------
    void SpawnBlock(BlockType type, int startIndex)
    {
        GameObject prefab = PrefabFor(type);
        if (prefab == null)
        {
            Debug.LogWarning($"[BlockCycler] Prefab for {type} is not assigned in the Inspector!");
            return;
        }

        Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;

        // Awake() runs immediately; Start() runs next frame —
        // so StartIndex is guaranteed to be set before Start() reads it.
        _currentBlockGO = Instantiate(prefab, pos, Quaternion.identity);
        _currentBlock   = _currentBlockGO.GetComponent<IFallingBlock>();

        if (_currentBlock != null)
            _currentBlock.StartIndex = startIndex;
        else
            Debug.LogWarning($"[BlockCycler] '{prefab.name}' has no IFallingBlock component!");
    }

    GameObject PrefabFor(BlockType type)
    {
        switch (type)
        {
            case BlockType.TBlock:  return tBlockPrefab;
            case BlockType.T1Block: return t1BlockPrefab;
            case BlockType.T2Block: return t2BlockPrefab;
            default:                return null;
        }
    }
}