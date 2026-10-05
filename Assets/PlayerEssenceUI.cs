using UnityEngine;
using TMPro;

public class PlayerEssenceUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerEssence playerEssence;
    [SerializeField] private TMP_Text essenceText;

    private void Start()
    {
        // This function connects the gameplay HUD to the player's Essence system and displays the starting Essence.
        if (playerEssence != null)
        {
            playerEssence.OnEssenceChanged += UpdateEssence;
            UpdateEssence(playerEssence.CurrentEssence);
        }
    }

    private void OnDestroy()
    {
        // This function removes the Essence event subscription when the HUD is destroyed.
        if (playerEssence != null)
        {
            playerEssence.OnEssenceChanged -= UpdateEssence;
        }
    }

    private void UpdateEssence(int amount)
    {
        // This function updates the gameplay HUD with the player's actual current Essence.
        essenceText.text = amount.ToString("N0");
    }
}
