using UnityEngine;

public class Bonfire : MonoBehaviour, IInteractable
{
    [Header("Bonfire")]
    [SerializeField] private Transform respawnPoint;

    [Header("Visuals")]
    [SerializeField] private GameObject fireOff;
    [SerializeField] private GameObject fireOn;

    [Header("UI")]
    [SerializeField] private BonfireUI bonfireUI;

    private bool isActivated = false;

    private void Start()
    {
        UpdateVisuals();
    }

    public void Interact(GameObject interactor)
    {
        if (!isActivated)
        {
            Activate();
            return;
        }

        Rest(interactor);
    }

    private void Activate()
    {
        isActivated = true;

        UpdateVisuals();

        Debug.Log("Bonfire activated!");


    }

    private void Rest(GameObject interactor)
    {
        PlayerHealth playerHealth =
            interactor.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.RestoreFullHealth();
        }

        // This bonfire becomes the current checkpoint
        CheckpointManager.Instance.SetCheckpoint(this);

        // Lock player movement
        PlayerController1 controller =
            interactor.GetComponent<PlayerController1>();

        if (controller != null)
        {
            controller.SetResting(true);
        }

        // Open the resting UI
        if (bonfireUI != null)
        {
            bonfireUI.Open(interactor, this);
        }

        Debug.Log("Player is resting.");
    }

    private void UpdateVisuals()
    {
        if (fireOff != null)
            fireOff.SetActive(!isActivated);

        if (fireOn != null)
            fireOn.SetActive(isActivated);
    }

    public Transform GetRespawnPoint()
    {
        return respawnPoint;
    }

}
