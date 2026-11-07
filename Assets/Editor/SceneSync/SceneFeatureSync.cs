using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Brings the features that only one of the two main scenes has into the other,
// so SampleScene and LevelBlockout_01 play the same.
//
//   SampleScene -> LevelBlockout_01: the equipment system — EquipmentManager on
//     the player, the Equipment HUD, the bonfire's Equipment button + panel and
//     the inventory's category tabs. Copied from SampleScene with all their
//     settings; links to objects both scenes share (player, bonfire menu,
//     inventory) are re-pointed at the target scene's own objects.
//
//   LevelBlockout_01 -> SampleScene: wall jump + ladder climbing on the player
//     (with the blockout's settings), enemies converted to Enemy_Base prefab
//     instances (so they get jumping, navigation, attack data, search...), and a
//     "Feature Test Strip" to the right of the ground with one of each level
//     piece: acid, crumbling platform, breakable wall, wall-jump shaft, and a
//     gated arena whose ladder drops.
//
// Safe to run again: anything the scene already has is skipped. Each run opens
// both scenes, saves only the target, and says exactly what it changed.
public static class SceneFeatureSync
{
    const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    const string BlockoutScenePath = "Assets/Scenes/LevelBlockout_01.unity";
    const string DialogTitle = "Scene Feature Sync";
    const string StripRootName = "Feature Test Strip";

    // Objects (with their children) copied from SampleScene, by hierarchy path.
    static readonly string[] EquipmentObjects =
    {
        "Canvas/PlayerHUD/EquipmentHUD",
        "Canvas/BonfireUI/MainPanel/Equipment",
        "Canvas/BonfireUI/EquipmentPanel",
        "Canvas/InventoryUI/InventoryPanel/CategoryTabs",
    };

    // UI that SampleScene moved/resized to make room for the equipment pieces
    // (the inventory's item list sits lower to fit the category tabs above it).
    static readonly string[] EquipmentLayouts =
    {
        "Canvas/InventoryUI/InventoryPanel/ItemScrollView",
    };

    static readonly (string path, Type type)[] EquipmentComponents =
    {
        ("Player", typeof(EquipmentManager)),
    };

    static readonly (string path, Type type)[] BlockoutPlayerComponents =
    {
        ("Player", typeof(WallJumpScript)),
        ("Player", typeof(LadderClimbScript)),
    };

    // =========================
    // MENU
    // =========================

    [MenuItem("Tools/Scene Sync/Bring SampleScene Features Into LevelBlockout_01")]
    static void IntoBlockoutMenu()
    {
        RunFromMenu("Copy SampleScene's equipment system (HUD, bonfire Equipment menu, inventory tabs, " +
                    "EquipmentManager) into LevelBlockout_01 and save it?\n\nSampleScene isn't changed.",
                    IntoBlockout);
    }

    [MenuItem("Tools/Scene Sync/Bring LevelBlockout_01 Features Into SampleScene")]
    static void IntoSampleSceneMenu()
    {
        RunFromMenu("Add LevelBlockout_01's features to SampleScene and save it?\n\n" +
                    "• Wall jump + ladder climbing on the player\n" +
                    "• Enemies become Enemy_Base prefab instances (their current values are kept)\n" +
                    "• A '" + StripRootName + "' of level pieces to the right of the ground\n\n" +
                    "LevelBlockout_01 isn't changed. Tell whoever else edits SampleScene first.",
                    IntoSampleScene);
    }

    static void RunFromMenu(string question, Func<string> run)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(DialogTitle,
                "Stop Play Mode first — changes made during Play Mode are thrown away when it ends.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(DialogTitle, question, "Go", "Cancel"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        string message = run();
        Debug.Log("SceneFeatureSync: " + message);
        EditorUtility.DisplayDialog(DialogTitle, message, "OK");
    }

    // =========================
    // THE TWO DIRECTIONS (no dialogs; return a report)
    // =========================

    public static string IntoBlockout()
    {
        return Run(SampleScenePath, BlockoutScenePath, (copy, report) =>
        {
            foreach (var (path, type) in EquipmentComponents)
                copy.CopyComponent(path, type);

            foreach (string path in EquipmentObjects)
                copy.CopyObject(path);

            foreach (string path in EquipmentLayouts)
                copy.CopyRectLayout(path);

            copy.FinishLinks();
        });
    }

    public static string IntoSampleScene()
    {
        return Run(BlockoutScenePath, SampleScenePath, (copy, report) =>
        {
            foreach (var (path, type) in BlockoutPlayerComponents)
                copy.CopyComponent(path, type);

            copy.FinishLinks();

            string enemies = EnemyPrefabTool.ConvertScene(copy.Target);
            report.Add("Enemies: " + enemies.Split('\n')[0]);

            report.Add(BuildTestStrip(copy.Target));
        });
    }

    static string Run(string sourcePath, string targetPath, Action<SceneCopy, List<string>> work)
    {
        Scene target = EditorSceneManager.OpenScene(targetPath, OpenSceneMode.Single);
        Scene source = EditorSceneManager.OpenScene(sourcePath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(target);

        var report = new List<string>();
        var copy = new SceneCopy(source, target, report);

        try
        {
            work(copy, report);
        }
        finally
        {
            EditorSceneManager.CloseScene(source, true);
        }

        if (!copy.Changed && !target.isDirty)
            return "No change — " + target.name + " already has everything.\n\n" + string.Join("\n", report);

        EditorSceneManager.MarkSceneDirty(target);

        if (!EditorSceneManager.SaveScene(target))
            return "Changed " + target.name + " but saving failed — save it manually (Ctrl/Cmd+S).\n\n" +
                   string.Join("\n", report);

        return "Done — " + target.name + " updated and saved.\n\n" + string.Join("\n", report);
    }

    // =========================
    // TEST STRIP (SampleScene)
    // =========================

    // One of each LevelBlockout_01 level piece, so they can be tried in SampleScene.
    // Built with the blockout builder's pieces, starting at the right edge of
    // "Ground" with its floor level with the ground's top. Local x runs 0..60.
    static string BuildTestStrip(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == StripRootName)
                return "Test strip: already there (delete '" + StripRootName + "' and run again to rebuild it).";
        }

        Bounds? ground = GroundBounds(scene);

        if (ground == null)
            return "Test strip: skipped — no root object named 'Ground' with a collider.";

        if (!LevelBlockout01Builder.LoadPieceAssets(out string error))
            return "Test strip: skipped — " + error;

        GameObject strip = new GameObject(StripRootName);
        SceneManager.MoveGameObjectToScene(strip, scene);

        float ox = ground.Value.max.x;
        float oy = ground.Value.max.y;
        Transform t = strip.transform;

        // The pieces take world coordinates; this shifts the layout onto the ground's edge.
        float X(float x) => ox + x;
        float Y(float y) => oy + y;

        // Acid pit with one stepping platform.
        LevelBlockout01Builder.Solid(t, "Floor", X(0f), Y(-2f), X(8f), Y(0f));
        LevelBlockout01Builder.Solid(t, "Acid Pool Bottom", X(8f), Y(-2f), X(16f), Y(-1f));
        LevelBlockout01Builder.Hazard(t, "Acid", "Acid", X(8f), Y(-1f), X(16f), Y(-0.35f));
        LevelBlockout01Builder.Platform(t, "Acid Step", X(11f), X(13f), Y(1.05f));
        LevelBlockout01Builder.Solid(t, "Floor", X(16f), Y(-2f), X(60f), Y(0f));

        LevelBlockout01Builder.Crumbling(t, "Crumbling Platform", X(18f), X(21f), Y(1.05f));

        // Taller than a jump: dash into it to break it.
        LevelBlockout01Builder.Breakable(t, "Breakable Wall", X(25f), Y(0f), X(25.6f), Y(2.6f));

        // Wall-jump shaft: walls start above head height; climb, then step right onto the ledge.
        LevelBlockout01Builder.WallJumpWall(t, "Wall Jump Wall L", X(29f), Y(1.4f), X(29.5f), Y(9f));
        LevelBlockout01Builder.WallJumpWall(t, "Wall Jump Wall R", X(32f), Y(1.4f), X(32.5f), Y(7f));
        LevelBlockout01Builder.Platform(t, "Wall Jump Ledge", X(32f), X(36f), Y(7.5f));

        // Arena: gates close on entry and open again (no boss yet), and the ladder drops.
        LevelGate gateL = LevelBlockout01Builder.Gate(t, "Gate L", X(40f), Y(0f), X(40.5f), Y(4f), true);
        LevelGate gateR = LevelBlockout01Builder.Gate(t, "Gate R", X(57.5f), Y(0f), X(58f), Y(4f), true);
        Ladder ladder = LevelBlockout01Builder.LadderAt(t, "Ladder (drops when the encounter ends)",
                                                        X(48f), Y(0f), X(49f), Y(7f), 4.6f);
        LevelBlockout01Builder.Encounter(t, "Arena Encounter", X(41f), Y(0f), X(57f), Y(3.5f),
                                         new[] { gateL, gateR }, new[] { ladder });
        LevelBlockout01Builder.Platform(t, "Ladder Top", X(49.3f), X(57f), Y(6f));
        LevelBlockout01Builder.Marker(t, "Boss (assign it on Arena Encounter)", X(52f), Y(0f),
                                      LevelBlockout01Builder.BossMarker);

        LevelBlockout01Builder.Solid(t, "Right Boundary", X(60f), Y(-2f), X(61f), Y(12f));

        return "Test strip: built '" + StripRootName + "' from x " + ox.ToString("0.#") + " to " +
               (ox + 61f).ToString("0.#") + " (acid, crumbling platform, breakable wall, wall-jump shaft, arena + ladder).";
    }

    static Bounds? GroundBounds(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != "Ground")
                continue;

            Collider2D col = root.GetComponent<Collider2D>();

            if (col == null)
                continue;

            // Colliders of objects in a non-active-at-load scene may not have bounds yet.
            Physics2D.SyncTransforms();
            return col.bounds.size != Vector3.zero ? col.bounds : (Bounds?)RendererOrTransformBounds(root);
        }

        return null;
    }

    static Bounds RendererOrTransformBounds(GameObject go)
    {
        Renderer r = go.GetComponent<Renderer>();
        return r != null ? r.bounds : new Bounds(go.transform.position, go.transform.lossyScale);
    }

    // =========================
    // COPYING BETWEEN SCENES
    // =========================

    // Copies objects/components from `Source` into `Target`. References to
    // objects in the source scene are re-pointed at the object with the same
    // hierarchy path in the target scene. FinishLinks() also re-creates the
    // links FROM objects both scenes share TO the newly copied ones (e.g. the
    // bonfire menu's equipmentPanel field, a button's click event).
    class SceneCopy
    {
        public readonly Scene Source;
        public readonly Scene Target;
        public bool Changed { get; private set; }

        readonly List<string> report;
        readonly Dictionary<Object, Object> copied = new Dictionary<Object, Object>();
        readonly List<Object> created = new List<Object>();
        readonly HashSet<string> unresolved = new HashSet<string>();

        public SceneCopy(Scene source, Scene target, List<string> report)
        {
            Source = source;
            Target = target;
            this.report = report;
        }

        public void CopyObject(string path)
        {
            GameObject src = Find(Source, path);

            if (src == null)
            {
                report.Add("Skipped " + path + " — not in " + Source.name + ".");
                return;
            }

            if (Find(Target, path) != null)
            {
                report.Add("Already there: " + path);
                return;
            }

            int slash = path.LastIndexOf('/');
            GameObject parent = slash < 0 ? null : Find(Target, path.Substring(0, slash));

            if (slash >= 0 && parent == null)
            {
                report.Add("Skipped " + path + " — " + Target.name + " has no " + path.Substring(0, slash) + ".");
                return;
            }

            GameObject clone = parent != null
                ? Object.Instantiate(src, parent.transform, false)
                : Object.Instantiate(src);

            if (parent == null)
                SceneManager.MoveGameObjectToScene(clone, Target);

            clone.name = src.name;
            clone.transform.SetSiblingIndex(Mathf.Min(src.transform.GetSiblingIndex(),
                                                      clone.transform.parent != null
                                                          ? clone.transform.parent.childCount - 1
                                                          : clone.transform.GetSiblingIndex()));

            // Same hierarchy on both sides — pair everything up by order.
            Transform[] from = src.GetComponentsInChildren<Transform>(true);
            Transform[] to = clone.GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < from.Length; i++)
            {
                copied[from[i].gameObject] = to[i].gameObject;

                Component[] fromComps = from[i].GetComponents<Component>();
                Component[] toComps = to[i].GetComponents<Component>();

                for (int c = 0; c < fromComps.Length && c < toComps.Length; c++)
                {
                    if (fromComps[c] == null || toComps[c] == null)
                        continue;

                    copied[fromComps[c]] = toComps[c];
                    created.Add(toComps[c]);
                }
            }

            Changed = true;
            report.Add("Copied " + path + " (" + from.Length + " object" + (from.Length == 1 ? "" : "s") + ")");
        }

        public void CopyComponent(string path, Type type)
        {
            GameObject src = Find(Source, path);
            GameObject dst = Find(Target, path);

            Component srcComp = src != null ? src.GetComponent(type) : null;

            if (srcComp == null)
            {
                report.Add("Skipped " + type.Name + " — " + Source.name + " has no " + path + " with one.");
                return;
            }

            if (dst == null)
            {
                report.Add("Skipped " + type.Name + " — " + Target.name + " has no " + path + ".");
                return;
            }

            if (dst.GetComponent(type) != null)
            {
                report.Add("Already there: " + type.Name + " on " + path);
                return;
            }

            Component dstComp = dst.AddComponent(type);
            CopyFields(srcComp, dstComp);

            copied[srcComp] = dstComp;
            created.Add(dstComp);
            Changed = true;
            report.Add("Added " + type.Name + " to " + path + " (with " + Source.name + "'s settings)");
        }

        // Matches the size and position of a UI object both scenes have.
        public void CopyRectLayout(string path)
        {
            GameObject srcGO = Find(Source, path);
            GameObject dstGO = Find(Target, path);
            RectTransform src = srcGO != null ? srcGO.GetComponent<RectTransform>() : null;
            RectTransform dst = dstGO != null ? dstGO.GetComponent<RectTransform>() : null;

            if (src == null || dst == null)
            {
                report.Add("Skipped layout of " + path + " — not a UI object in both scenes.");
                return;
            }

            if (dst.anchorMin == src.anchorMin && dst.anchorMax == src.anchorMax && dst.pivot == src.pivot &&
                dst.anchoredPosition == src.anchoredPosition && dst.sizeDelta == src.sizeDelta)
            {
                report.Add("Already there: layout of " + path);
                return;
            }

            dst.anchorMin = src.anchorMin;
            dst.anchorMax = src.anchorMax;
            dst.pivot = src.pivot;
            dst.anchoredPosition = src.anchoredPosition;
            dst.sizeDelta = src.sizeDelta;

            Changed = true;
            report.Add("Matched the size/position of " + path);
        }

        // Call once after all CopyObject/CopyComponent calls.
        public void FinishLinks()
        {
            // 1. Copied things that point back into the source scene.
            foreach (Object obj in created)
            {
                SerializedObject so = new SerializedObject(obj);
                SerializedProperty it = so.GetIterator();

                while (it.Next(true))
                {
                    if (it.propertyType == SerializedPropertyType.ObjectReference && IsInSource(it.objectReferenceValue))
                        it.objectReferenceValue = Remap(it.objectReferenceValue, obj);
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // 2. Shared objects whose fields point at the copied things in the source.
            int linked = 0;

            foreach (GameObject root in Source.GetRootGameObjects())
            {
                foreach (Component srcComp in root.GetComponentsInChildren<Component>(true))
                {
                    if (srcComp == null || copied.ContainsKey(srcComp))
                        continue;

                    Component dstComp = FindCounterpart(srcComp);

                    if (dstComp == null)
                        continue;

                    linked += LinkFields(srcComp, dstComp);
                }
            }

            if (linked > 0)
                report.Add("Linked " + linked + " existing field" + (linked == 1 ? "" : "s") + " to the copied objects");

            foreach (string missing in unresolved)
                report.Add("Warning: " + missing);
        }

        // Copies each top-level field of `src` that references a copied object.
        int LinkFields(Component src, Component dst)
        {
            SerializedObject srcSO = new SerializedObject(src);
            SerializedObject dstSO = new SerializedObject(dst);
            SerializedProperty field = srcSO.GetIterator();
            int count = 0;

            field.Next(true);

            do
            {
                if (!ReferencesCopied(field))
                    continue;

                SerializedProperty dstField = dstSO.FindProperty(field.propertyPath);

                if (dstField == null)
                    continue;

                CopyValue(field, dstField, dst);
                Changed = true;
                count++;
                Debug.Log("SceneFeatureSync: linked " + Path(dst.transform) + " > " + dst.GetType().Name +
                          "." + field.propertyPath, dst);
            }
            while (field.Next(false));

            dstSO.ApplyModifiedPropertiesWithoutUndo();
            return count;
        }

        bool ReferencesCopied(SerializedProperty field)
        {
            SerializedProperty it = field.Copy();
            SerializedProperty end = field.GetEndProperty();
            bool enter = true;

            if (it.propertyType == SerializedPropertyType.ObjectReference)
                return it.objectReferenceValue != null && copied.ContainsKey(it.objectReferenceValue);

            while (it.Next(enter) && !SerializedProperty.EqualContents(it, end))
            {
                enter = true;

                if (it.propertyType == SerializedPropertyType.ObjectReference &&
                    it.objectReferenceValue != null && copied.ContainsKey(it.objectReferenceValue))
                    return true;
            }

            return false;
        }

        void CopyFields(Component src, Component dst)
        {
            SerializedObject srcSO = new SerializedObject(src);
            SerializedObject dstSO = new SerializedObject(dst);
            SerializedProperty field = srcSO.GetIterator();

            field.Next(true);

            do
            {
                if (field.name == "m_GameObject" || field.name == "m_Script" ||
                    field.name.StartsWith("m_Prefab") || field.name == "m_CorrespondingSourceObject" ||
                    field.name == "m_ObjectHideFlags")
                    continue;

                SerializedProperty dstField = dstSO.FindProperty(field.propertyPath);

                if (dstField != null)
                    CopyValue(field, dstField, dst);
            }
            while (field.Next(false));

            dstSO.ApplyModifiedPropertiesWithoutUndo();
        }

        void CopyValue(SerializedProperty src, SerializedProperty dst, Object owner)
        {
            if (src.isArray && src.propertyType != SerializedPropertyType.String)
            {
                dst.arraySize = src.arraySize;

                for (int i = 0; i < src.arraySize; i++)
                    CopyValue(src.GetArrayElementAtIndex(i), dst.GetArrayElementAtIndex(i), owner);

                return;
            }

            switch (src.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    dst.objectReferenceValue = IsInSource(src.objectReferenceValue)
                        ? Remap(src.objectReferenceValue, owner)
                        : src.objectReferenceValue;
                    return;

                case SerializedPropertyType.Generic:
                    SerializedProperty child = src.Copy();
                    SerializedProperty end = src.GetEndProperty();

                    if (!child.Next(true))
                        return;

                    while (!SerializedProperty.EqualContents(child, end))
                    {
                        SerializedProperty dstChild = dst.serializedObject.FindProperty(child.propertyPath);

                        if (dstChild != null)
                            CopyValue(child, dstChild, owner);

                        if (!child.Next(false))
                            break;
                    }

                    return;

                default:
                    try
                    {
                        dst.boxedValue = src.boxedValue;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("SceneFeatureSync: couldn't copy " + src.propertyPath + " (" + e.Message + ")",
                                         owner);
                    }

                    return;
            }
        }

        bool IsInSource(Object obj)
        {
            if (obj == null || EditorUtility.IsPersistent(obj))
                return false;

            if (obj is GameObject go)
                return go.scene == Source;

            if (obj is Component c)
                return c.gameObject.scene == Source;

            return false;
        }

        Object Remap(Object srcObj, Object owner)
        {
            if (copied.TryGetValue(srcObj, out Object mapped))
                return mapped;

            Object found = srcObj is Component c ? FindCounterpart(c) : Find(Target, Path(((GameObject)srcObj).transform));

            if (found == null)
            {
                string from = owner is Component oc ? Path(oc.transform) + " > " + oc.GetType().Name : owner.name;
                string what = srcObj is Component sc ? Path(sc.transform) + " (" + sc.GetType().Name + ")"
                                                      : Path(((GameObject)srcObj).transform);
                unresolved.Add(from + " pointed at " + what + ", which " + Target.name +
                               " doesn't have — left empty, set it by hand.");
            }

            return found;
        }

        // The component of the same type (and same index among that type) on the
        // object with the same path in the target scene.
        Component FindCounterpart(Component srcComp)
        {
            if (copied.TryGetValue(srcComp, out Object mapped))
                return (Component)mapped;

            GameObject dst = Find(Target, Path(srcComp.transform));

            if (dst == null)
                return null;

            Type type = srcComp.GetType();
            Component[] srcAll = srcComp.GetComponents(type);
            Component[] dstAll = dst.GetComponents(type);
            int index = Array.IndexOf(srcAll, srcComp);

            return index >= 0 && index < dstAll.Length ? dstAll[index] : null;
        }
    }

    // =========================
    // HIERARCHY PATHS
    // =========================

    // "Canvas/BonfireUI/MainPanel". A name that repeats among its siblings gets
    // its order appended: "Step[2]".
    static string Path(Transform t)
    {
        var parts = new List<string>();

        for (; t != null; t = t.parent)
            parts.Add(Segment(t));

        parts.Reverse();
        return string.Join("/", parts);
    }

    static string Segment(Transform t)
    {
        IEnumerable<Transform> siblings = t.parent != null ? Children(t.parent) : Roots(t.gameObject.scene);
        int index = 0;

        foreach (Transform s in siblings)
        {
            if (s == t)
                break;

            if (s.name == t.name)
                index++;
        }

        return index == 0 ? t.name : t.name + "[" + index + "]";
    }

    static GameObject Find(Scene scene, string path)
    {
        string[] parts = path.Split('/');
        IEnumerable<Transform> level = Roots(scene);
        Transform current = null;

        foreach (string part in parts)
        {
            string name = part;
            int index = 0;
            int bracket = part.LastIndexOf('[');

            if (bracket > 0 && part.EndsWith("]") &&
                int.TryParse(part.Substring(bracket + 1, part.Length - bracket - 2), out int parsed))
            {
                name = part.Substring(0, bracket);
                index = parsed;
            }

            current = null;

            foreach (Transform t in level)
            {
                if (t.name != name)
                    continue;

                if (index-- == 0)
                {
                    current = t;
                    break;
                }
            }

            if (current == null)
                return null;

            level = Children(current);
        }

        return current != null ? current.gameObject : null;
    }

    static IEnumerable<Transform> Children(Transform t)
    {
        for (int i = 0; i < t.childCount; i++)
            yield return t.GetChild(i);
    }

    static IEnumerable<Transform> Roots(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            yield return root.transform;
    }
}
