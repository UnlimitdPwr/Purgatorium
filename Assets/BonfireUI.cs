using UnityEngine;
using System.Collections;

public class BonfireUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject bonfireUI;
    [SerializeField] private GameObject playerHUD;

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
        // This function sets the initial UI state when the game starts.
        // The GameObjects stay active so the transition coroutine can run.

        bonfireUI.SetActive(true);
        playerHUD.SetActive(true);

        bonfireCanvasGroup.alpha = 0f;
        bonfireCanvasGroup.interactable = false;
        bonfireCanvasGroup.blocksRaycasts = false;

        playerHUDCanvasGroup.alpha = 1f;
        playerHUDCanvasGroup.interactable = true;
        playerHUDCanvasGroup.blocksRaycasts = true;
    }

    public void Open(GameObject player, Bonfire bonfire)
    {
        // This function starts the transition into the bonfire resting state.

        playerController = player.GetComponent<PlayerController1>();
        currentBonfire = bonfire;

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(EnterBonfireTransition());
    }

    private IEnumerator EnterBonfireTransition()
    {
        // This function fades the Player HUD out while the Bonfire UI fades in.

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

    public void Close()
    {
        // This function immediately hides the Bonfire UI.

        bonfireCanvasGroup.alpha = 0f;
        bonfireCanvasGroup.interactable = false;
        bonfireCanvasGroup.blocksRaycasts = false;
    }

    public void Leave()
    {
        // This function immediately exits the bonfire resting state.

        if (playerController == null)
            return;

        playerController.SetResting(false);

        // Immediately hide the Bonfire UI.
        bonfireCanvasGroup.alpha = 0f;
        bonfireCanvasGroup.interactable = false;
        bonfireCanvasGroup.blocksRaycasts = false;

        // Immediately show the Player HUD.
        playerHUDCanvasGroup.alpha = 1f;
        playerHUDCanvasGroup.interactable = true;
        playerHUDCanvasGroup.blocksRaycasts = true;

        transitionCoroutine = null;

        Debug.Log("Player left the bonfire.");
    }
}
