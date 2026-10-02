using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Editor helper that stops the player getting stuck in the jump animation, so
// nobody has to hand-edit PlayerAnimator.controller.
//
// The only way out of HeroKnight_Jump was HeroKnight_Fall, and that needs
// VerticalVelocity < -0.1 while Jump is playing. A short hop — a knockback
// pop, or landing on a ledge before the fall starts — is over before the
// 0.25s Run -> Jump blend finishes, so by the time Jump is reached the player
// is already back on the ground at 0 velocity and Jump never exits.
//
// This adds HeroKnight_Jump -> HeroKnight_Idle when IsGrounded is true and
// VerticalVelocity < 0.1 (so it can't fire on the first frames of a real jump,
// while the ground check still overlaps but the player is moving up fast).
//
// Safe to re-run.
public static class PlayerJumpLandingFix
{
    // Found by GUID so moving the file around doesn't break the tool.
    const string PlayerAnimatorGuid = "288d09e84a33fb04da517d7301990eaa";
    const string JumpStateName = "HeroKnight_Jump";
    const string IdleStateName = "HeroKnight_Idle";

    const float LandingVelocityThreshold = 0.1f;
    const float LandingBlendDuration = 0.08f;

    [MenuItem("Tools/Player Animation/Fix Stuck Jump Animation")]
    public static void FixStuckJumpAnimation()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Player Jump Fix",
                "Stop Play Mode first — changes made during Play Mode are thrown away when it ends.",
                "OK");
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(PlayerAnimatorGuid);
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);

        if (controller == null)
        {
            Debug.LogWarning("PlayerJumpLandingFix: PlayerAnimator.controller not found.");
            return;
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState jump = FindState(stateMachine, JumpStateName);
        AnimatorState idle = FindState(stateMachine, IdleStateName);

        if (jump == null || idle == null)
        {
            Debug.LogWarning("PlayerJumpLandingFix: " + JumpStateName + " or " + IdleStateName + " state missing in " + path);
            return;
        }

        foreach (AnimatorStateTransition existing in jump.transitions)
        {
            if (existing.destinationState == idle)
            {
                Debug.Log("PlayerJumpLandingFix: Jump -> Idle landing transition already exists in " + path);
                return;
            }
        }

        Undo.RecordObject(jump, "Add Jump -> Idle landing transition");

        AnimatorStateTransition landing = jump.AddTransition(idle);
        landing.hasExitTime = false;
        landing.hasFixedDuration = true;
        landing.duration = LandingBlendDuration;
        landing.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
        landing.AddCondition(AnimatorConditionMode.Less, LandingVelocityThreshold, "VerticalVelocity");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log("PlayerJumpLandingFix: added Jump -> Idle landing transition in " + path);
    }

    static AnimatorState FindState(AnimatorStateMachine stateMachine, string name)
    {
        foreach (ChildAnimatorState child in stateMachine.states)
        {
            if (child.state.name == name)
                return child.state;
        }

        return null;
    }
}
