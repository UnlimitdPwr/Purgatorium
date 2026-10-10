using UnityEngine;

// A labelled placeholder for something that will go here later (an enemy, a
// chest, a boss...). Only visible in the Scene view — it does nothing in game.
public class BlockoutMarker : MonoBehaviour
{
    [SerializeField] private string label = "Marker";
    [SerializeField] private Color color = Color.red;

    public string Label => label;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = color;

        // Roughly player-sized, standing on the marker's position.
        Vector3 center = transform.position + Vector3.up * 0.6f;
        Gizmos.DrawWireCube(center, new Vector3(0.9f, 1.2f, 0f));

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = color;
        style.fontStyle = FontStyle.Bold;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.6f, label, style);
    }
#endif
}
