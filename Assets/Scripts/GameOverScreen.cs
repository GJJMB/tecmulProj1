using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverScreen : MonoBehaviour
{
    [Tooltip("UI panel displayed when the player loses.")]
    public GameObject gameOverPanel;

    [Tooltip("Name of the main menu scene to return to.")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Audio")]
    [Tooltip("Sound to play when the player loses.")]
    public AudioClip loseSound;

    private AudioSource audioSource;

    private void Awake()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // Get or create an AudioSource for playing lose sound
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void ShowGameOverScreen()
    {
        Debug.Log("ShowGameOverScreen called!");
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Debug.Log("Game over panel activated!");
        }
        else
        {
            Debug.LogWarning("GameOverScreen: gameOverPanel is not assigned!");
        }

        // Play lose sound
        if (loseSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(loseSound);
            Debug.Log("Lose sound played!");
        }
        else if (loseSound == null)
        {
            Debug.LogWarning("GameOverScreen: loseSound is not assigned!");
        }

        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void HideGameOverScreen()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

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
            Debug.LogWarning("GameOverScreen: mainMenuSceneName is not assigned.");
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