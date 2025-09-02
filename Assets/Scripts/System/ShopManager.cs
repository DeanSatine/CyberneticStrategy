using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    [Header("Bench Settings")]
    public Transform[] benchSlots; // assign your 9 bench slot transforms in inspector

    private void Awake()
    {
        Instance = this;
    }

    public void FillShop(ShopSlotUI[] slots)
    {
        int stage = StageManager.Instance.currentStage;

        for (int i = 0; i < slots.Length; i++)
        {
            ShopSlotUI slot = slots[i];
            if (RollAllowsThisUnit(slot.cost, stage))
                slot.gameObject.SetActive(true);
            else
                slot.gameObject.SetActive(false);
        }
    }

    public void TryBuyUnit(ShopSlotUI slot)
    {
        if (!EconomyManager.Instance.SpendGold(slot.cost))
        {
            Debug.Log("Not enough gold!");
            return;
        }

        // Find first empty bench slot
        Transform freeSlot = null;
        foreach (Transform benchSlot in benchSlots)
        {
            if (benchSlot.childCount == 0)
            {
                freeSlot = benchSlot;
                break;
            }
        }

        if (freeSlot == null)
        {
            Debug.Log("Bench is full!");
            return;
        }

        // Spawn unit on free bench slot
        GameObject unit = Instantiate(slot.unitPrefab, freeSlot.position, Quaternion.identity);
        unit.transform.SetParent(freeSlot); // parent it so it "sticks" to slot
        Debug.Log($"Bought {slot.unitPrefab.name} for {slot.cost} gold, placed on bench!");
    }

    private bool RollAllowsThisUnit(int cost, int stage)
    {
        int roll = Random.Range(0, 100);

        if (stage == 1)
        {
            if (cost == 1) return roll < 70;
            if (cost == 2) return roll >= 70;
        }
        else if (stage == 2)
        {
            if (cost == 1) return roll < 30;
            if (cost == 2) return roll >= 30 && roll < 70;
            if (cost == 3) return roll >= 70 && roll < 90;
            if (cost == 4) return roll >= 90;
        }
        else if (stage >= 3)
        {
            if (cost == 1) return roll < 10;
            if (cost == 2) return roll >= 10 && roll < 30;
            if (cost == 3) return roll >= 30 && roll < 60;
            if (cost == 4) return roll >= 60 && roll < 85;
            if (cost == 5) return roll >= 85;
        }

        return false;
    }
}
