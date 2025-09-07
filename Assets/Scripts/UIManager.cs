using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Lives UI")]
    public TMP_Text livesText; // drag here in inspector

    [Header("Stage UI")]
    public TMP_Text stageText; // drag another TMP text here for Stage/Round

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

    public void UpdateStageUI(int stage, int round, int roundsPerStage)
    {
        if (stageText != null)
            stageText.text = $"Stage {stage} - Round {round}/{roundsPerStage}";
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
