using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Small buildup bars under the player's stamina bar, one per status. A bar
// only shows while its buildup is above 0, and visible bars stack downward
// with no gaps. While a condition is active the bar pulses as it drains.
// The bars are built at runtime from the player's statuses, so a new status
// gets a bar without any UI work.
[RequireComponent(typeof(RectTransform))]
public class PlayerStatusBars : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStatusEffects playerStatus;

    [Header("Layout")]
    [SerializeField] private float barWidth = 150f;
    [SerializeField] private float barHeight = 8f;
    [SerializeField] private float spacing = 4f;
    [SerializeField] private int labelFontSize = 12;

    [Header("Look")]
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.6f);

    [Tooltip("Pulses per second while a condition is active.")]
    [SerializeField] private float conditionPulseSpeed = 3f;

    private class Bar
    {
        public StatusMeter meter;
        public RectTransform root;
        public RectTransform fill;
        public Image fillImage;
    }

    private readonly List<Bar> bars = new List<Bar>();

    // =========================
    // SETUP
    // =========================

    private void Start()
    {
        if (playerStatus == null)
        {
            Debug.LogWarning("PlayerStatusBars: no PlayerStatusEffects assigned.", this);
            enabled = false;
            return;
        }

        foreach (StatusMeter meter in playerStatus.Meters)
            bars.Add(CreateBar(meter));
    }

    Bar CreateBar(StatusMeter meter)
    {
        Bar bar = new Bar { meter = meter };

        bar.root = NewRect(meter.Definition.displayName, transform);
        bar.root.anchorMin = bar.root.anchorMax = new Vector2(0f, 1f);
        bar.root.pivot = new Vector2(0f, 1f);
        bar.root.sizeDelta = new Vector2(barWidth, barHeight);
        bar.root.gameObject.AddComponent<Image>().color = backgroundColor;

        bar.fill = NewRect("Fill", bar.root);
        bar.fill.anchorMin = Vector2.zero;
        bar.fill.anchorMax = new Vector2(0f, 1f);
        bar.fill.offsetMin = bar.fill.offsetMax = Vector2.zero;
        bar.fillImage = bar.fill.gameObject.AddComponent<Image>();
        bar.fillImage.color = meter.Definition.barColor;

        RectTransform labelRect = NewRect("Label", bar.root);
        labelRect.anchorMin = new Vector2(1f, 0.5f);
        labelRect.anchorMax = new Vector2(1f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = new Vector2(6f, 0f);
        labelRect.sizeDelta = new Vector2(120f, labelFontSize + 4f);

        Text label = labelRect.gameObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = labelFontSize;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = meter.Definition.barColor;
        label.text = meter.Definition.displayName;
        label.raycastTarget = false;

        bar.root.gameObject.SetActive(false);
        return bar;
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    // =========================
    // PER FRAME
    // =========================

    private void Update()
    {
        float y = 0f;

        foreach (Bar bar in bars)
        {
            bool visible = bar.meter.Buildup > 0f;

            if (bar.root.gameObject.activeSelf != visible)
                bar.root.gameObject.SetActive(visible);

            if (!visible)
                continue;

            bar.root.anchoredPosition = new Vector2(0f, y);
            y -= barHeight + spacing;

            bar.fill.anchorMax = new Vector2(Mathf.Clamp01(bar.meter.Fill), 1f);

            Color color = bar.meter.Definition.barColor;

            if (bar.meter.IsConditionActive)
                color.a *= 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * conditionPulseSpeed * Mathf.PI));

            bar.fillImage.color = color;
        }
    }
}
