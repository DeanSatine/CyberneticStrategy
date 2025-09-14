using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Lives UI")]
    public TMP_Text livesText;

    [Header("Stage UI")]
    public TMP_Text stageText;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;

    [Header("Fight Button UI")]
    public GameObject fightButton; // 👈 drag your button object here

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

    public void ShowFightButton(bool visible)
    {
        if (fightButton != null)
            fightButton.SetActive(visible);
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

    public void OnFightButtonPressed()
    {
        StageManager.Instance.EnterCombatPhase();
    }

}
