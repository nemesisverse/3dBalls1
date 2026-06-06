using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MainMenuButtons : MonoBehaviour
{
    public GameObject screenLoader;
    public Slider loadingSlider;
    public float minLoadTime = 2f;
    public float holdAtFullTime = 0.4f;

    public void Play()
    {
        screenLoader.SetActive(true);
        StartCoroutine(LoadSceneAsync(0));
    }

    private IEnumerator LoadSceneAsync(int sceneIndex)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);
        operation.allowSceneActivation = false;

        float elapsed = 0f;

        while (true)
        {
            float targetProgress = Mathf.Clamp01(operation.progress / 0.9f);
            elapsed += Time.deltaTime;
            float timeProgress = Mathf.Clamp01(elapsed / minLoadTime);
            float displayedProgress = Mathf.Min(targetProgress, timeProgress);
            loadingSlider.value = displayedProgress;

            if (operation.progress >= 0.9f && displayedProgress >= 1f)
            {
                break;
            }

            yield return null;
        }

        loadingSlider.value = 1f;
        yield return new WaitForSeconds(holdAtFullTime);

        operation.allowSceneActivation = true;
    }

    public void Quit()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}