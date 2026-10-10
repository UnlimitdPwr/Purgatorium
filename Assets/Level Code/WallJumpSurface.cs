using UnityEngine;

// Marks a wall the player can slide down and wall-jump off. Ordinary walls
// can't be climbed unless WallJumpScript's "Require Wall Jump Surface" is off,
// which keeps one-way drops one-way.
[RequireComponent(typeof(Collider2D))]
public class WallJumpSurface : MonoBehaviour
{
}
