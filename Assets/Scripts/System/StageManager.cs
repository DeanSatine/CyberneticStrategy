using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance;

    [Header("Stage settings")]
    public int currentStage = 1;
    public int roundInStage = 1;
    public int roundsPerStage = 3;

    [Header("Player lives")]
    public int maxLives = 3;
    private int currentLives;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        currentLives = maxLives;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateLivesUI(currentLives);
            UIManager.Instance.UpdateStageUI(currentStage, roundInStage, roundsPerStage);
        }

        EnemyWaveManager.Instance.SpawnEnemyWave(currentStage);
    }

    /// <summary>
    /// Called by CombatManager when a round ends.
    /// playerWon == true  => player defeated the enemy wave
    /// playerWon == false => player lost the round (lose a life)
    /// </summary>
    public void OnCombatEnd(bool playerWon)
    {
        if (playerWon)
        {
            Debug.Log("✅ Player won the round!");
            NextRound(); // advance round/stage as before
            return;
        }

        // player lost the round
        Debug.Log("❌ Player lost the round!");
        currentLives--;

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateLivesUI(currentLives);

        if (currentLives <= 0)
        {
            Debug.Log("💀 Player has no lives left - Game Over.");
            if (UIManager.Instance != null)
                UIManager.Instance.ShowGameOver();
            return;
        }

        // Player still has lives — advance the round/respawn new wave
        NextRound();
    }

    /// <summary>
    /// Advance round/stage and spawn the next wave (keeps the original behavior).
    /// </summary>
    public void NextRound()
    {
        roundInStage++;

        if (roundInStage > roundsPerStage)
        {
            roundInStage = 1;
            currentStage++;
            Debug.Log($"Stage advanced! Now at Stage {currentStage}");
        }
        else
        {
            Debug.Log($"Round advanced! Stage {currentStage}, Round {roundInStage}");
        }

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateStageUI(currentStage, roundInStage, roundsPerStage);

        EnemyWaveManager.Instance.SpawnEnemyWave(currentStage);

        if (GameManager.Instance != null)
            GameManager.Instance.ResetCombatFlag();
    }
}
