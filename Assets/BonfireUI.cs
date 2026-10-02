using UnityEngine;

public class BonfireUI : MonoBehaviour
{
    [SerializeField] private GameObject bonfireUI;

    [SerializeField] private GameObject playerHUD;

    private PlayerController1 playerController;
    private Bonfire currentBonfire;

    private void Awake()
    {
        bonfireUI.SetActive(false);
        playerHUD.SetActive(true);
    }

    public void Open(GameObject player, Bonfire bonfire)
    {
        // This function opens the Bonfire UI when the player starts resting.
        playerController =
            player.GetComponent<PlayerController1>();

        currentBonfire = bonfire;

        // Hide the normal player HUD while resting.
        playerHUD.SetActive(false);

        // Show the Bonfire UI.
        bonfireUI.SetActive(true);
    }

    public void Close()
    {
        // This function closes the Bonfire UI.
        bonfireUI.SetActive(false);
    }

    public void Leave()
    {
        // This function exits the bonfire resting state.
        if (playerController == null)
            return;

        playerController.SetResting(false);

        // Close the Bonfire UI.
        Close();

        // Show the normal player HUD again.
        playerHUD.SetActive(true);

        Debug.Log("Player left the bonfire.");
    }
}
