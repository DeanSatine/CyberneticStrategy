using UnityEngine;

public class StageManager : MonoBehaviour
{
    public enum GamePhase
    {
        Prep,
        Combat
    }

    public static StageManager Instance;

    [Header("Stage settings")]
    public int currentStage = 1;
    public int roundInStage = 1;
    public int roundsPerStage = 3;

    [Header("Player lives")]
    public int maxLives = 3;
    private int currentLives;

    public GamePhase currentPhase { get; private set; } = GamePhase.Prep;

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
            UIManager.Instance.ShowFightButton(true); // start in prep
        }

        EnterPrepPhase(); // start in prep
    }

    public void OnCombatEnd(bool playerWon)
    {
        if (playerWon)
        {
            Debug.Log("✅ Player won the round!");
            ReturnToPrepPhase();
            return;
        }
        else
        {
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
        }

        NextRound();
        EnterPrepPhase();
    }

    private void ReturnToPrepPhase()
    {
        Debug.Log("🔄 Returning to Prep Phase...");

        // 1. Restore player units to their saved hexes
        foreach (var kvp in CombatManager.Instance.GetSavedPlayerPositions())
        {
            UnitAI unit = kvp.Key;
            HexTile tile = kvp.Value;

            if (unit != null && unit.isAlive)
            {
                unit.SetState(UnitAI.UnitState.BoardIdle);
                unit.AssignToTile(tile);
                unit.animator.SetBool("IsRunning", false);
            }
        }

        // 2. Spawn next enemy wave (but idle until fight button pressed)
        EnemyWaveManager.Instance.SpawnEnemyWave(currentStage);

        // 3. Show fight button again
        if (UIManager.Instance != null)
            UIManager.Instance.ShowFightButton(true);

        // 4. Advance round counters
        NextRound();
    }

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
    }

    private void EnterPrepPhase()
    {
        currentPhase = GamePhase.Prep;

        // reset player units back to their tiles
        GameManager.Instance.ResetPlayerUnits();

        // spawn enemies for new round
        EnemyWaveManager.Instance.SpawnEnemyWave(currentStage);

        // allow repositioning
        if (UIManager.Instance != null)
            UIManager.Instance.ShowFightButton(true);
    }

    public void EnterCombatPhase()
    {
        currentPhase = GamePhase.Combat;

        if (UIManager.Instance != null)
            UIManager.Instance.ShowFightButton(false);

        CombatManager.Instance.StartCombat();
    }
}
