using UnityEngine;

public class HyperdriveTrait : MonoBehaviour
{
    [HideInInspector] public float attackSpeedPerStack;
    [HideInInspector] public float bonusAttackSpeedLowEnemies;
    [HideInInspector] public int maxStacks = 10;
    [HideInInspector] public bool infiniteStacks = false;

    private UnitAI unitAI;
    private int currentStacks = 0;
    private float baseAttackSpeed;
    private bool isInitialized = false;

    private const int LOW_ENEMY_THRESHOLD = 3;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
        if (unitAI != null)
        {
            baseAttackSpeed = unitAI.attackSpeed;
            isInitialized = true;
            Debug.Log($"⚡ [HYPERDRIVE AWAKE] {unitAI.unitName} - Base AS: {baseAttackSpeed:F2}, PerStack: {attackSpeedPerStack:F2}, LowEnemyBonus: {bonusAttackSpeedLowEnemies:F2}, MaxStacks: {maxStacks}, Infinite: {infiniteStacks}");
        }
        else
        {
            Debug.LogError($"❌ [HYPERDRIVE AWAKE] UnitAI component not found!");
        }
    }

    private void OnEnable()
    {
        if (unitAI != null)
        {
            unitAI.OnAttackEvent += OnBasicAttack;
            Debug.Log($"✅ [HYPERDRIVE ENABLE] {unitAI.unitName} - Subscribed to OnAttackEvent");
        }
        else
        {
            Debug.LogError($"❌ [HYPERDRIVE ENABLE] UnitAI is null, cannot subscribe to OnAttackEvent");
        }
    }

    private void OnDisable()
    {
        if (unitAI != null)
        {
            unitAI.OnAttackEvent -= OnBasicAttack;
            Debug.Log($"🔴 [HYPERDRIVE DISABLE] {unitAI.unitName} - Unsubscribed from OnAttackEvent");
        }
        RemoveAttackSpeed();
    }

    private void Update()
    {
        if (unitAI != null && unitAI.currentState == UnitAI.UnitState.Bench)
        {
            Debug.Log($"🏟️ [HYPERDRIVE UPDATE] {unitAI.unitName} moved to bench - removing Hyperdrive bonus");
            RemoveAttackSpeed();
            Destroy(this);
            return;
        }

        if (isInitialized && unitAI != null)
        {
            UpdateAttackSpeed();
        }
    }

    private void OnBasicAttack(UnitAI target)
    {
        Debug.Log($"🔥 [HYPERDRIVE ATTACK] {unitAI.unitName} attacked {target.unitName}! Current stacks: {currentStacks}, State: {unitAI.currentState}");

        if (!isInitialized || unitAI.currentState == UnitAI.UnitState.Bench)
        {
            Debug.LogWarning($"⚠️ [HYPERDRIVE ATTACK] Attack ignored - Initialized: {isInitialized}, State: {unitAI.currentState}");
            return;
        }

        if (infiniteStacks || currentStacks < maxStacks)
        {
            currentStacks++;
            Debug.Log($"⚡ [HYPERDRIVE STACK] {unitAI.unitName} gained stack! Now at {currentStacks}/{(infiniteStacks ? "∞" : maxStacks.ToString())} stacks");
            UpdateAttackSpeed();
        }
        else
        {
            Debug.Log($"⚠️ [HYPERDRIVE MAX] {unitAI.unitName} already at max stacks ({maxStacks})");
        }
    }

    private void UpdateAttackSpeed()
    {
        if (!isInitialized) return;

        int enemyCount = CountEnemies();

        float stackBonus = currentStacks * attackSpeedPerStack;
        float lowEnemyBonus = (enemyCount <= LOW_ENEMY_THRESHOLD) ? (currentStacks * bonusAttackSpeedLowEnemies) : 0f;
        float totalBonus = stackBonus + lowEnemyBonus;

        float newAttackSpeed = baseAttackSpeed + totalBonus;

        if (unitAI.attackSpeed != newAttackSpeed)
        {
            unitAI.attackSpeed = newAttackSpeed;

            string lowEnemyInfo = (enemyCount <= LOW_ENEMY_THRESHOLD)
                ? $" (+{lowEnemyBonus:F2} bonus from {enemyCount} enemies)"
                : "";

            Debug.Log($"⚡ [HYPERDRIVE UPDATE] {unitAI.unitName}: {currentStacks} stacks = +{totalBonus:F2} AS{lowEnemyInfo} (Base: {baseAttackSpeed:F2} → New: {unitAI.attackSpeed:F2})");
        }
    }

    private int CountEnemies()
    {
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        int count = 0;
        foreach (var unit in allUnits)
        {
            if (unit != null && unit.isAlive && unit.team == Team.Enemy && unit.currentState != UnitAI.UnitState.Bench)
            {
                count++;
            }
        }
        return count;
    }

    public void ResetStacks()
    {
        if (isInitialized && unitAI != null)
        {
            currentStacks = 0;
            unitAI.attackSpeed = baseAttackSpeed;
            Debug.Log($"🔄 [HYPERDRIVE RESET STACKS] {unitAI.unitName} stacks reset to 0, AS reset to {baseAttackSpeed:F2}");
        }
    }

    private void RemoveAttackSpeed()
    {
        if (isInitialized && unitAI != null)
        {
            unitAI.attackSpeed = baseAttackSpeed;
            Debug.Log($"💔 [HYPERDRIVE REMOVE] {unitAI.unitName} deactivated! Reset to base AS: {baseAttackSpeed:F2}");
        }

        currentStacks = 0;
        isInitialized = false;
    }

    public static void ResetAllHyperdriveStacks()
    {
        HyperdriveTrait[] allHyperdrives = FindObjectsOfType<HyperdriveTrait>();
        Debug.Log($"🔄 [HYPERDRIVE STACK RESET] Found {allHyperdrives.Length} Hyperdrive components to reset stacks");

        foreach (var hyperdrive in allHyperdrives)
        {
            if (hyperdrive != null && hyperdrive.unitAI != null)
            {
                Debug.Log($"🔄 [HYPERDRIVE STACK RESET] Resetting stacks for {hyperdrive.unitAI.unitName}");
                hyperdrive.ResetStacks();
            }
        }

        Debug.Log("✅ All Hyperdrive stacks reset for new round");
    }

    public static void ResetAllHyperdrives()
    {
        HyperdriveTrait[] allHyperdrives = FindObjectsOfType<HyperdriveTrait>();
        Debug.Log($"🔍 [HYPERDRIVE DEACTIVATE] Found {allHyperdrives.Length} Hyperdrive components to remove");

        foreach (var hyperdrive in allHyperdrives)
        {
            if (hyperdrive != null && hyperdrive.unitAI != null)
            {
                Debug.Log($"🔍 [HYPERDRIVE DEACTIVATE] Removing Hyperdrive from {hyperdrive.unitAI.unitName}");
                hyperdrive.RemoveAttackSpeed();
                Destroy(hyperdrive);
            }
        }

        Debug.Log("❌ All Hyperdrive traits deactivated");
    }
}
