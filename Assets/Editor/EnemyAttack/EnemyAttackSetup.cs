using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor helper that lines the enemy's attack hitbox up with its Attack1 slash,
// both in time and in space, so nobody has to hand-edit the .controller or the
// scene:
//
// 1. TIMING — EnemyAnimator.controller: Idle -> Attack1 and Attack1 -> Idle get
//    zero-length transitions, and Attack1 -> Idle waits for the whole clip.
//    Sprite animations can't blend, so any transition time made the picture lag
//    behind the clip's EnableHitbox/DisableHitbox events.
//
// 2. PLACEMENT — every enemy's AttackHitbox in the open scene is moved over the
//    slash arc in front of the enemy (for an enemy facing right, the way the art
//    faces). EnemyHitbox mirrors it at runtime when the enemy faces left.
//
// Safe to re-run.
public static class EnemyAttackSetup
{
    // Found by GUID so moving the file around doesn't break the tool.
    const string EnemyAnimatorGuid = "7edf01fe91b80a44dbb6b5270b6d5ae5";
    const string AttackStateName = "Attack1";
    const string IdleStateName = "Idle";

    // Where the Attack1 slash arc (frames 3-5) sits, in the enemy's local space,
    // for an enemy facing right. Measured from the sprite sheet: the arc runs
    // from ~0.14 behind the pivot to ~0.30 in front, ~0.33 to ~0.88 up.
    static readonly Vector2 HitboxCenter = new Vector2(0.08f, 0.6f);
    static readonly Vector2 HitboxSize = new Vector2(0.44f, 0.55f);

    [MenuItem("Tools/Enemy Attack/Fix Attack Timing And Hitbox")]
    public static void FixAttackTimingAndHitbox()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Enemy Attack Setup",
                "Stop Play Mode first — changes made during Play Mode are thrown away when it ends.",
                "OK");
            return;
        }

        FixAnimatorTransitions();
        FixHitboxesInOpenScene();
    }

    // =========================
    // TIMING
    // =========================

    public static void FixAnimatorTransitions()
    {
        string path = AssetDatabase.GUIDToAssetPath(EnemyAnimatorGuid);
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);

        if (controller == null)
        {
            Debug.LogWarning("EnemyAttackSetup: EnemyAnimator.controller not found.");
            return;
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idle = FindState(stateMachine, IdleStateName);
        AnimatorState attack = FindState(stateMachine, AttackStateName);

        if (idle == null || attack == null)
        {
            Debug.LogWarning("EnemyAttackSetup: Idle or Attack1 state missing in " + path);
            return;
        }

        foreach (AnimatorStateTransition transition in idle.transitions)
        {
            if (transition.destinationState != attack)
                continue;

            Undo.RecordObject(transition, "Fix Idle -> Attack1");
            transition.duration = 0f;
            transition.hasExitTime = false;
        }

        foreach (AnimatorStateTransition transition in attack.transitions)
        {
            if (transition.destinationState != idle)
                continue;

            Undo.RecordObject(transition, "Fix Attack1 -> Idle");
            transition.hasExitTime = true;
            transition.exitTime = 1f;
            transition.duration = 0f;
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log("EnemyAttackSetup: Attack1 transitions fixed in " + path);
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

    // =========================
    // PLACEMENT
    // =========================

    public static void FixHitboxesInOpenScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        bool changed = false;

        foreach (EnemyHitbox hitbox in Object.FindObjectsByType<EnemyHitbox>(FindObjectsInactive.Include))
        {
            Transform hitboxTransform = hitbox.transform;
            Collider2D collider = hitbox.GetComponent<Collider2D>();

            if (collider == null)
                continue;

            Vector3 scale = hitboxTransform.localScale;
            float scaleX = !Mathf.Approximately(scale.x, 0f) ? Mathf.Abs(scale.x) : 1f;
            float scaleY = !Mathf.Approximately(scale.y, 0f) ? Mathf.Abs(scale.y) : 1f;

            Undo.RecordObject(hitboxTransform, "Place attack hitbox");
            hitboxTransform.localPosition = new Vector3(HitboxCenter.x, HitboxCenter.y, hitboxTransform.localPosition.z);

            Undo.RecordObject(collider, "Size attack hitbox");
            collider.offset = Vector2.zero;

            Vector2 size = new Vector2(HitboxSize.x / scaleX, HitboxSize.y / scaleY);

            if (collider is CapsuleCollider2D capsule)
                capsule.size = size;
            else if (collider is BoxCollider2D box)
                box.size = size;
            else
                Debug.LogWarning("EnemyAttackSetup: unsupported collider on '" + hitbox.name + "' — only moved it.", hitbox);

            Debug.Log("EnemyAttackSetup: placed hitbox '" + hitboxTransform.name + "' on '" + hitboxTransform.root.name + "'", hitbox);
            changed = true;
        }

        if (!changed)
        {
            Debug.Log("EnemyAttackSetup: no EnemyHitbox in the open scene.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("EnemyAttackSetup: done — " + scene.name + " saved.");
    }
}
