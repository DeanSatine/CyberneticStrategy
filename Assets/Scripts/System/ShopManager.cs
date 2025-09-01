using UnityEngine;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;
    public Transform benchSpawn;

    [Header("Unit Pool")]
    public List<ShopUnit> allUnits;

    [Header("Current Shop")]
    public List<ShopUnit> currentShop = new List<ShopUnit>();
    public int shopSize = 5;


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void GenerateShop()
    {
        currentShop.Clear();

        for (int i = 0; i < shopSize; i++)
        {
            ShopUnit chosen = GetRandomUnitByStage();
            currentShop.Add(chosen);
        }

        Debug.Log($"Shop generated for Stage {StageManager.Instance.currentStage}");
    }
    private ShopUnit GetRandomUnitByStage()
    {
        int stage = StageManager.Instance.currentStage;
        int roll = Random.Range(0, 100);

        int chosenCost = 1;

        if (stage == 1)
        {
            if (roll < 70) chosenCost = 1;
            else chosenCost = 2;
        }
        else if (stage == 2)
        {
            if (roll < 30) chosenCost = 1;
            else if (roll < 70) chosenCost = 2;
            else if (roll < 90) chosenCost = 3;
            else chosenCost = 4;
        }
        else if (stage >= 3)
        {
            if (roll < 10) chosenCost = 1;
            else if (roll < 30) chosenCost = 2;
            else if (roll < 60) chosenCost = 3;
            else if (roll < 85) chosenCost = 4;
            else chosenCost = 5;
        }

        // filter unit pool by cost
        List<ShopUnit> validUnits = allUnits.FindAll(u => u.cost == chosenCost);

        if (validUnits.Count == 0)
        {
            Debug.LogWarning($"No units of cost {chosenCost} found!");
            return allUnits[0];
        }

        return validUnits[Random.Range(0, validUnits.Count)];
    }


    public void BuyUnit(int index)
    {
        if (index < 0 || index >= currentShop.Count) return;

        ShopUnit chosen = currentShop[index];
        if (EconomyManager.Instance.SpendGold(chosen.cost))
        {
            Debug.Log($"Bought {chosen.unitName} for {chosen.cost} gold!");

            // Spawn on bench
            Vector3 spawnPos = benchSpawn.position + new Vector3(index * 2f, 0, 0); // offset so they don’t overlap
            Instantiate(chosen.prefab, spawnPos, Quaternion.identity);
        }
    }

    public void RerollShop()
    {
        if (EconomyManager.Instance.SpendGold(2)) // 2 gold cost for reroll
        {
            GenerateShop();
            ShopUIManager.Instance.RefreshShopUI();
        }
        else
        {
            Debug.Log("Not enough gold to reroll!");
        }
    }
}
