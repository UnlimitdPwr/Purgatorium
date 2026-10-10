using System.Collections.Generic;
using UnityEngine;

// Brings enemies back when the player rests at a bonfire: every enemy —
// killed or still alive — is replaced by a fresh copy exactly as it was
// placed in the scene (position, health, settings, AI state).
//
// How: when the game starts, each enemy with this component makes a hidden,
// switched-off copy of itself — its spawn template. RespawnAll() removes
// whatever is left of each enemy and spawns a new one from its template.
// Bonfire.Rest calls RespawnAll().
public class EnemyRespawn : MonoBehaviour
{
    private class SpawnEntry
    {
        public GameObject template;
        public GameObject current;
        public Transform parent;
    }

    private static readonly List<SpawnEntry> entries = new List<SpawnEntry>();

    // Set while RespawnAll spawns an enemy, so the new copy takes over its
    // entry instead of making another template.
    private static SpawnEntry spawning;

    // Switched-off object the templates live under. Being inactive is what
    // keeps the templates from running any of their scripts.
    private static Transform templateHolder;

    // Domain reload is off when entering Play Mode, so statics survive
    // between runs — clear them at the start of each one.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        entries.Clear();
        spawning = null;
        templateHolder = null;
    }

    void Awake()
    {
        if (spawning != null)
        {
            spawning.current = gameObject;
            return;
        }

        MakeTemplate();
    }

    // =========================
    // RESPAWN
    // =========================

    // Replaces every enemy with a fresh copy at its starting spot.
    public static void RespawnAll()
    {
        // Templates go away with their scene.
        entries.RemoveAll(entry => entry.template == null);

        foreach (SpawnEntry entry in entries)
        {
            if (entry.current != null)
            {
                // Switch it off now (frees its attack token, stops its AI);
                // Destroy only happens at the end of the frame.
                entry.current.SetActive(false);
                Destroy(entry.current);
            }

            spawning = entry;

            // Copies the template's local position/rotation/scale, which are
            // the original's — so it lands where the enemy was placed.
            GameObject fresh = Instantiate(entry.template, entry.parent, false);
            fresh.name = entry.template.name;

            spawning = null;
        }
    }

    // =========================
    // INTERNAL
    // =========================

    void MakeTemplate()
    {
        GameObject template = Instantiate(gameObject, GetTemplateHolder(), false);
        template.name = name;

        entries.Add(new SpawnEntry
        {
            template = template,
            current = gameObject,
            parent = transform.parent
        });
    }

    static Transform GetTemplateHolder()
    {
        if (templateHolder == null)
        {
            GameObject holder = new GameObject("Enemy Spawn Templates");
            holder.SetActive(false);
            templateHolder = holder.transform;
        }

        return templateHolder;
    }
}
