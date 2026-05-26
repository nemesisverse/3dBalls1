using UnityEngine;

public class BlockSwitcher : MonoBehaviour
{
    private ISwitchable activeBlock;   // currently falling / moving
    private ISwitchable pausedBlock;   // frozen mid-air

    void OnEnable()  => TapInput.OnTap += HandleTap;
    void OnDisable() => TapInput.OnTap -= HandleTap;

    // ── Called by each movement script in its Start() ──────────────────────
    public void RegisterBlock(ISwitchable block)
    {
        if (activeBlock == null)
        {
            activeBlock = block;
            activeBlock.IsPaused = false;          // first registered → moves immediately
        }
        else if (pausedBlock == null)
        {
            pausedBlock = block;
            pausedBlock.IsPaused = true;           // second registered → waits frozen
        }
        // if both slots are full, new block is ignored until one slot frees
    }

    // ── Called by each movement script just before it reparents ────────────
    public void UnregisterBlock(ISwitchable block)
    {
        if      (activeBlock == block) activeBlock = null;
        else if (pausedBlock == block) pausedBlock = null;

        // if active just landed, automatically promote the paused block
        if (activeBlock == null && pausedBlock != null)
        {
            activeBlock          = pausedBlock;
            pausedBlock          = null;
            activeBlock.IsPaused = false;
            Debug.Log("[BlockSwitcher] Active block landed → promoting paused block.");
        }
    }

    // ── Tap handler ─────────────────────────────────────────────────────────
    void HandleTap(Vector2 _)
    {
        if (activeBlock == null || pausedBlock == null)
        {
            Debug.Log("[BlockSwitcher] Tap ignored — need two blocks in play.");
            return;
        }

        activeBlock.IsPaused = true;
        pausedBlock.IsPaused = false;
        (activeBlock, pausedBlock) = (pausedBlock, activeBlock);

        Debug.Log($"[BlockSwitcher] Switched. New active savedIndex={activeBlock.SavedIndex}");
    }
}