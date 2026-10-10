using System;
using System.Collections.Generic;
using UnityEngine;

// Shared by PlayerStatusEffects and EnemyStatusEffects: holds one StatusMeter
// per status and ticks them. Subclasses only decide what a condition DOES to
// their character (damage, death, slow...) and how incoming buildup is scaled.
public abstract class StatusEffectsBase : MonoBehaviour, IStatusReceiver
{
    [Tooltip("One settings asset per status. Filled in automatically — run " +
             "Tools > Status Effects > Set Up Status Effects In Open Scene after adding a status.")]
    [SerializeField] private StatusEffectDefinition[] statusEffects = new StatusEffectDefinition[0];

    [Header("Debug")]
    [Tooltip("Used by the 'Debug: Add Buildup' right-click menu on this component.")]
    [SerializeField] private StatusType debugStatus = StatusType.Poison;
    [SerializeField] private float debugAmount = 10f;

    private readonly Dictionary<StatusType, StatusMeter> meters = new Dictionary<StatusType, StatusMeter>();

    // In StatusType order — for UI.
    private readonly List<StatusMeter> meterList = new List<StatusMeter>();

    public IReadOnlyList<StatusMeter> Meters => meterList;

    public event Action<StatusType> OnConditionStarted;
    public event Action<StatusType> OnConditionEnded;

    // =========================
    // SETUP
    // =========================

    protected virtual void Awake()
    {
        foreach (StatusEffectDefinition definition in statusEffects)
        {
            if (definition == null || meters.ContainsKey(definition.type))
                continue;

            StatusMeter meter = new StatusMeter(definition);
            meters.Add(definition.type, meter);
            meterList.Add(meter);
        }

        meterList.Sort((a, b) => a.Definition.type.CompareTo(b.Definition.type));
    }

    // =========================
    // EXPOSURE
    // =========================

    public void AddBuildup(StatusType type, float amount)
    {
        if (!CanBeAffected())
            return;

        if (!meters.TryGetValue(type, out StatusMeter meter))
        {
            Debug.LogWarning(name + ": no settings for status '" + type + "' — run " +
                             "Tools > Status Effects > Set Up Status Effects In Open Scene.", this);
            return;
        }

        amount = ScaleBuildup(type, amount);

        if (meter.AddBuildup(amount))
        {
            ConditionStarted(meter);
            OnConditionStarted?.Invoke(type);
        }
    }

    public float GetBuildup(StatusType type)
    {
        return meters.TryGetValue(type, out StatusMeter meter) ? meter.Buildup : 0f;
    }

    public bool HasCondition(StatusType type)
    {
        return meters.TryGetValue(type, out StatusMeter meter) && meter.IsConditionActive;
    }

    // =========================
    // PER FRAME
    // =========================

    protected virtual void Update()
    {
        if (!CanBeAffected())
            return;

        foreach (StatusMeter meter in meterList)
        {
            int ticks = meter.Tick(Time.deltaTime, out bool ended);

            for (int i = 0; i < ticks; i++)
                ConditionTick(meter);

            if (ended)
                EndCondition(meter);
        }
    }

    // =========================
    // RESET
    // =========================

    // Clears every buildup and active condition (bonfire rest, respawn).
    public void ClearAll()
    {
        foreach (StatusMeter meter in meterList)
        {
            if (meter.Clear())
                EndCondition(meter);
        }
    }

    void EndCondition(StatusMeter meter)
    {
        ConditionEnded(meter);
        OnConditionEnded?.Invoke(meter.Definition.type);
    }

    // =========================
    // SUBCLASS HOOKS
    // =========================

    // False while the character is dead — meters freeze and ignore exposure.
    protected abstract bool CanBeAffected();

    // Lets enemies apply resistances / weaknesses. Default: unchanged.
    protected virtual float ScaleBuildup(StatusType type, float amount) => amount;

    protected virtual void ConditionStarted(StatusMeter meter) { }
    protected abstract void ConditionTick(StatusMeter meter);
    protected virtual void ConditionEnded(StatusMeter meter) { }

    // =========================
    // DEBUG
    // =========================

    [ContextMenu("Debug: Add Buildup")]
    void DebugAddBuildup()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("Status effects only run in Play Mode.", this);
            return;
        }

        AddBuildup(debugStatus, debugAmount);
        Debug.Log(name + ": " + debugStatus + " buildup " + GetBuildup(debugStatus) +
                  (HasCondition(debugStatus) ? " (CONDITION ACTIVE)" : ""), this);
    }

    [ContextMenu("Debug: Clear All")]
    void DebugClearAll()
    {
        ClearAll();
    }

#if UNITY_EDITOR
    // Adding the component in the Inspector picks up every status asset.
    void Reset()
    {
        statusEffects = FindAllDefinitions();
    }

    public static StatusEffectDefinition[] FindAllDefinitions()
    {
        List<StatusEffectDefinition> found = new List<StatusEffectDefinition>();

        foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:" + nameof(StatusEffectDefinition)))
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            StatusEffectDefinition definition =
                UnityEditor.AssetDatabase.LoadAssetAtPath<StatusEffectDefinition>(path);

            if (definition != null)
                found.Add(definition);
        }

        found.Sort((a, b) => a.type.CompareTo(b.type));
        return found.ToArray();
    }
#endif
}
