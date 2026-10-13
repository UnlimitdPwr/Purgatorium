using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentUIItemButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button button;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemLabel;

    private Action clickAction;

    private void Awake()
    {
        // This function ensures the button component is assigned and registers its click handler.
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }
    }

    public void Setup(
        Sprite icon,
        string label,
        Action onClick,
        bool interactable = true)
    {
        // This function updates the button's icon, label, click action, and interactable state.
        clickAction = onClick;

        if (itemIcon != null)
        {
            itemIcon.sprite = icon;
            itemIcon.enabled = icon != null;
        }

        if (itemLabel != null)
        {
            itemLabel.text = label;
        }

        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    private void HandleClick()
    {
        // This function checks whether the generated equipment button receives a click and runs its assigned action.
        Debug.Log("Equipment button clicked: " + gameObject.name);

        if (clickAction == null)
        {
            Debug.LogWarning("No click action assigned to: " + gameObject.name);
            return;
        }

        clickAction.Invoke();
    }

    private void OnDestroy()
    {
        // This function removes the button listener when the component is destroyed.
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }
}

