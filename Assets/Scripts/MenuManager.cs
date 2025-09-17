using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Name of your game scene to load when pressing Play.")]
    public string gameSceneName = "GameScene";

    [Header("UI References")]
    public GameObject creditsCanvas;

    private void Start()
    {
        if (creditsCanvas != null)
            creditsCanvas.SetActive(false); // Hide credits at start
    }

    private void Update()
    {
        // Close credits with Escape key
        if (creditsCanvas != null && creditsCanvas.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            HideCredits();
        }
    }

    // Called by Play button
    public void PlayGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // ✅ NEW: Load Test scene
    public void LoadTestScene()
    {
        SceneManager.LoadScene("Test");
    }

    // Called by Credits button
    public void ShowCredits()
    {
        if (creditsCanvas != null)
            creditsCanvas.SetActive(true);
    }

    // Called by Back button inside credits
    public void HideCredits()
    {
        if (creditsCanvas != null)
            creditsCanvas.SetActive(false);
    }

    // Optional quit button
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit game!"); // Only shows in editor
    }
}
