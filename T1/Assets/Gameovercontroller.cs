using UnityEngine;
using UnityEngine.SceneManagement;

// ================================================================
//  GameOverController
//  ---------------------------------------------------------------
//  Attach to any ALWAYS-ACTIVE GameObject in the scene
//  (e.g. create a new empty GO called "GameOverController").
//
//  Inspector wiring:
//    • gameOverPanel  → drag the GameOver GameObject here
//    • gameplayCanvas → drag the main gameplay Canvas here
//                       (the one that holds Fixed Joystick, Slider,
//                        Player Score, etc.)
//    • blockSpawner   → drag the Block Spawner GameObject here
//
//  Button wiring (GameOver panel):
//    Retry button  OnClick → GameOverController.OnRetry()
//    Home  button  OnClick → GameOverController.OnHome()
//    Quit  button  OnClick → GameOverController.OnQuit()
// ================================================================

public class GameOverController : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────
    //  Accessible from any movement script without a pre-wired
    //  Inspector field: GameOverController.Instance?.TriggerGameOver()
    // ─────────────────────────────────────────────────────────────
    public static GameOverController Instance { get; private set; }

    [Header("UI References")]
    public GameObject gameOverPanel;    // The GameOver UI panel / canvas
    public Canvas     gameplayCanvas;   // Main gameplay canvas (joystick, score, etc.)

    [Header("Scene References")]
    public GameObject blockSpawner;     // The Block Spawner GameObject

    // ── State ─────────────────────────────────────────────────────
    public bool IsGameOver { get; private set; }

    private SwipeInput swipeInput;

    // ================================================================
    //  Lifecycle
    // ================================================================

    void Awake()
    {
        Instance   = this;
        swipeInput = FindFirstObjectByType<SwipeInput>();

        // Make sure the Game Over panel is hidden when the scene loads
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    // ================================================================
    //  TRIGGER GAME OVER
    //  ---------------------------------------------------------------
    //  Called by any movement script (TMovement, T1Movement, T2Movement…)
    //  when a block lands at radius coordinate index ≤ 3, meaning the
    //  outer zone has been reached and the stack has overflowed.
    //
    //  Safe to call multiple times — idempotent guard at the top.
    // ================================================================

    public void TriggerGameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        // ── Freeze the entire game world ──────────────────────────
        //  WaitForSeconds coroutines pause automatically.
        //  SwipeInput is also explicitly disabled below for safety.
        Time.timeScale = 0f;

        // ── Kill swipe / touch rotation input ─────────────────────
        if (swipeInput != null)
            swipeInput.enabled = false;

        // ── Stop the spawner so no new blocks are queued ──────────
        if (blockSpawner != null)
            blockSpawner.SetActive(false);

        // ── Hide gameplay HUD (joystick, slider, score…) ──────────
        //  Disabling the Canvas component also blocks all raycasting
        //  for that canvas, so buttons and joystick stop receiving
        //  touch events entirely.
        if (gameplayCanvas != null)
            gameplayCanvas.enabled = false;

        // ── Reveal the Game Over screen ───────────────────────────
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    // ================================================================
    //  BUTTON CALLBACKS
    //  Wire these to the Retry / Home / Quit buttons in the Inspector.
    // ================================================================

    // Restart the current scene from scratch
    public void OnRetry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Return to main menu (adjust scene index / name to match your project)
    public void OnHome()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    // Quit the application (or stop play mode in the editor)
    public void OnQuit()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}