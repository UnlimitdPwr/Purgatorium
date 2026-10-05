using UnityEngine;
using System.Collections;

public class BonfireUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject bonfireUI;
    [SerializeField] private GameObject playerHUD;

    [Header("Bonfire Menu")]
    [SerializeField] private GameObject mainPanel;

    [Header("Stats UI")]
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private BonfireStatsUI bonfireStatsUI;

    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup bonfireCanvasGroup;
    [SerializeField] private CanvasGroup playerHUDCanvasGroup;

    [Header("Enter Transition")]
    [SerializeField] private float transitionDuration = 0.6f;

    private PlayerController1 playerController;
    private Bonfire currentBonfire;

    private Coroutine transitionCoroutine;


    private void Awake()
    {
        // This function sets the initial state of the Bonfire UI when the game starts.

        bonfireUI.SetActive(true);
        playerHUD.SetActive(true);

        mainPanel.SetActive(true);
        statsPanel.SetActive(false);

        bonfireCanvasGroup.alpha = 0f;
        bonfireCanvasGroup.interactable = false;
        bonfireCanvasGroup.blocksRaycasts = false;

        playerHUDCanvasGroup.alpha = 1f;
        playerHUDCanvasGroup.interactable = true;
        playerHUDCanvasGroup.blocksRaycasts = true;
    }


    public void Open(GameObject player, Bonfire bonfire)
    {
        // This function starts the transition into the Bonfire resting state and opens the main Bonfire menu.

        playerController = player.GetComponent<PlayerController1>();
        currentBonfire = bonfire;

        mainPanel.SetActive(true);
        statsPanel.SetActive(false);

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(
            EnterBonfireTransition()
        );
    }


    private IEnumerator EnterBonfireTransition()
    {
        // This function fades the Player HUD out while fading the Bonfire UI in.

        float startHUDAlpha = playerHUDCanvasGroup.alpha;
        float startBonfireAlpha = bonfireCanvasGroup.alpha;

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;

            float progress = elapsed / transitionDuration;

            playerHUDCanvasGroup.alpha = Mathf.Lerp(
                startHUDAlpha,
                0f,
                progress
            );

            bonfireCanvasGroup.alpha = Mathf.Lerp(
                startBonfireAlpha,
                1f,
                progress
            );

            yield return null;
        }

        playerHUDCanvasGroup.alpha = 0f;
        bonfireCanvasGroup.alpha = 1f;

        playerHUDCanvasGroup.interactable = false;
        playerHUDCanvasGroup.blocksRaycasts = false;

        bonfireCanvasGroup.interactable = true;
        bonfireCanvasGroup.blocksRaycasts = true;

        transitionCoroutine = null;
    }


    public void OpenStats()
    {
        // This function closes the normal Bonfire menu and opens the Stats screen.

        if (bonfireStatsUI != null)
        {
            bonfireStatsUI.BeginStatSelection();
        }

        mainPanel.SetActive(false);
        statsPanel.SetActive(true);
    }


    public void CloseStats()
    {
        // This function closes the Stats screen, discards temporary changes and returns to the main Bonfire menu.

        if (bonfireStatsUI != null)
        {
            bonfireStatsUI.Cancel();
        }

        statsPanel.SetActive(false);
        mainPanel.SetActive(true);
    }


    public void Close()
    {
        // This function immediately hides the Bonfire UI.

        mainPanel.SetActive(true);
        statsPanel.SetActive(false);

        bonfireCanvasGroup.alpha = 0f;
        bonfireCanvasGroup.interactable = false;
        bonfireCanvasGroup.blocksRaycasts = false;
    }


    public void Leave()
    {
        // This function exits the Bonfire resting state and restores the player's normal HUD.

        if (playerController == null)
            return;

        playerController.SetResting(false);

        mainPanel.SetActive(true);
        statsPanel.SetActive(false);

        bonfireCanvasGroup.alpha = 0f;
        bonfireCanvasGroup.interactable = false;
        bonfireCanvasGroup.blocksRaycasts = false;

        playerHUDCanvasGroup.alpha = 1f;
        playerHUDCanvasGroup.interactable = true;
        playerHUDCanvasGroup.blocksRaycasts = true;

        transitionCoroutine = null;

        Debug.Log("Player left the bonfire.");
    }
}