using UnityEngine;
using UnityEngine.SceneManagement;

public class WinScreen : MonoBehaviour
{
    [Tooltip("UI panel displayed when the player reaches the maze exit.")]
    public GameObject winPanel;

    [Tooltip("Name of the main menu scene to return to.")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Audio")]
    [Tooltip("Sound to play when the player wins.")]
    public AudioClip winSound;

    [Header("Timer")]
    [Tooltip("UI text to display the completion time.")]
    public TMPro.TMP_Text timerText;

    private AudioSource audioSource;
    private float _completionTime = 0f;

    private void Awake()
    {
        if (winPanel != null)
            winPanel.SetActive(false);

        // Get or create an AudioSource for playing win sound
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void ShowWinScreen(float completionTime = 0f)
    {
        Debug.Log($"ShowWinScreen called! WinPanel assigned: {winPanel != null}");

        // Store completion time
        _completionTime = completionTime;

        if (winPanel != null)
        {
            winPanel.SetActive(true);
            Debug.Log("Win panel activated!");
        }
        else
        {
            Debug.LogWarning("WinScreen: winPanel is not assigned!");
        }

        // Update timer display
        UpdateTimerDisplay();

        // Play win sound
        if (winSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(winSound);
            Debug.Log("Win sound played!");
        }
        else if (winSound == null)
        {
            Debug.LogWarning("WinScreen: winSound is not assigned!");
        }

        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    /// <summary>Updates the timer display text.</summary>
    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int minutes = (int)(_completionTime / 60f);
            int seconds = (int)(_completionTime % 60f);
            int milliseconds = (int)((_completionTime % 1f) * 100f);
            timerText.text = $"Time Taken: {minutes:00}:{seconds:00}:{milliseconds:00}";
            Debug.Log($"Timer updated: {timerText.text}");
        }
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
        GameSetup.SelectedSeed = Random.Range(0, 999999); 
        GameSetup.MapWidth = Random.Range(10, 30);
        GameSetup.MapHeight = Random.Range(10, 30);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void RetryLevel()
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