using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonManager : MonoBehaviour
{
    public GameObject pauseMenu;
    public GameObject mother;

    void Start()
    {
        pauseMenu.SetActive(false);
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
        Time.timeScale = 1f;  // reset before reload, otherwise scene starts frozen
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Home()
{
    Time.timeScale = 1f;  // reset timeScale in case pause menu is open
    SceneManager.LoadScene("Main Menu");
}
}
