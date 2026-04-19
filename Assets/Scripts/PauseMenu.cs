using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PauseMenu : MonoBehaviour
{
    [Tooltip("The UI panel that will be shown when the game is paused.")]
    public GameObject pausePanel;

    [Tooltip("The scene name for the main menu. Leave blank to keep current scene.")]
    public string mainMenuSceneName = "MainMenu";

    private static bool _isPaused;
    public static bool IsPaused => _isPaused;

    private void Awake()
    {
        SetPaused(false);
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
#else
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
#endif
    }

    public void TogglePause()
    {
        SetPaused(!_isPaused);
    }

    public void Pause()
    {
        SetPaused(true);
    }

    public void Resume()
    {
        SetPaused(false);
    }

    public void RestartLevel()
    {
        SetPaused(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMainMenu()
    {
        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogWarning("PauseMenu: mainMenuSceneName is not assigned.");
            return;
        }

        SetPaused(false);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void SetPaused(bool pause)
    {
        _isPaused = pause;
        Time.timeScale = pause ? 0f : 1f;

        if (pausePanel != null)
            pausePanel.SetActive(pause);

        Cursor.visible = pause;
        Cursor.lockState = pause ? CursorLockMode.None : CursorLockMode.Locked;
    }
}

