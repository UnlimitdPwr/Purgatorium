using UnityEngine;

public class Bonfire : MonoBehaviour, IInteractable
{
    [Header("Bonfire")]
    [SerializeField] private Transform respawnPoint;


    // This function is called by the InteractionScript when the player interacts with the active bonfire.
    public void Interact(GameObject interactor)
    {
        // All bonfires are already active when they are spawned, // so interacting with one immediately allows the player to rest.
        Rest(interactor);
    }

    // This function restores the player, sets this bonfire as the checkpoint,
    // locks the player and opens the bonfire UI.
    private void Rest(GameObject interactor)
    {
        PlayerHealth playerHealth =
            interactor.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.RestoreFullHealth();
        }

        // This bonfire becomes the current checkpoint.
        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.SetCheckpoint(this);
        }

        // Lock player movement and actions.
        PlayerController1 controller =
            interactor.GetComponent<PlayerController1>();

        if (controller != null)
        {
            controller.SetResting(true);
        }

        // Find the BonfireUI that exists in the scene.
        BonfireUI bonfireUI =
            FindFirstObjectByType<BonfireUI>(
                FindObjectsInactive.Include
            );

        if (bonfireUI != null)
        {
            bonfireUI.Open(interactor, this);
        }
        else
        {
            Debug.LogWarning(
                "No BonfireUI found in the scene."
            );
        }

        Debug.Log("Player is resting.");
    }

    // This function gives the checkpoint system the location where the player should respawn after dying.
    public Transform GetRespawnPoint()
    {
        return respawnPoint;
    }

}
