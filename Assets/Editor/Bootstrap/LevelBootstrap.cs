using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class LevelBootstrap
{
    const string GroundLayerName = "Ground";
    const string CharRoot = "Assets/Art/Characters/HeroKnight/Sprites";
    const string CharFolder = "Assets/Art/Characters/HeroKnight";
    const string AnimRoot = "Assets/Art/Characters/HeroKnight/Animations";
    const string TileRoot = "Assets/Art/Environment/Kenney/PNG Castle";
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string MaterialsFolder = "Assets/Art/Materials";

    static readonly (string file, int frames)[] CharacterSheets =
    {
        ("Idle.png", 11),
        ("Run.png", 8),
        ("Jump.png", 3),
        ("Fall.png", 3),
        ("Attack1.png", 7),
        ("Attack2.png", 7),
        ("Take Hit.png", 4),
        ("Death.png", 11),
    };

    [MenuItem("Tools/Bootstrap/Build Level 1")]
    public static void Run()
    {
        EnsureGroundLayer();
        ConfigureTileImporters();
        var frames = ConfigureAndSliceCharacterSheets();
        var controller = BuildAnimatorController(frames);
        BuildScene(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("LevelBootstrap: done");
    }

    static void EnsureGroundLayer()
    {
        var tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        var tagManager = new SerializedObject(tagManagerAssets[0]);
        var layers = tagManager.FindProperty("layers");

        for (int i = 8; i < layers.arraySize; i++)
        {
            if (layers.GetArrayElementAtIndex(i).stringValue == GroundLayerName) return;
        }
        for (int i = 8; i < layers.arraySize; i++)
        {
            var sp = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(sp.stringValue))
            {
                sp.stringValue = GroundLayerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return;
            }
        }
        Debug.LogWarning("LevelBootstrap: no free layer slot found for Ground layer");
    }

    static void ConfigureTileImporters()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/Environment" });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 70f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }
    }

    static Dictionary<string, Sprite[]> ConfigureAndSliceCharacterSheets()
    {
        const int frameSize = 180;
        var result = new Dictionary<string, Sprite[]>();

        foreach (var (file, frameCount) in CharacterSheets)
        {
            string path = $"{CharRoot}/{file}";
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                Debug.LogWarning($"LevelBootstrap: missing importer for {path}");
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 130f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            var metas = new List<SpriteMetaData>();
            string baseName = System.IO.Path.GetFileNameWithoutExtension(file).Replace(" ", "");
            for (int i = 0; i < frameCount; i++)
            {
                metas.Add(new SpriteMetaData
                {
                    name = $"{baseName}_{i}",
                    rect = new Rect(i * frameSize, 0, frameSize, frameSize),
                    pivot = new Vector2(0.5f, 0.12f),
                    alignment = (int)SpriteAlignment.Custom,
                });
            }
            importer.spritesheet = metas.ToArray();

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            var sprites = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(s => int.Parse(s.name.Split('_').Last()))
                .ToArray();
            result[baseName] = sprites;
        }
        return result;
    }

    static AnimatorController BuildAnimatorController(Dictionary<string, Sprite[]> frames)
    {
        if (!AssetDatabase.IsValidFolder(AnimRoot))
            AssetDatabase.CreateFolder(CharFolder, "Animations");

        string path = $"{CharFolder}/HeroKnight.controller";
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);

        var sm = controller.layers[0].stateMachine;

        AnimationClip MakeClip(string name, Sprite[] sprites, bool loop)
        {
            var clip = new AnimationClip { frameRate = 12 };
            var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
            clipSettings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, clipSettings);

            var binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite",
            };
            var keyframes = new ObjectReferenceKeyframe[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe { time = i / 12f, value = sprites[i] };
            }
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
            AssetDatabase.CreateAsset(clip, $"{AnimRoot}/{name}.anim");
            return clip;
        }

        var idleClip = MakeClip("Idle", frames["Idle"], true);
        var runClip = MakeClip("Run", frames["Run"], true);
        var jumpClip = MakeClip("Jump", frames["Jump"], false);
        var fallClip = MakeClip("Fall", frames["Fall"], true);
        MakeClip("Attack1", frames["Attack1"], false);
        MakeClip("Attack2", frames["Attack2"], false);
        MakeClip("TakeHit", frames["TakeHit"], false);
        MakeClip("Death", frames["Death"], false);

        var idleState = sm.AddState("Idle");
        idleState.motion = idleClip;
        var runState = sm.AddState("Run");
        runState.motion = runClip;
        var jumpState = sm.AddState("Jump");
        jumpState.motion = jumpClip;
        var fallState = sm.AddState("Fall");
        fallState.motion = fallClip;

        sm.defaultState = idleState;

        var idleToRun = idleState.AddTransition(runState);
        idleToRun.hasExitTime = false;
        idleToRun.duration = 0.05f;
        idleToRun.AddCondition(AnimatorConditionMode.Greater, 0.05f, "Speed");

        var runToIdle = runState.AddTransition(idleState);
        runToIdle.hasExitTime = false;
        runToIdle.duration = 0.05f;
        runToIdle.AddCondition(AnimatorConditionMode.Less, 0.05f, "Speed");

        var idleToJump = idleState.AddTransition(jumpState);
        idleToJump.hasExitTime = false;
        idleToJump.duration = 0.02f;
        idleToJump.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");

        var runToJump = runState.AddTransition(jumpState);
        runToJump.hasExitTime = false;
        runToJump.duration = 0.02f;
        runToJump.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");

        var jumpToFall = jumpState.AddTransition(fallState);
        jumpToFall.hasExitTime = false;
        jumpToFall.duration = 0.02f;
        jumpToFall.AddCondition(AnimatorConditionMode.Less, 0f, "VerticalVelocity");

        var fallToIdle = fallState.AddTransition(idleState);
        fallToIdle.hasExitTime = false;
        fallToIdle.duration = 0.05f;
        fallToIdle.AddCondition(AnimatorConditionMode.If, 0, "Grounded");

        AssetDatabase.SaveAssets();
        return controller;
    }

    static Material GetSpriteLitMaterial()
    {
        if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            AssetDatabase.CreateFolder("Assets/Art", "Materials");

        string path = $"{MaterialsFolder}/SpriteLit.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        var mat = new Material(shader);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static void BuildScene(AnimatorController controller)
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var spriteMat = GetSpriteLitMaterial();

        var light2D = Object.FindFirstObjectByType<Light2D>();
        if (light2D != null)
        {
            light2D.color = new Color(0.55f, 0.65f, 0.85f);
            light2D.intensity = 0.75f;
        }

        var cam = Camera.main;
        if (cam != null)
        {
            cam.backgroundColor = new Color32(0x14, 0x16, 0x22, 0xFF);
            cam.orthographic = true;
            cam.orthographicSize = 6f;
        }

        int groundLayer = LayerMask.NameToLayer(GroundLayerName);
        Sprite groundTile = AssetDatabase.LoadAssetAtPath<Sprite>($"{TileRoot}/castleHalfMid.png");

        GameObject MakePlatform(string name, Vector3 pos, float width, float height)
        {
            var go = new GameObject(name);
            go.layer = groundLayer;
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = groundTile;
            sr.sharedMaterial = spriteMat;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(width, height);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width, height);
            return go;
        }

        MakePlatform("Ground", new Vector3(10f, -3f, 0f), 40f, 2f);
        MakePlatform("Platform_A", new Vector3(6f, 0.5f, 0f), 4f, 1f);
        MakePlatform("Platform_B", new Vector3(13f, 2.5f, 0f), 4f, 1f);
        MakePlatform("Platform_C", new Vector3(19f, 0.5f, 0f), 3f, 1f);
        MakePlatform("Platform_D", new Vector3(24f, 3.2f, 0f), 3f, 1f);

        var player = new GameObject("Player");
        player.transform.position = new Vector3(-8f, -1.4f, 0f);

        var psr = player.AddComponent<SpriteRenderer>();
        psr.sharedMaterial = spriteMat;
        psr.sortingOrder = 5;
        var idleSprites = AssetDatabase.LoadAllAssetsAtPath($"{CharRoot}/Idle.png").OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Split('_').Last())).ToArray();
        if (idleSprites.Length > 0) psr.sprite = idleSprites[0];

        var animator = player.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        var rb = player.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.freezeRotation = true;
        rb.gravityScale = 3.5f;

        var capsule = player.AddComponent<CapsuleCollider2D>();
        capsule.size = new Vector2(0.5f, 1.1f);
        capsule.offset = new Vector2(0f, -0.15f);
        capsule.direction = CapsuleDirection2D.Vertical;

        var groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform);
        groundCheck.transform.localPosition = new Vector3(0f, -0.75f, 0f);

        var playerScript = player.AddComponent<PlayerController>();
        playerScript.groundCheck = groundCheck.transform;
        playerScript.groundLayer = 1 << groundLayer;

        if (cam != null)
        {
            var follow = cam.gameObject.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = player.transform;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    [MenuItem("Tools/Bootstrap/Add Boundary Walls")]
    public static void AddBoundaryWalls()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        int groundLayer = LayerMask.NameToLayer(GroundLayerName);

        GameObject MakeWall(string name, float x)
        {
            var existing = GameObject.Find(name);
            if (existing != null) Object.DestroyImmediate(existing);

            var go = new GameObject(name);
            go.layer = groundLayer;
            go.transform.position = new Vector3(x, 5f, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.5f, 30f);
            return go;
        }

        MakeWall("Wall_Left", -11f);
        MakeWall("Wall_Right", 31f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("LevelBootstrap: boundary walls added");
    }
}
