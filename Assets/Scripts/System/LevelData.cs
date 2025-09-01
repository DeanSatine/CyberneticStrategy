using UnityEngine;

[System.Serializable]
public class LevelData
{
    public int level;
    public int xpToLevel;
    public int shopSlots = 5;
}

public class PlayerLevel : MonoBehaviour
{
    public static PlayerLevel Instance;

    [Header("XP/Level")]
    public int currentLevel = 1;
    public int currentXP = 0;

    public int xpPerBuy = 4;
    public int xpCost = 4; // 4 gold for 4 XP

    public LevelData[] levelTable; // define in inspector

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void BuyXP()
    {
        if (EconomyManager.Instance.SpendGold(xpCost))
        {
            currentXP += xpPerBuy;
            CheckLevelUp();
        }
    }

    private void CheckLevelUp()
    {
        foreach (var data in levelTable)
        {
            if (currentLevel == data.level && currentXP >= data.xpToLevel)
            {
                currentLevel++;
                currentXP = 0;
                Debug.Log($"Player leveled up! Now Level {currentLevel}");
            }
        }
    }
}
