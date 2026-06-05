using System.Collections;
using UnityEngine;

public class SplashScreenController : MonoBehaviour
{
    [Header("Splash Screen Reference")]
    [SerializeField] private GameObject splashScreen;
    [SerializeField] private CanvasGroup splashCanvasGroup;

    [Header("Timing (seconds)")]
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float holdDuration = 1.5f;
    [SerializeField] private float fadeOutDuration = 1f;

    [Header("Settings")]
    [SerializeField] private bool showOnlyOnFirstLaunch = true;

    private const string FIRST_LAUNCH_KEY = "HasLaunchedBefore";

    private void Awake()
    {
        if (splashScreen == null)
        {
            Debug.LogWarning("[SplashScreenController] Splash Screen GameObject not assigned.");
            return;
        }

        // Auto-grab or add CanvasGroup if not assigned
        if (splashCanvasGroup == null)
        {
            splashCanvasGroup = splashScreen.GetComponent<CanvasGroup>();
            if (splashCanvasGroup == null)
                splashCanvasGroup = splashScreen.AddComponent<CanvasGroup>();
        }

        // Already shown before → hide immediately and bail out
        if (showOnlyOnFirstLaunch && PlayerPrefs.HasKey(FIRST_LAUNCH_KEY))
        {
            splashScreen.SetActive(false);
            return;
        }

        // Mark as shown so it never appears again
        PlayerPrefs.SetInt(FIRST_LAUNCH_KEY, 1);
        PlayerPrefs.Save();

        splashScreen.SetActive(true);
        StartCoroutine(PlaySplashSequence());
    }

    private IEnumerator PlaySplashSequence()
    {
        splashCanvasGroup.alpha = 0f;
        splashCanvasGroup.blocksRaycasts = true;
        splashCanvasGroup.interactable = false;

        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            splashCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        splashCanvasGroup.alpha = 1f;

        // Hold
        yield return new WaitForSecondsRealtime(holdDuration);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            splashCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
            yield return null;
        }
        splashCanvasGroup.alpha = 0f;
        splashCanvasGroup.blocksRaycasts = false;

        splashScreen.SetActive(false);
    }

    // For testing — right-click the component in Inspector to re-trigger splash on next run
    [ContextMenu("Reset First Launch Flag")]
    private void ResetFirstLaunchFlag()
    {
        PlayerPrefs.DeleteKey(FIRST_LAUNCH_KEY);
        PlayerPrefs.Save();
        Debug.Log("[SplashScreenController] First launch flag cleared. Splash will show on next launch.");
    }
}