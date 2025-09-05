using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Lives UI")]
    public TMP_Text livesText; // drag a Text or TMP_Text here

    [Header("Game Over UI")]
    public GameObject gameOverPanel; // panel with "Game Over" + Retry button

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    public void UpdateLivesUI(int lives)
    {
        if (livesText != null)
            livesText.text = $"{lives}";
    }

    public void ShowGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    public void RetryGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
