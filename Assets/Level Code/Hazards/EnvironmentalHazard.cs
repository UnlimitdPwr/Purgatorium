using System.Collections.Generic;
using UnityEngine;

// A hazard area (acid pool, lava, swamp...). What it does and how it looks
// come from its HazardType — swap the type to turn acid into lava. Anything
// with a HazardReceiver that enters is ticked (damage + status buildup) and
// slowed for as long as it stays inside.
//
// Size the area with the Size field, not the Transform's scale, so tiled
// textures repeat instead of stretching. The collider follows Size.
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class EnvironmentalHazard : MonoBehaviour
{
    // =========================
    // CONFIG
    // =========================

    [SerializeField] private HazardType hazardType;

    [Tooltip("Width and height of the hazard area, in units.")]
    [SerializeField] private Vector2 size = new Vector2(4f, 1f);

    public HazardType Type => hazardType;

    // =========================
    // STATE
    // =========================

    // A character can have several colliders — they're only out of the
    // hazard once all of them have left.
    private class Occupant
    {
        public readonly HashSet<Collider2D> colliders = new HashSet<Collider2D>();
        public float tickTimer;
    }

    private readonly Dictionary<HazardReceiver, Occupant> occupants = new Dictionary<HazardReceiver, Occupant>();
    private readonly List<HazardReceiver> scratch = new List<HazardReceiver>();

    // =========================
    // SETUP
    // =========================

    void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        ApplyType();

        if (hazardType == null)
            Debug.LogWarning(name + ": EnvironmentalHazard has no Hazard Type — it does nothing.", this);
    }

    // Live preview when the type or size is changed in the Inspector.
    void OnValidate()
    {
        ApplyType();
    }

    // Applies the type's look and the area's size to the renderer and collider.
    public void ApplyType()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.size = size;
        box.offset = Vector2.zero;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.drawMode = hazardType != null && !hazardType.tileSprite
            ? SpriteDrawMode.Sliced
            : SpriteDrawMode.Tiled;
        sr.size = size;

        if (hazardType == null)
            return;

        if (hazardType.sprite != null)
            sr.sprite = hazardType.sprite;

        sr.color = hazardType.tint;
    }

    // Used by editor tools that build hazards.
    public void Configure(HazardType type, Vector2 areaSize)
    {
        hazardType = type;
        size = areaSize;
        ApplyType();
    }

    // =========================
    // ENTER / EXIT
    // =========================

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hazardType == null)
            return;

        HazardReceiver receiver = other.GetComponentInParent<HazardReceiver>();

        if (receiver == null || !Affects(receiver))
            return;

        if (!occupants.TryGetValue(receiver, out Occupant occupant))
        {
            occupant = new Occupant();
            occupants.Add(receiver, occupant);

            receiver.EnterHazard(this);
            receiver.ApplyHazardTick(hazardType);
            occupant.tickTimer = hazardType.tickInterval;
        }

        occupant.colliders.Add(other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        HazardReceiver receiver = other.GetComponentInParent<HazardReceiver>();

        if (receiver == null || !occupants.TryGetValue(receiver, out Occupant occupant))
            return;

        occupant.colliders.Remove(other);

        if (occupant.colliders.Count == 0)
            Release(receiver);
    }

    bool Affects(HazardReceiver receiver)
    {
        return receiver.IsPlayer ? hazardType.affectsPlayer : hazardType.affectsEnemies;
    }

    void Release(HazardReceiver receiver)
    {
        occupants.Remove(receiver);

        if (receiver != null)
            receiver.ExitHazard(this);
    }

    void OnDisable()
    {
        scratch.Clear();
        scratch.AddRange(occupants.Keys);

        foreach (HazardReceiver receiver in scratch)
            Release(receiver);
    }

    // =========================
    // TICKS
    // =========================

    void Update()
    {
        if (occupants.Count == 0 || hazardType == null)
            return;

        scratch.Clear();
        scratch.AddRange(occupants.Keys);

        foreach (HazardReceiver receiver in scratch)
        {
            // Destroyed while inside (e.g. an enemy removed on death).
            if (receiver == null)
            {
                occupants.Remove(receiver);
                continue;
            }

            Occupant occupant = occupants[receiver];
            occupant.tickTimer -= Time.deltaTime;

            while (occupant.tickTimer <= 0f)
            {
                receiver.ApplyHazardTick(hazardType);
                occupant.tickTimer += hazardType.tickInterval;
            }
        }
    }
}
