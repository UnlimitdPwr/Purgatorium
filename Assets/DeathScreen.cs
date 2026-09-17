using UnityEngine;

public class DeathScreen : MonoBehaviour
{
    [SerializeField] private GameObject deathScreen;

    private void Awake()
    {
        deathScreen.SetActive(false);
    }

    public void Show()
    {
        Debug.Log("SHOWING DEATH SCREEN");

        deathScreen.SetActive(true);
    }

    public void Restart()
    {
        Debug.Log("RESTARTING FROM CHECKPOINT");

        PlayerDeath playerDeath = FindFirstObjectByType<PlayerDeath>();

        if (playerDeath != null)
        {
            playerDeath.Respawn();
        }
        else
        {
            Debug.LogWarning("PlayerDeath could not be found.");
            return;
        }

        deathScreen.SetActive(false);
    }
}