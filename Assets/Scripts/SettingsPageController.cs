using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the Settings Page UI and applies user preferences.
/// Attach this script to your Settings Page Canvas or root GameObject.
/// </summary>
public class SettingsPageController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject settingsPanel;
    public GameObject mainMenuPanel;

    /// <summary>
    /// Shows the settings panel and hides the main menu panel.
    /// </summary>
    public void ShowSettingsPanel()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
    }

    /// <summary>
    /// Returns to the main menu panel and hides the settings panel.
    /// </summary>
    public void ReturnToMainMenu()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }
    [Header("UI References")]
    public Slider volumeSlider;
    public Toggle fullscreenToggle;
    public Dropdown resolutionDropdown;

    [Header("Maze Settings")]
    public TMP_InputField mazeWidthInput;
    public TMP_InputField mazeHeightInput;
    public TMP_InputField numDoorsKeysInput;
    public TMP_InputField mazeSeedInput;
    private Resolution[] resolutions;

    void Start()
    {
        // Populate resolution dropdown
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        var options = new System.Collections.Generic.List<string>();
        int currentResIndex = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResIndex = i;
            }
        }
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResIndex;
        resolutionDropdown.RefreshShownValue();

        // Load saved settings
        if (volumeSlider != null)
            volumeSlider.value = PlayerPrefs.GetFloat("volume", 1f);
        fullscreenToggle.isOn = Screen.fullScreen;

        // Maze settings: load from PlayerPrefs or defaults
        int mw = PlayerPrefs.GetInt("mazeWidth", 10);
        int mh = PlayerPrefs.GetInt("mazeHeight", 10);
        int dk = PlayerPrefs.GetInt("numDoorsKeys", 3);
        int seed = PlayerPrefs.GetInt("mazeSeed", 0);

        mazeWidthInput.text = mw.ToString();
        mazeHeightInput.text = mh.ToString();
        numDoorsKeysInput.text = dk.ToString();
        mazeSeedInput.text = seed > 0 ? seed.ToString() : "";

        // Also update GameSetup for scene transfer
        GameSetup.MapWidth = mw;
        GameSetup.MapHeight = mh;
        GameSetup.NumDoorsKeys = dk;
        GameSetup.SelectedSeed = seed;
    }

    public void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("volume", value);
    }

    public void OnFullscreenToggle(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    public void OnResolutionChanged(int index)
    {
        Resolution res = resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
    }

    public void OnApplyButton()
    {
        // Parse and save settings
        int mw = 10, mh = 10, dk = 3, seed = 0;
        int.TryParse(mazeWidthInput.text, out mw);
        int.TryParse(mazeHeightInput.text, out mh);
        int.TryParse(numDoorsKeysInput.text, out dk);
        int.TryParse(mazeSeedInput.text, out seed);

        // Clamp values
        mw = Mathf.Clamp(mw, 1, 100);
        mh = Mathf.Clamp(mh, 1, 100);
        dk = Mathf.Clamp(dk, 0, 10);

        // Save to PlayerPrefs
        PlayerPrefs.SetInt("mazeWidth", mw);
        PlayerPrefs.SetInt("mazeHeight", mh);
        PlayerPrefs.SetInt("numDoorsKeys", dk);
        PlayerPrefs.SetInt("mazeSeed", seed);
        if (volumeSlider != null)
            PlayerPrefs.SetFloat("volume", volumeSlider.value);
        PlayerPrefs.Save();

        // Parse enemy speed
        float enemySpeed = enemySpeedSlider != null ? enemySpeedSlider.value : 0.2f;
        enemySpeed = Mathf.Clamp(enemySpeed, 0.05f, 1.0f); // Clamp between reasonable values

        // Save enemy speed to PlayerPrefs
        PlayerPrefs.SetFloat("enemyMoveTime", enemySpeed);

        // Update GameSetup for scene transfer
        GameSetup.MapWidth = mw;
        GameSetup.MapHeight = mh;
        GameSetup.NumDoorsKeys = dk;
        GameSetup.SelectedSeed = seed;

        Debug.Log("Settings applied and saved.");
    }
}
