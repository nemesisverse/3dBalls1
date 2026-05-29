using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Attach to the GameOver GameObject.
/// Wire Retry and Home (Quit) buttons in the Inspector,
/// then call GameOverManager.Instance.ShowGameOver() from GameManager
/// when a block can no longer be placed.
/// </summary>
public class GameOverManager : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────
    public static GameOverManager Instance { get; private set; }

    // ── Inspector references ─────────────────────────────────────────────────
    [Header("Panel")]
    [Tooltip("The root GameOver panel GameObject (this object, or a child canvas group).")]
    public GameObject gameOverPanel;

    [Header("Buttons")]
    [Tooltip("The Retry button child of GameOver.")]
    public Button retryButton;

    [Tooltip("The Home / Quit button child of GameOver.")]
    public Button homeButton;

    [Header("Scene Settings")]
    [Tooltip("Build index of the main-menu scene. Set to 0 if your menu is scene 0.")]
    public int mainMenuSceneIndex = 0;

    [Tooltip("If true, Home loads the main-menu scene. If false, it quits the application.")]
    public bool homeLoadsMenu = true;

    // ── Internal state ───────────────────────────────────────────────────────
    private bool _isGameOver = false;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Always hide the panel at game start
        HideGameOver();

        // Wire up buttons (safe even if already wired in Inspector)
        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryClicked);

        if (homeButton != null)
            homeButton.onClick.AddListener(OnHomeClicked);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Call this from GameManager when the game is lost.
    /// e.g. GameOverManager.Instance.ShowGameOver();
    /// </summary>
    public void ShowGameOver()
    {
        if (_isGameOver) return;   // guard against double-calls
        _isGameOver = true;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        Debug.Log("[GameOverManager] Game Over shown.");
    }

    /// <summary>
    /// Hides the panel (called on Start and after scene reload).
    /// </summary>
    public void HideGameOver()
    {
        _isGameOver = false;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    // ── Button handlers ───────────────────────────────────────────────────────

    private void OnRetryClicked()
    {
        Debug.Log("[GameOverManager] Retry clicked — reloading scene.");

        // Ensure time is running (in case you paused it on game over)
        Time.timeScale = 1f;

        // Reload the active scene — the cleanest full reset:
        // destroys all GameObjects, reinitialises SphericalGrid,
        // GameManager, spawners, and block scripts from scratch.
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnHomeClicked()
    {
        Debug.Log("[GameOverManager] Home clicked.");

        Time.timeScale = 1f;

        if (homeLoadsMenu)
        {
            SceneManager.LoadScene(mainMenuSceneIndex);
        }
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}