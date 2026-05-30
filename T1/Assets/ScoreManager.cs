using TMPro;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private AudioClip ringClearSound;

    private int score = 0;
    private AudioSource audioSource;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void Start() => UpdateUI();

    public void AddRingScore()
    {
        score += 1;
        UpdateUI();

        if (ringClearSound != null)
            audioSource.PlayOneShot(ringClearSound);

        Debug.Log($"[Score] Ring completed! Score = {score}");
    }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = score.ToString();
    }
}