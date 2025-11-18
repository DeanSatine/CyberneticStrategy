using System.Collections.Generic;
using UnityEngine;

public class BaseStageManager : MonoBehaviour
{
    public enum GamePhase { Prep, Combat }

    public static BaseStageManager Instance;

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
        CheckForAugmentSelection();
    }

    public void OnCombatEnd(bool playerWon)
    {
        if (playerWon)
        {
            Debug.Log("✅ Player won the round!");

            // ✅ Calculate gold based on current round (base 3 + round number)
            int totalRoundNumber = ((currentStage - 1) * roundsPerStage) + roundInStage;
            int goldReward = 3 + totalRoundNumber;

            EconomyManager.Instance.AddGold(goldReward);
            Debug.Log($"💰 Player earned {goldReward} gold for winning round {totalRoundNumber}!");
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
        EnemyWaveManager.Instance.SpawnEnemyWave(currentStage, roundInStage);
        // Call this when a round ends or starts
        HaymakerAbility.CleanupAllDuplicateClones();

        // ✅ UPDATED: Use the new visibility logic instead of always showing
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateFightButtonVisibility();
        }
    }

    public void EnterCombatPhase()
    {
        Debug.Log("⚔️ Entering Combat Phase");
        currentPhase = GamePhase.Combat;
        if (AugmentManager.Instance != null)
        {
            AugmentManager.Instance.OnCombatStart();
        }
        CombatManager.Instance.ClearProjectiles();

        UIManager.Instance.ShowFightButton(false);
        CombatManager.Instance.StartCombat();
    }

    private void ResetToPrepPhase()
    {
        Debug.Log("🔄 Starting ResetToPrepPhase");
        
        CombatManager.Instance.ForceResetCombatState();
        
        currentPhase = GamePhase.Prep;
        Debug.Log("✅ Phase set to Prep");
        
        EradicatorTrait.ResetAllEradicators();
        HyperdriveTrait.ResetAllHyperdriveStacks();
        CombatManager.Instance.ClearProjectiles();

        CombatManager.Instance.RestorePlayerUnitsFromSnapshots();
        
        HaymakerAbility.CleanupAllDuplicateClones();

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
        
        if (AugmentManager.Instance != null)
        {
            AugmentManager.Instance.OnCombatEnd();
        }
        
        TraitManager.Instance.EvaluateTraits(GameManager.Instance.playerUnits);
        TraitManager.Instance.ApplyTraits(GameManager.Instance.playerUnits);

        ShopManager.Instance.GenerateShop();
        Debug.Log("🛒 Shop reset for new round!");

        EnemyWaveManager.Instance.SpawnEnemyWave(currentStage, roundInStage);
        
        UIManager.Instance.ShowFightButton(true);
        
        Debug.Log("✅ ResetToPrepPhase complete");
    }

    private void CheckForAugmentSelection()
    {
        if (AugmentManager.Instance != null &&
            AugmentManager.Instance.ShouldOfferAugment(currentStage, roundInStage, roundsPerStage))
        {
            int totalRound = ((currentStage - 1) * roundsPerStage) + roundInStage;
            Debug.Log($"🎯 Offering augment selection for Stage {currentStage}, Round {roundInStage} (Total Round {totalRound})");

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowStaticAugmentSelection(currentStage, roundInStage);
            }
        }
    }

    // Update your existing NextRound() method to include this line:
    private void NextRound()
    {
        roundInStage++;
        if (roundInStage > roundsPerStage)
        {
            roundInStage = 1;
            currentStage++;
        }

        UIManager.Instance.UpdateStageUI(currentStage, roundInStage, roundsPerStage);

        // Check for augment selection on new round
        CheckForAugmentSelection();
    }
}
