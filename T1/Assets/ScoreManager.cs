using TMPro;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;

    [Header("Game Over Screen")]
    [SerializeField] private TextMeshProUGUI gameOverScoreText;      // wire to "current Scored"
    [SerializeField] private TextMeshProUGUI gameOverHighScoreText;  // wire to "High Scored"

    [Header("Audio")]
    [SerializeField] private AudioClip ringClearSound;

    private const string HighScoreKey = "HighScore";

    private int score = 0;
    private int highScore = 0;
    private AudioSource audioSource;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    void Start() => UpdateUI();

    public void AddRingScore()
    {
        score += 1;

        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
            Debug.Log($"[Score] New high score! HighScore = {highScore}");
        }

        UpdateUI();

        if (ringClearSound != null)
            audioSource.PlayOneShot(ringClearSound);

        Debug.Log($"[Score] Ring completed! Score = {score}");
    }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = score.ToString();

        if (highScoreText != null)
            highScoreText.text = highScore.ToString();

        // Always keep Game Over texts in sync — safe to set on inactive GameObjects
        if (gameOverScoreText != null)
            gameOverScoreText.text = score.ToString();

        if (gameOverHighScoreText != null)
            gameOverHighScoreText.text = highScore.ToString();
    }
}