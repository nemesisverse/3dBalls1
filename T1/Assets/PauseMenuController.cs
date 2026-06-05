using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pauseUI;

    [Header("Game Input (disabled when paused)")]
    [SerializeField] private GameObject[] disableOnPause;

    [Header("Buttons")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button homeButton;

    private bool isPaused;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (pauseButton)   pauseButton.onClick.AddListener(PauseGame);
        if (resumeButton)  resumeButton.onClick.AddListener(ResumeGame);
        if (retryButton)   retryButton.onClick.AddListener(RetryGame);
        if (homeButton)    homeButton.onClick.AddListener(GoHome);
    }

    private void Start()
    {
        pauseUI.SetActive(false);
        isPaused = false;
        Time.timeScale = 1f;
    }

    public void PauseGame()
    {
        isPaused = true;
        pauseUI.SetActive(true);
        if (pauseButton) pauseButton.gameObject.SetActive(false);
        SetGameInput(false);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        pauseUI.SetActive(false);
        if (pauseButton) pauseButton.gameObject.SetActive(true);
        SetGameInput(true);
        Time.timeScale = 1f;
    }

    private void SetGameInput(bool active)
    {
        foreach (var obj in disableOnPause)
        {
            if (obj) obj.SetActive(active);
        }
    }

    public void RetryGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoHome()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main Menu");
    }
}