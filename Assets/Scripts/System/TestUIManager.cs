using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestUIManager : MonoBehaviour
{
    public static TestUIManager Instance;

    [Header("Lives UI")]
    public TMP_Text livesText;

    [Header("Stage UI")]
    public TMP_Text stageText;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;

    [Header("Fight Button UI")]
    public GameObject fightButton; // 👈 drag your button object here
    [Header("Win/Lose UI")]
    public GameObject winLosePanel;    // Simple panel with text
    public TMP_Text winLoseText;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (winLosePanel != null)
            winLosePanel.SetActive(false);
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
    public void ShowStaticAugmentSelection(int currentStage, int currentRound)
    {
        if (AugmentManager.Instance != null)
        {
            AugmentManager.Instance.ShowStaticAugmentSelection(currentStage, currentRound);
        }
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
    // ✅ ENHANCED: Color-coded win/lose display
    public IEnumerator ShowWinLose(bool playerWon)
    {
        string message = playerWon ? "WIN!" : "LOSE!";
        Color textColor = playerWon ? Color.green : Color.red;

        Debug.Log($"🎭 Showing win/lose UI: {message}");

        if (winLosePanel != null)
        {
            winLosePanel.SetActive(true);

            if (winLoseText != null)
            {
                winLoseText.text = message;
                winLoseText.color = textColor;  // Green for WIN, red for LOSE
            }
        }

        yield return new WaitForSeconds(2.0f);

        if (winLosePanel != null)
            winLosePanel.SetActive(false);

        Debug.Log($"🎭 Win/lose UI hidden");
    }

    // ✅ NEW: Check if there are any player units on the board
    private bool HasUnitsOnBoard()
    {
        if (TestGameManager.Instance == null) return false;

        var playerUnits = TestGameManager.Instance.GetPlayerUnits();

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
        if (TestGameManager.Instance == null) return 0;

        int count = 0;
        var playerUnits = TestGameManager.Instance.GetPlayerUnits();

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
        TestStageManager.Instance.EnterCombatPhase();
    }
}
