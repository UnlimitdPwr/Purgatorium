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

    [Header("Buildup Rate")]
    [Tooltip("How fast this character builds up EVERY status. 1 = normal, 0.5 = half as fast, " +
             "2 = twice as fast. Stacks with the per-status resistances below.")]
    [SerializeField, Min(0f)] private float buildupRate = 1f;

    [Header("Resistances")]
    [Tooltip("Per-status buildup multipliers. Statuses not listed take normal buildup (x1).")]
    [SerializeField] private List<StatusResistance> resistances = new List<StatusResistance>();

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
            if (meter.IsConditionActive)
                ConditionUpdate(meter, Time.deltaTime);

            if (meter.Tick(Time.deltaTime))
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

    // =========================
    // RESISTANCES
    // =========================

    float ScaleBuildup(StatusType type, float amount)
    {
        return amount * buildupRate * GetBuildupMultiplier(type);
    }

    // The per-status multiplier from the resistance list (1 if not listed).
    public float GetBuildupMultiplier(StatusType type)
    {
        foreach (StatusResistance resistance in resistances)
        {
            if (resistance.status == type)
                return resistance.buildupMultiplier;
        }

        return 1f;
    }

    public float BuildupRate => buildupRate;

    protected virtual void ConditionStarted(StatusMeter meter) { }
    protected abstract void ConditionUpdate(StatusMeter meter, float deltaTime);
    protected virtual void ConditionEnded(StatusMeter meter) { }

    // =========================
    // COMBINING ACTIVE CONDITIONS
    // =========================

    // Weakest-ignored rule: of every active condition with this multiplier
    // ticked, only the one furthest from 1 in the harmful direction counts.
    // lowerIsStronger: true for slows/reductions, false for "longer"/"more".
    // 1 when no active condition has it.
    protected float Strongest(Func<StatusEffectDefinition, MultiplierEffect> pick, bool lowerIsStronger)
    {
        float result = 1f;
        bool any = false;

        foreach (StatusMeter meter in meterList)
        {
            if (!meter.IsConditionActive)
                continue;

            MultiplierEffect effect = pick(meter.Definition);

            if (effect == null || !effect.enabled)
                continue;

            if (!any)
                result = effect.multiplier;
            else
                result = lowerIsStronger ? Mathf.Min(result, effect.multiplier) : Mathf.Max(result, effect.multiplier);

            any = true;
        }

        return result;
    }

    // True if any active condition has this effect ticked.
    protected bool AnyActive(Func<StatusEffectDefinition, ConditionEffect> pick)
    {
        foreach (StatusMeter meter in meterList)
        {
            if (!meter.IsConditionActive)
                continue;

            ConditionEffect effect = pick(meter.Definition);

            if (effect != null && effect.enabled)
                return true;
        }

        return false;
    }

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
