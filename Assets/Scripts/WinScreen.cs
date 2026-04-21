using UnityEngine;
using UnityEngine.SceneManagement;

public class WinScreen : MonoBehaviour
{
    [Tooltip("UI panel displayed when the player reaches the maze exit.")]
    public GameObject winPanel;

    [Tooltip("Name of the main menu scene to return to.")]
    public string mainMenuSceneName = "MainMenu";

    private void Awake()
    {
        if (winPanel != null)
            winPanel.SetActive(false);
    }

    public void ShowWinScreen()
    {
        Debug.Log($"ShowWinScreen called! WinPanel assigned: {winPanel != null}");

        if (winPanel != null)
        {
            winPanel.SetActive(true);
            Debug.Log("Win panel activated!");
        }
        else
        {
            Debug.LogWarning("WinScreen: winPanel is not assigned!");
        }

        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void HideWinScreen()
    {
        if (winPanel != null)
            winPanel.SetActive(false);

        Time.timeScale = 1f;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;

        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogWarning("WinScreen: mainMenuSceneName is not assigned.");
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}