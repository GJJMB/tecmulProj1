using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenu : MonoBehaviour
{
    [Tooltip("The scene name used for gameplay.")]
    public string gameSceneName = "SampleScene";
    public GameObject settingsPanel;

    private void Awake()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void PlayGame()
    {
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogWarning("MainMenu: gameSceneName is not assigned.");
            return;
        }
        SceneManager.LoadScene(gameSceneName);
    }
    public void ShowSettings()
    {
        Debug.Log($"ShowSettings called! SettingsPanel assigned: {settingsPanel != null}");

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            Debug.Log("Settings panel activated!");
        }
        else
        {
            Debug.LogWarning("settingsPanel is not assigned!");
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("MainMenu: sceneName is empty.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
