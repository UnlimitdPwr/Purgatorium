using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Builds Assets/Scenes/LevelBlockout_01.unity — the platforming test level
// drawn on paper ("Sample Scene" sketch, 2026-10-10) — as grey boxes, so the
// scene never has to be hand-edited.
//
// First run: copies SampleScene (player, camera, UI, GameManager), removes its
// enemy and ground, then builds the level. Later runs only replace the
// "Level Blockout" object, so enemies/items you add elsewhere in the scene are
// kept — but hand edits made INSIDE "Level Blockout" are lost on rebuild.
//
// Units: 1 = one Unity unit. Heights are tuned to the player's jump
// (moveSpeed 5, jumpForce 10, gravityScale 3 → ~1.6 high, ~3.4 across):
// single steps rise 1.05, gaps are 2–2.5. y = 0 is the main floor's surface.
public static class LevelBlockout01Builder
{
    const string ScenePath = "Assets/Scenes/LevelBlockout_01.unity";
    const string TemplateSceneGuid = "8c9cfa26abfee488c85f1582747f6a02"; // SampleScene
    const string RootName = "Level Blockout";
    const string MaterialPath = "Assets/Level Code/BlockoutNoFriction.physicsMaterial2D";
    const string SquareSpritePath = "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Square.png";
    const string BonfirePrefabPath = "Assets/Prefabs/Bonfire.prefab";
    const string DialogTitle = "Level Blockout";

    const int GroundLayer = 8;
    const string GroundTag = "Ground";
    const float Thickness = 0.5f;

    static readonly Vector2 PlayerSpawn = new Vector2(4f, 10f);

    // =========================
    // COLOURS
    // =========================

    static readonly Color SolidColor = new Color(0.45f, 0.45f, 0.5f);
    static readonly Color CrumbleColor = new Color(0.9f, 0.55f, 0.2f);
    static readonly Color BreakableColor = new Color(0.55f, 0.33f, 0.18f);
    static readonly Color AcidColor = new Color(0.35f, 0.95f, 0.2f, 0.6f);
    static readonly Color GateColor = new Color(0.75f, 0.2f, 0.2f);
    static readonly Color WallJumpColor = new Color(0.3f, 0.5f, 0.9f);
    static readonly Color LadderColor = new Color(0.95f, 0.8f, 0.3f, 0.8f);

    static readonly Color EnemyMarker = new Color(1f, 0.25f, 0.25f);
    static readonly Color BossMarker = new Color(0.8f, 0f, 0.6f);
    static readonly Color ChestMarker = new Color(1f, 0.85f, 0.1f);
    static readonly Color PickupMarker = new Color(0.3f, 0.9f, 1f);

    static Sprite square;
    static PhysicsMaterial2D noFriction;

    // =========================
    // MENU
    // =========================

    [MenuItem("Tools/Level Blockout/Build LevelBlockout_01")]
    public static void BuildFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(DialogTitle,
                "Stop Play Mode first — changes made during Play Mode are thrown away when it ends.", "OK");
            return;
        }

        bool exists = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null;

        string question = exists
            ? "Rebuild the '" + RootName + "' object in " + ScenePath + "?\n\n" +
              "Hand edits inside '" + RootName + "' are lost. Everything else in the scene " +
              "(enemies, items you've added) is kept."
            : "Create " + ScenePath + " from SampleScene and build the level?";

        if (!EditorUtility.DisplayDialog(DialogTitle, question, exists ? "Rebuild" : "Create", "Cancel"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorUtility.DisplayDialog(DialogTitle, Build(), "OK");
    }

    // Does the work without dialogs. Returns a message; starts with "Done" on success.
    public static string Build()
    {
        square = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);

        if (square == null)
            return Fail("Couldn't load the square sprite at " + SquareSpritePath +
                        " — is the 2D Sprite package installed?");

        noFriction = LoadOrCreateNoFrictionMaterial();

        Scene scene;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
        else
        {
            string templatePath = AssetDatabase.GUIDToAssetPath(TemplateSceneGuid);

            if (string.IsNullOrEmpty(templatePath) || !AssetDatabase.CopyAsset(templatePath, ScenePath))
                return Fail("Couldn't copy SampleScene to " + ScenePath + ".");

            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RemoveTemplateLevel(scene);
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == RootName)
                Object.DestroyImmediate(root);
        }

        GameObject level = new GameObject(RootName);
        BuildLevel(level.transform);

        if (!SetUpPlayer(out string playerError))
            return Fail(playerError);

        EditorSceneManager.MarkSceneDirty(scene);

        if (!EditorSceneManager.SaveScene(scene))
            return Fail("Built the level, but saving failed — save it manually (Ctrl/Cmd+S).");

        string message = "Done — " + ScenePath + " built and saved.";
        Debug.Log("LevelBlockout01Builder: " + message);
        return message;
    }

    static string Fail(string error)
    {
        Debug.LogWarning("LevelBlockout01Builder: " + error);
        return "Nothing was saved.\n\n" + error;
    }

    // =========================
    // SCENE SETUP
    // =========================

    // The copied SampleScene keeps its player, camera, UI and managers; its
    // ground and enemies are replaced by this level.
    static void RemoveTemplateLevel(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            bool isEnemy = root.GetComponentInChildren<EnemyController>(true) != null;
            bool isGround = root.name == "Ground";

            if (isEnemy || isGround)
                Object.DestroyImmediate(root);
        }
    }

    static bool SetUpPlayer(out string error)
    {
        error = null;
        PlayerController1 player = Object.FindFirstObjectByType<PlayerController1>(FindObjectsInactive.Include);

        if (player == null)
        {
            error = "No player (PlayerController1) found in the scene.";
            return false;
        }

        // The level needs wall jump (secret area) and ladder climbing.
        if (player.GetComponent<WallJumpScript>() == null)
            player.gameObject.AddComponent<WallJumpScript>();

        if (player.GetComponent<LadderClimbScript>() == null)
            player.gameObject.AddComponent<LadderClimbScript>();

        player.transform.position = PlayerSpawn;

        CameraFollow1 cam = Object.FindFirstObjectByType<CameraFollow1>(FindObjectsInactive.Include);

        if (cam != null)
            cam.transform.position = new Vector3(PlayerSpawn.x, PlayerSpawn.y, cam.transform.position.z);

        return true;
    }

    static PhysicsMaterial2D LoadOrCreateNoFrictionMaterial()
    {
        PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(MaterialPath);

        if (material != null)
            return material;

        // Zero friction so the player slides down walls instead of sticking to
        // them while holding toward them. Movement sets velocity directly, so
        // running and stopping on the floor are unaffected.
        material = new PhysicsMaterial2D("BlockoutNoFriction") { friction = 0f, bounciness = 0f };
        AssetDatabase.CreateAsset(material, MaterialPath);
        return material;
    }

    // =========================
    // LEVEL LAYOUT
    // =========================

    static void BuildLevel(Transform level)
    {
        Transform t;

        // ---------- Bounds ----------
        t = Group(level, "Bounds");
        Solid(t, "Left Boundary", -1f, -2f, 0f, 40f);
        Solid(t, "Right Boundary", 121f, -2f, 122f, 40f);

        // ---------- Start: player drops in at the bottom-left ----------
        t = Group(level, "1 Start");
        Marker(t, "Player Spawn (drops in)", PlayerSpawn.x, PlayerSpawn.y, PickupMarker);
        Solid(t, "Floor", 0f, -2f, 22f, 0f);
        Platform(t, "Step", 17f, 18.5f, 1.05f);

        // Pit: shallow enough to jump back out of.
        Solid(t, "Pit Bottom", 22f, -2f, 24.5f, -1.3f);
        Solid(t, "Floor After Pit", 24.5f, -2f, 32.7f, 0f);
        Platform(t, "Step Before Acid", 29.5f, 31f, 1.05f);

        // ---------- Acid run: platforms over an acid pool ----------
        t = Group(level, "2 Acid Run");
        Solid(t, "Acid Pool Bottom", 32.7f, -2f, 58.7f, -1f);
        Acid(t, "Acid", 32.7f, -1f, 58.7f, -0.35f);
        Platform(t, "Acid Platform A", 32.7f, 49.3f, 2.1f);
        Marker(t, "Enemy", 46f, 2.1f, EnemyMarker);
        Platform(t, "Acid Platform B", 51.8f, 57f, 2.1f);
        Solid(t, "Floor", 58.7f, -2f, 121f, 0f);
        Marker(t, "Enemy", 63f, 0f, EnemyMarker);

        // ---------- Jumping staircase up to the bonfire ----------
        t = Group(level, "3 Pillar Staircase");
        Solid(t, "Pillar 1", 66.5f, 0f, 68f, 1.05f);
        Solid(t, "Pillar 2", 70f, 0f, 71.5f, 2.1f);
        Solid(t, "Pillar 3", 73.5f, 0f, 75f, 3.15f);
        Solid(t, "Pillar 4", 77f, 0f, 78.5f, 4.2f);
        Solid(t, "Pillar 5", 80.5f, 0f, 82f, 5.25f);
        Platform(t, "Wall Step", 82.5f, 83.8f, 6.3f);
        Platform(t, "Step To Bonfire", 78.8f, 80.3f, 7.35f);

        // ---------- Bonfire platform + steps up to the middle ----------
        t = Group(level, "4 Bonfire Platform");
        Platform(t, "Bonfire Platform", 57.3f, 76.5f, 7.35f);
        PlaceBonfire(t, 66f, 7.35f);
        Solid(t, "Ledge Stem", 61f, 4.5f, 61.3f, 6.85f);
        Platform(t, "Ledge", 61f, 63.5f, 4.5f);
        Solid(t, "Step Block", 57.3f, 7.35f, 59f, 8.4f);
        Platform(t, "Step", 53.5f, 55.5f, 9.45f);
        Platform(t, "Step", 49.5f, 51.5f, 10.5f);

        // ---------- Middle: lower walkway, breakable wall, long upper platform ----------
        t = Group(level, "5 Middle");
        Platform(t, "Lower Walkway", 28.3f, 48f, 11.55f);
        Breakable(t, "Breakable Wall", 39f, 11.55f, 39.6f, 14.15f);
        Platform(t, "Step", 24.5f, 26.8f, 12.6f);
        Platform(t, "Step", 29f, 31f, 13.65f);
        Platform(t, "Upper Platform", 33f, 84.3f, 14.65f);
        Marker(t, "Enemy", 50f, 14.65f, EnemyMarker);
        Marker(t, "Enemy", 53f, 14.65f, EnemyMarker);

        // Jump-only steps up to the boss roof.
        Solid(t, "Jump Step 1", 72f, 14.65f, 84.3f, 15.7f);
        Solid(t, "Jump Step 2", 76.5f, 15.7f, 84.3f, 16.75f);

        // ---------- Optional: chest nook + wall-jump secret (top-left of the middle) ----------
        // The shaft walls start just above head height: jump straight up, then
        // push into a wall to grab it.
        t = Group(level, "6 Secret (optional)");
        Platform(t, "Chest Ledge", 8.7f, 19.5f, 12.6f);
        Crumbling(t, "Crumbling Floor", 19.5f, 22.5f, 12.6f);
        Marker(t, "Chest", 11f, 12.6f, ChestMarker);
        Breakable(t, "Breakable Wall", 16f, 12.6f, 16.5f, 14f);
        WallJumpWall(t, "Wall Jump Wall L", 16f, 14f, 16.5f, 21f);
        WallJumpWall(t, "Wall Jump Wall R", 19.5f, 14f, 20f, 25f);
        Solid(t, "Secret Floor", 8.7f, 20.5f, 16.5f, 21f);
        Solid(t, "Secret Ceiling", 8.2f, 25f, 20f, 25.5f);
        Solid(t, "Secret Left Wall", 8.2f, 20.5f, 8.7f, 25f);
        Marker(t, "Secret Reward", 12f, 21f, PickupMarker);

        // ---------- Boss roof + steps up to the mini-boss ----------
        t = Group(level, "7 Boss Roof");
        Solid(t, "Boss Roof L", 84.3f, 16.8f, 99f, 17.8f);
        Solid(t, "Boss Roof R", 101.5f, 16.8f, 121f, 17.8f);
        Marker(t, "Enemy", 93f, 17.8f, EnemyMarker);
        Solid(t, "Step Block", 89.5f, 17.8f, 91f, 18.85f);
        Platform(t, "Step", 86f, 88f, 19.9f);
        Platform(t, "Platform Before Arena", 67f, 84f, 20.95f);

        // ---------- Mini-boss arena + ladder up ----------
        t = Group(level, "8 Mini-Boss Arena");
        Platform(t, "Arena Floor", 37f, 64.7f, 20.95f);
        LevelGate gateL = Gate(t, "Gate L", 37f, 20.95f, 37.5f, 25f, true);
        LevelGate gateR = Gate(t, "Gate R", 64.2f, 20.95f, 64.7f, 25f, true);
        Marker(t, "Mini-Boss (drops Bonfire Seed)", 45f, 20.95f, BossMarker);
        Ladder ladder = LadderAt(t, "Ladder (drops when mini-boss dies)", 49.3f, 20.95f, 50.3f, 27.95f, 4.6f);
        Encounter(t, "Mini-Boss Encounter", 38f, 20.95f, 63.5f, 24.5f,
                  new[] { gateL, gateR }, new[] { ladder });
        Platform(t, "Ladder Top", 50.5f, 64.3f, 26.95f);

        // ---------- Optional: tough enemy + essence (top-left) ----------
        t = Group(level, "9 Top-Left (optional)");
        Platform(t, "Step", 46f, 48f, 26.95f);
        Platform(t, "Step", 41f, 43.5f, 27.5f);
        Platform(t, "Long Platform", 12f, 38.5f, 28f);
        Marker(t, "Tough Enemy (empty for now)", 25f, 28f, EnemyMarker);
        Crumbling(t, "Crumbling Platform", 7.5f, 10f, 28f);
        Platform(t, "Essence Ledge", 0f, 5.5f, 28f);
        Marker(t, "Essence Drop", 2.5f, 28f, PickupMarker);

        // ---------- Upper-right: enemy room, steps over its wall ----------
        t = Group(level, "10 Upper Right");
        Platform(t, "Room Floor", 64.3f, 87f, 25.9f);
        Marker(t, "Enemy", 75f, 25.9f, EnemyMarker);
        Marker(t, "Enemy", 80f, 25.9f, EnemyMarker);
        Solid(t, "Room Wall", 87f, 25.9f, 87.5f, 29.6f);
        Platform(t, "Wall Cap", 85f, 90f, 30.1f);
        Platform(t, "Step", 66f, 68f, 28f);
        Platform(t, "Step", 70.2f, 72.2f, 29.05f);
        Platform(t, "Step", 74.5f, 76.5f, 30.1f);
        Platform(t, "Enemy Perch", 78.5f, 83f, 31.15f);
        Marker(t, "Enemy", 80.7f, 31.15f, EnemyMarker);
        Platform(t, "Step", 92f, 94f, 29.05f);

        // ---------- One-way shaft down into the boss arena ----------
        t = Group(level, "11 Shaft");
        Platform(t, "Shaft Lip", 94.5f, 99f, 27f);
        Solid(t, "Shaft Wall L", 98.5f, 17.8f, 99f, 27f);
        Solid(t, "Shaft Wall R", 101.5f, 17.8f, 102f, 26f);

        // ---------- Optional: chest room across the shaft (top-right) ----------
        t = Group(level, "12 Top-Right Room (optional)");
        Solid(t, "Floor", 101.5f, 26f, 121f, 27f);
        Breakable(t, "Breakable Wall", 105f, 27f, 105.7f, 31.5f);
        Solid(t, "Ceiling", 105f, 31.5f, 121f, 32f);
        Marker(t, "Enemy", 113f, 27f, EnemyMarker);
        Marker(t, "Chest", 118f, 27f, ChestMarker);

        // ---------- Boss arena (end of the sample level) ----------
        t = Group(level, "13 Boss Arena");
        LevelGate bossWall = Gate(t, "Boss Wall (opens when boss dies)", 83.8f, 0f, 84.3f, 14.15f, false);
        Solid(t, "Wall Above Boss Wall", 83.8f, 14.15f, 84.3f, 17.8f);
        Marker(t, "Boss", 103f, 0f, BossMarker);
        Marker(t, "Chest", 117f, 0f, ChestMarker);
        Encounter(t, "Boss Encounter", 85f, 0f, 120.5f, 16.3f,
                  new[] { bossWall }, new Ladder[0]);
    }

    // =========================
    // PIECES
    // =========================

    static Transform Group(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    // A thin platform whose top surface is at `top`.
    static GameObject Platform(Transform parent, string name, float x1, float x2, float top)
    {
        return Solid(parent, name, x1, top - Thickness, x2, top);
    }

    static GameObject Solid(Transform parent, string name, float x1, float y1, float x2, float y2)
    {
        return Box(parent, name, x1, y1, x2, y2, SolidColor, true);
    }

    static GameObject Box(Transform parent, string name, float x1, float y1, float x2, float y2,
                          Color color, bool solid)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3((x1 + x2) * 0.5f, (y1 + y2) * 0.5f, 0f);
        go.transform.localScale = new Vector3(x2 - x1, y2 - y1, 1f);

        AddSprite(go, color);

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;

        if (solid)
        {
            col.sharedMaterial = noFriction;
            go.layer = GroundLayer;
            go.tag = GroundTag;
        }
        else
        {
            col.isTrigger = true;
        }

        return go;
    }

    static void AddSprite(GameObject go, Color color)
    {
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = color;
        sr.sortingOrder = -10;
    }

    static void Crumbling(Transform parent, string name, float x1, float x2, float top)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3((x1 + x2) * 0.5f, top - Thickness * 0.5f, 0f);
        go.transform.localScale = new Vector3(x2 - x1, Thickness, 1f);
        go.layer = GroundLayer;
        go.tag = GroundTag;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
        col.sharedMaterial = noFriction;

        // The sprite lives on a child so the shake/fall moves only the look.
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        AddSprite(visual, CrumbleColor);

        go.AddComponent<CrumblingPlatform>();
    }

    static void Breakable(Transform parent, string name, float x1, float y1, float x2, float y2)
    {
        GameObject go = Box(parent, name, x1, y1, x2, y2, BreakableColor, true);
        go.AddComponent<BreakableWall>();
    }

    static void WallJumpWall(Transform parent, string name, float x1, float y1, float x2, float y2)
    {
        GameObject go = Box(parent, name, x1, y1, x2, y2, WallJumpColor, true);
        go.AddComponent<WallJumpSurface>();
    }

    static void Acid(Transform parent, string name, float x1, float y1, float x2, float y2)
    {
        GameObject go = Box(parent, name, x1, y1, x2, y2, AcidColor, false);
        go.AddComponent<AcidPool>();
    }

    static LevelGate Gate(Transform parent, string name, float x1, float y1, float x2, float y2, bool startsOpen)
    {
        GameObject go = Box(parent, name, x1, y1, x2, y2, GateColor, true);
        LevelGate gate = go.AddComponent<LevelGate>();

        SerializedObject so = new SerializedObject(gate);
        so.FindProperty("startsOpen").boolValue = startsOpen;
        so.ApplyModifiedPropertiesWithoutUndo();

        return gate;
    }

    // The ladder is placed in its lowered position and starts raised by `raisedHeight`.
    static Ladder LadderAt(Transform parent, string name, float x1, float y1, float x2, float y2, float raisedHeight)
    {
        GameObject go = Box(parent, name, x1, y1, x2, y2, LadderColor, false);
        Ladder ladder = go.AddComponent<Ladder>();

        SerializedObject so = new SerializedObject(ladder);
        so.FindProperty("startRaised").boolValue = true;
        so.FindProperty("raisedHeight").floatValue = raisedHeight;
        so.ApplyModifiedPropertiesWithoutUndo();

        return ladder;
    }

    static void Encounter(Transform parent, string name, float x1, float y1, float x2, float y2,
                          LevelGate[] gates, Ladder[] ladders)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3((x1 + x2) * 0.5f, (y1 + y2) * 0.5f, 0f);

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(x2 - x1, y2 - y1);

        ArenaEncounter encounter = go.AddComponent<ArenaEncounter>();

        SerializedObject so = new SerializedObject(encounter);
        SetArray(so.FindProperty("gates"), gates);
        SetArray(so.FindProperty("laddersToDrop"), ladders);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetArray<T>(SerializedProperty property, IList<T> items) where T : Object
    {
        property.arraySize = items.Count;

        for (int i = 0; i < items.Count; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
    }

    static void Marker(Transform parent, string label, float x, float y, Color color)
    {
        GameObject go = new GameObject("Marker - " + label);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, y, 0f);

        BlockoutMarker marker = go.AddComponent<BlockoutMarker>();

        SerializedObject so = new SerializedObject(marker);
        so.FindProperty("label").stringValue = label;
        so.FindProperty("color").colorValue = color;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void PlaceBonfire(Transform parent, float x, float groundY)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BonfirePrefabPath);

        if (prefab == null)
        {
            Debug.LogWarning("LevelBlockout01Builder: no Bonfire prefab at " + BonfirePrefabPath +
                             " — placing a marker instead.");
            Marker(parent, "Bonfire", x, groundY, PickupMarker);
            return;
        }

        GameObject bonfire = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        bonfire.transform.position = new Vector3(x, groundY, 0f);
    }
}
