using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ButtonManager : MonoBehaviour
{
    public GameObject pauseMenu;
    public GameObject mother;
    public Slider musicSlider;

    public GameObject gameOver;
    public GameObject optionMenu;

    void Start()
    {
        pauseMenu.SetActive(false);

        float saved = PlayerPrefs.GetFloat("MusicVolume", 1f);
        musicSlider.value = saved;
        AudioListener.volume = saved;   // apply on load

        musicSlider.onValueChanged.AddListener(SetMusicVolume);

        optionMenu.SetActive(false);
    }
    public void option()
    {
        gameOver.SetActive(false);
        optionMenu.SetActive(true);
    }

    public void CloseOption()
    {
        optionMenu.SetActive(false);
        gameOver.SetActive(true);
    }
    public void SetMusicVolume(float volume)
    {
        AudioListener.volume = volume;                   // controls ALL audio globally
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }

    public void showMenu()
    {
        pauseMenu.SetActive(true);
        Time.timeScale = 0f;
        mother.SetActive(false);
    }

    public void Resume()
    {
        pauseMenu.SetActive(false);
        Time.timeScale = 1f;
        mother.SetActive(true);
    }

    public void Retry()
    {
        mother.SetActive(true);
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Home()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main Menu");
    }
}