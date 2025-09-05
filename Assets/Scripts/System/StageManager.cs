using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance;
    public int currentStage = 1;
    public int roundInStage = 1;
    public int roundsPerStage = 3;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        EnemyWaveManager.Instance.SpawnEnemyWave(currentStage);
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

        // ✅ Spawn a new wave every round
        EnemyWaveManager.Instance.SpawnEnemyWave(currentStage);
    }
}
