using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance;

    [Header("Gold Settings")]
    public int startingGold = 5;
    public int currentGold;

    [Header("UI")]
    [SerializeField] private TMP_Text goldText; // drag TMP text here
    [SerializeField] private Color gainColor = Color.green;
    [SerializeField] private Color spendColor = Color.red;
    private Color baseColor;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        currentGold = startingGold;
    }

    private void Start()
    {
        if (goldText != null)
            baseColor = goldText.color;

        UpdateGoldUI();
    }

    public bool SpendGold(int amount)
    {
        if (currentGold >= amount)
        {
            currentGold -= amount;
            UpdateGoldUI();
            TriggerGoldFlash(spendColor);
            return true;
        }
        return false;
    }

    public void AddGold(int amount)
    {
        currentGold += amount;
        UpdateGoldUI();
        TriggerGoldFlash(gainColor);
    }

    private void UpdateGoldUI()
    {
        if (goldText != null)
            goldText.text = $"{currentGold}";
    }

    private void TriggerGoldFlash(Color flashColor)
    {
        if (goldText == null) return;
        StopAllCoroutines();
        StartCoroutine(FlashRoutine(flashColor));
    }

    private IEnumerator FlashRoutine(Color flashColor)
    {
        goldText.color = flashColor;
        yield return new WaitForSeconds(0.2f); // quick flash
        goldText.color = baseColor;
    }
}
