using UnityEngine;

public class BonfireUI : MonoBehaviour
{
    [SerializeField] private GameObject bonfireUI;

    private PlayerController1 playerController;
    private Bonfire currentBonfire;

    private void Awake()
    {
        bonfireUI.SetActive(false);
    }

    public void Open(GameObject player, Bonfire bonfire)
    {
        playerController =
            player.GetComponent<PlayerController1>();

        currentBonfire = bonfire;

        bonfireUI.SetActive(true);
    }

    public void Close()
    {
        bonfireUI.SetActive(false);
    }

    public void Leave()
    {
        if (playerController == null)
            return;

        playerController.SetResting(false);

        Close();

        Debug.Log("Player left the bonfire.");
    }
}
