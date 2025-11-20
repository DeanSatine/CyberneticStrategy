using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text playerNameText;
    public TMP_Text healthText;
    public Image healthBarFill;
    public GameObject aliveIndicator;
    public GameObject deadIndicator;
    public Image backgroundImage;

    private int actorNumber;
    private int maxHealth = 100;

    public void Initialize(PvPPlayerState playerState)
    {
        actorNumber = playerState.actorNumber;

        if (playerNameText != null)
            playerNameText.text = playerState.playerName;

        UpdateHealth(playerState.health, playerState.isAlive);

        if (actorNumber == Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber && backgroundImage != null)
        {
            backgroundImage.color = new Color(0.3f, 0.6f, 1f, 0.5f);
        }
    }

    public void UpdateHealth(int health, bool isAlive)
    {
        if (healthText != null)
            healthText.text = $"{health}/{maxHealth}";

        if (healthBarFill != null)
        {
            float fillAmount = (float)health / maxHealth;
            healthBarFill.fillAmount = fillAmount;

            healthBarFill.color = fillAmount > 0.5f ? Color.green :
                                   fillAmount > 0.25f ? Color.yellow :
                                   Color.red;
        }

        if (aliveIndicator != null)
            aliveIndicator.SetActive(isAlive);

        if (deadIndicator != null)
            deadIndicator.SetActive(!isAlive);
    }
}
