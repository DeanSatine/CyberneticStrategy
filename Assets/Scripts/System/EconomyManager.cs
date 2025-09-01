using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance;

    [Header("Gold Settings")]
    public int startingGold = 5;
    public int currentGold;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        currentGold = startingGold;
    }

    public bool SpendGold(int amount)
    {
        if (currentGold >= amount)
        {
            currentGold -= amount;
            return true;
        }
        return false;
    }

    public void AddGold(int amount)
    {
        currentGold += amount;
    }
}
