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

    [Header("Equipment UI")]
    [SerializeField] private GameObject equipmentPanel;

    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup bonfireCanvasGroup;
    [SerializeField] private CanvasGroup playerHUDCanvasGroup;

    [Header("Enter Transition")]
    [SerializeField] private float transitionDuration = 0.6f;

    private PlayerController1 playerController;
    private Bonfire currentBonfire;
    public bool IsEquipmentScreenOpen { get; private set; }

    private Coroutine transitionCoroutine;

    // This property tells other UI scripts whether the player is using the bonfire menu.
    public bool IsOpen { get; private set; }


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

        // This function ensures the equipment screen starts hidden.
        IsEquipmentScreenOpen = false;

        if (equipmentPanel != null)
        {
            equipmentPanel.SetActive(false);
        }
    }


    public void Open(GameObject player, Bonfire bonfire)
    {
        // Mark the bonfire menu as open so gameplay-only UI cannot be opened.
        IsOpen = true;

        // This function starts the transition into the Bonfire resting state and opens the main Bonfire menu.

        playerController = player.GetComponent<PlayerController1>();
        currentBonfire = bonfire;

        // This function ensures every bonfire visit starts at the main menu.
        IsEquipmentScreenOpen = false;

        mainPanel.SetActive(true);
        statsPanel.SetActive(false);

        if (equipmentPanel != null)
        {
            equipmentPanel.SetActive(false);
        }

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
        // This function closes all Bonfire UI screens and marks the equipment screen as closed.
        IsOpen = false;
        IsEquipmentScreenOpen = false;

        mainPanel.SetActive(true);
        statsPanel.SetActive(false);

        if (equipmentPanel != null)
        {
            equipmentPanel.SetActive(false);
        }

        bonfireCanvasGroup.alpha = 0f;
        bonfireCanvasGroup.interactable = false;
        bonfireCanvasGroup.blocksRaycasts = false;
    }




    public void Leave()
    {
        // This function exits the Bonfire, closes the equipment screen, and restores gameplay UI.
        IsOpen = false;
        IsEquipmentScreenOpen = false;

        if (playerController == null)
            return;

        playerController.SetResting(false);

        mainPanel.SetActive(true);
        statsPanel.SetActive(false);

        if (equipmentPanel != null)
        {
            equipmentPanel.SetActive(false);
        }

        bonfireCanvasGroup.alpha = 0f;
        bonfireCanvasGroup.interactable = false;
        bonfireCanvasGroup.blocksRaycasts = false;

        playerHUDCanvasGroup.alpha = 1f;
        playerHUDCanvasGroup.interactable = true;
        playerHUDCanvasGroup.blocksRaycasts = true;

        transitionCoroutine = null;

        Debug.Log("Player left the bonfire.");
    }




    public void OpenEquipment()
    {
        // This function opens the equipment screen from the main Bonfire menu.
        if (!IsOpen || equipmentPanel == null)
            return;

        IsEquipmentScreenOpen = true;

        mainPanel.SetActive(false);
        statsPanel.SetActive(false);
        equipmentPanel.SetActive(true);
    }

    public void CloseEquipment()
    {
        // This function closes the equipment screen and returns to the main Bonfire menu.
        if (equipmentPanel != null)
        {
            equipmentPanel.SetActive(false);
        }

        IsEquipmentScreenOpen = false;

        mainPanel.SetActive(true);
        statsPanel.SetActive(false);
    }


}