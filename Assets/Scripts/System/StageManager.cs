using UnityEngine;

public class StageManager : MonoBehaviour
{
    public enum GamePhase { Prep, Combat }

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

        UIManager.Instance.UpdateLivesUI(currentLives);
        UIManager.Instance.UpdateStageUI(currentStage, roundInStage, roundsPerStage);

        EnterPrepPhase(); // start in prep
    }

    public void OnCombatEnd(bool playerWon)
    {
        if (playerWon)
        {
            Debug.Log("✅ Player won the round!");

            // ✅ Give player 3 gold for winning
            EconomyManager.Instance.AddGold(3);
            Debug.Log("💰 Player earned 3 gold for winning!");
        }
        else
        {
            Debug.Log("❌ Player lost the round!");
            currentLives--;
            UIManager.Instance.UpdateLivesUI(currentLives);

            if (currentLives <= 0)
            {
                Debug.Log("💀 Game Over!");
                UIManager.Instance.ShowGameOver();
                return;
            }

            // ✅ Give player 1 gold even for losing (optional)
            EconomyManager.Instance.AddGold(1);
            Debug.Log("💰 Player earned 1 gold for participation!");
        }

        ResetToPrepPhase();
        NextRound();
    }

    public void EnterPrepPhase()
    {
        Debug.Log("🔄 Entering Prep Phase");
        currentPhase = GamePhase.Prep;

        GameManager.Instance.ResetPlayerUnits();  // snap players back
        EnemyWaveManager.Instance.SpawnEnemyWave(currentStage); // preload enemies

        UIManager.Instance.ShowFightButton(true); // allow pressing fight
    }

    public void EnterCombatPhase()
    {
        Debug.Log("⚔️ Entering Combat Phase");
        currentPhase = GamePhase.Combat;

        UIManager.Instance.ShowFightButton(false);
        CombatManager.Instance.StartCombat();
    }

    private void ResetToPrepPhase()
    {
        // ✅ Reset Eradicator statics first
        EradicatorTrait.ResetAllEradicators();

        foreach (var kvp in CombatManager.Instance.GetSavedPlayerPositions())
        {
            UnitAI unit = kvp.Key;
            HexTile tile = kvp.Value;

            if (unit != null && unit.isAlive)
            {
                unit.ResetAfterCombat();
                unit.AssignToTile(tile);
            }
        }

        // ✅ Reapply traits after all units are reset
        TraitManager.Instance.EvaluateTraits(GameManager.Instance.playerUnits);
        TraitManager.Instance.ApplyTraits(GameManager.Instance.playerUnits);

        // ✅ Reset shop for new round
        ShopManager.Instance.GenerateShop();
        Debug.Log("🛒 Shop reset for new round!");

        EnemyWaveManager.Instance.SpawnEnemyWave(currentStage);
        UIManager.Instance.ShowFightButton(true);

        currentPhase = GamePhase.Prep;
    }

    private void NextRound()
    {
        roundInStage++;
        if (roundInStage > roundsPerStage)
        {
            roundInStage = 1;
            currentStage++;
        }

        UIManager.Instance.UpdateStageUI(currentStage, roundInStage, roundsPerStage);
    }
}
