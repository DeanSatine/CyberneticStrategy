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

    private void Start()
    {
        // Check initial state
        UpdateFightButtonVisibility();
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

    // ✅ NEW: Check and update fight button based on units on board
    public void UpdateFightButtonVisibility()
    {
        bool hasUnitsOnBoard = HasUnitsOnBoard();
        ShowFightButton(hasUnitsOnBoard);

        Debug.Log($"🎯 Fight button visibility: {hasUnitsOnBoard} (Units on board: {GetUnitsOnBoardCount()})");
    }

    // ✅ NEW: Check if there are any player units on the board
    private bool HasUnitsOnBoard()
    {
        if (GameManager.Instance == null) return false;

        var playerUnits = GameManager.Instance.GetPlayerUnits();

        foreach (var unit in playerUnits)
        {
            if (unit != null && unit.isAlive && unit.currentState == UnitAI.UnitState.BoardIdle)
            {
                return true;
            }
        }

        return false;
    }

    // ✅ NEW: Helper method to count units on board (for debugging)
    private int GetUnitsOnBoardCount()
    {
        if (GameManager.Instance == null) return 0;

        int count = 0;
        var playerUnits = GameManager.Instance.GetPlayerUnits();

        foreach (var unit in playerUnits)
        {
            if (unit != null && unit.isAlive && unit.currentState == UnitAI.UnitState.BoardIdle)
            {
                count++;
            }
        }

        return count;
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
