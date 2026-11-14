using UnityEngine;
using UnityEngine.UI;

// A thin Soul Durability bar under the HUD shield icon.
//   - Hidden when the active shield isn't a ShieldData (or nothing is equipped).
//   - Fill colour goes from Healthy to Low under the low threshold.
//   - When the shield is broken the bar shows empty and the shield icon greys out.
//
// EquipmentHUD adds this automatically if the scene has none, and builds the
// bar under the shield icon, so no scene setup is needed. To place it by hand,
// add it to a HUD object and fill in the references; it then uses those instead.
public class ShieldDurabilityBar : MonoBehaviour
{
    [Header("References (auto-filled when left empty)")]
    [SerializeField] private EquipmentManager equipment;
    [SerializeField] private Image shieldIcon;
    [Tooltip("Bar root; hidden when there's nothing to show.")]
    [SerializeField] private RectTransform barRoot;
    [Tooltip("Fill image; its width is scaled to the durability left.")]
    [SerializeField] private Image fill;

    [Header("Look")]
    [SerializeField] private Vector2 barSize = new Vector2(50f, 6f);
    [Tooltip("Gap between the bottom of the shield icon and the bar.")]
    [SerializeField] private float gapBelowIcon = 4f;
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] private Color healthyColor = new Color(0.85f, 0.85f, 0.95f, 1f);
    [SerializeField] private Color lowColor = new Color(0.9f, 0.3f, 0.25f, 1f);
    [Tooltip("At or below this share of durability the fill uses Low Color.")]
    [SerializeField, Range(0f, 1f)] private float lowThreshold = 0.25f;
    [Tooltip("Tint for the shield icon while the shield is broken.")]
    [SerializeField] private Color brokenIconTint = new Color(0.35f, 0.35f, 0.35f, 1f);

    private PlayerShield playerShield;
    private PlayerStats playerStats;
    private RectTransform fillRect;

    // Called by EquipmentHUD when it adds this component at runtime.
    public void Setup(EquipmentManager equipmentManager, Image icon)
    {
        equipment = equipmentManager;
        shieldIcon = icon;
    }

    private void Start()
    {
        if (equipment == null)
            equipment = FindAnyObjectByType<EquipmentManager>();

        if (equipment == null)
        {
            Debug.LogWarning("ShieldDurabilityBar: no EquipmentManager found; bar disabled.", this);
            enabled = false;
            return;
        }

        playerShield = PlayerShield.GetOrAdd(equipment.gameObject);
        playerStats = equipment.GetComponent<PlayerStats>();

        if (fill == null)
            BuildBar();

        fillRect = fill.rectTransform;

        playerShield.OnDurabilityChanged += HandleDurabilityChanged;
        equipment.OnEquipmentChanged += Refresh;
        equipment.OnActiveShieldChanged += Refresh;

        if (playerStats != null)
            playerStats.OnStatsChanged += Refresh;

        Refresh();
    }

    private void OnDestroy()
    {
        if (playerShield != null)
            playerShield.OnDurabilityChanged -= HandleDurabilityChanged;

        if (equipment != null)
        {
            equipment.OnEquipmentChanged -= Refresh;
            equipment.OnActiveShieldChanged -= Refresh;
        }

        if (playerStats != null)
            playerStats.OnStatsChanged -= Refresh;
    }

    private void HandleDurabilityChanged(ShieldData shield) => Refresh();

    public void Refresh()
    {
        if (fillRect == null)
            return;

        ShieldData shield = playerShield.Equipped;
        bool show = shield != null;

        if (barRoot != null)
            barRoot.gameObject.SetActive(show);

        bool broken = show && playerShield.IsBroken(shield);

        if (shieldIcon != null)
            shieldIcon.color = broken ? brokenIconTint : Color.white;

        if (!show)
            return;

        float fraction = playerShield.GetDurabilityFraction(shield);

        // Scale the fill from the left edge.
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(fraction, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        fill.color = fraction <= lowThreshold ? lowColor : healthyColor;
    }

    // Creates background + fill as a sibling just below the shield icon.
    private void BuildBar()
    {
        Transform parent = shieldIcon != null ? shieldIcon.transform.parent : transform;

        var root = new GameObject("ShieldDurabilityBar", typeof(RectTransform), typeof(Image));
        root.layer = parent.gameObject.layer;
        barRoot = root.GetComponent<RectTransform>();
        barRoot.SetParent(parent, false);
        barRoot.sizeDelta = barSize;

        if (shieldIcon != null)
        {
            RectTransform icon = shieldIcon.rectTransform;
            barRoot.anchorMin = icon.anchorMin;
            barRoot.anchorMax = icon.anchorMax;
            barRoot.pivot = new Vector2(0.5f, 1f);

            // Bottom edge of the icon, then the gap.
            float iconBottom = icon.anchoredPosition.y - icon.rect.height * icon.pivot.y;
            barRoot.anchoredPosition = new Vector2(icon.anchoredPosition.x, iconBottom - gapBelowIcon);
        }

        Image background = root.GetComponent<Image>();
        background.color = backgroundColor;
        background.raycastTarget = false;

        var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.layer = root.layer;
        fillObject.transform.SetParent(barRoot, false);

        fill = fillObject.GetComponent<Image>();
        fill.raycastTarget = false;
    }
}
