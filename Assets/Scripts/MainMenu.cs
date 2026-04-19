using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Tooltip("The scene name used for gameplay.")]
    public string gameSceneName = "SampleScene";

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
