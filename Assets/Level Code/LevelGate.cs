using UnityEngine;

// A wall that can be opened and closed at runtime — arena doors, the wall that
// disappears when a boss dies, etc. Usually driven by an ArenaEncounter.
public class LevelGate : MonoBehaviour
{
    [Tooltip("Whether the gate is open when the scene starts.")]
    [SerializeField] private bool startsOpen = true;

    private Collider2D[] colliders;
    private Renderer[] renderers;
    private bool isOpen;

    public bool IsOpen => isOpen;

    void Awake()
    {
        colliders = GetComponentsInChildren<Collider2D>(true);
        renderers = GetComponentsInChildren<Renderer>(true);
        SetOpen(startsOpen);
    }

    [ContextMenu("Open")]
    public void Open()
    {
        SetOpen(true);
    }

    [ContextMenu("Close")]
    public void Close()
    {
        SetOpen(false);
    }

    void SetOpen(bool open)
    {
        isOpen = open;

        foreach (Collider2D col in colliders)
            col.enabled = !open;

        foreach (Renderer rend in renderers)
            rend.enabled = !open;
    }
}
