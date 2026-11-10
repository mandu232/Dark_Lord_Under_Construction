using System.IO;
using System.Linq;
using DarkLord.Actors;
using DarkLord.Minions;
using DarkLord.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DarkLord.Editor
{
    public static class GameplaySetup
    {
        private const string Root = "Assets";

        [MenuItem("Tools/Dark Lord/Setup Player and Minions")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var player = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Animator>(true))
                .FirstOrDefault(a => a.runtimeAnimatorController != null && a.runtimeAnimatorController.name == "B_witch");
            if (player == null) { Debug.LogError("Open the scene containing the existing B_witch player first."); return; }
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Setup player and minions");
            var go = player.gameObject;
            ConfigureBody(go);
            Add<ActorHealth>(go);
            Add<TopDownMotor>(go);
            var animation = Add<ActorAnimation>(go);
            Set(animation, "useExistingSpeedParameter", p => p.boolValue = true);
            var movement = Add<PlayerMovement>(go);
            Set(movement, "inputActions", p => p.objectReferenceValue = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/InputSystem_Actions.inputactions"));

            Directory.CreateDirectory(Root);
            AssetDatabase.Refresh();
            var prefab = CreateMinionPrefab();
            var minions = Object.FindObjectsByType<MinionController>()
                .Where(m => m.gameObject.scene == scene && m.GetComponent<ActorHealth>().Faction == Faction.Dungeon).ToArray();
            if (minions.Length == 0)
            {
                minions = new MinionController[2];
                for (int i = 0; i < minions.Length; i++)
                {
                    var unit = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    Undo.RegisterCreatedObjectUndo(unit, "Create guard minion");
                    unit.name = "Guard Minion " + (i + 1);
                    unit.transform.position = go.transform.position + new Vector3(i == 0 ? -1.5f : 1.5f, -1f, 0);
                    minions[i] = unit.GetComponent<MinionController>();
                }
            }
            var camera = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>()).FirstOrDefault();
            var commands = Add<MinionCommands>(go);
            Set(commands, "worldCamera", p => p.objectReferenceValue = camera);
            Set(commands, "minions", p => {
                p.arraySize = minions.Length;
                for (int i = 0; i < minions.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = minions[i];
            });
            if (camera != null)
            {
                Set(Add<CameraFollow>(camera.gameObject), "target", p => p.objectReferenceValue = go.transform);
                ConfigurePixelCamera(camera);
            }
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = go;
            Debug.Log("Dark Lord setup complete. WASD/arrows: move. B: construction. E: return to post. Animation clips can now be assigned to Animations/Minions/Minion.controller.");
        }

        [MenuItem("Tools/Dark Lord/Add Sparring Invader")]
        private static void AddInvader()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var prefab = CreateMinionPrefab();
            var unit = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(unit, "Create sparring invader");
            unit.name = "Sparring Invader";
            unit.transform.position = new Vector3(5, 0, 0);
            Set(unit.GetComponent<ActorHealth>(), "faction", p => p.enumValueIndex = (int)Faction.Invader);
            Set(unit.GetComponent<SpriteRenderer>(), "m_Color", p => p.colorValue = new Color(1f, 0.35f, 0.3f));
            Selection.activeGameObject = unit;
            EditorSceneManager.MarkSceneDirty(unit.scene);
        }

        private static GameObject CreateMinionPrefab()
        {
            string path = Root + "/Prefabs/Minions/GuardTemplate.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            Directory.CreateDirectory(Root + "/Prefabs/Minions");
            Directory.CreateDirectory(Root + "/Art/Placeholders");
            Directory.CreateDirectory(Root + "/Animations/Minions");
            string imagePath = Root + "/Art/Placeholders/MinionPlaceholder.png";
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                    texture.SetPixel(x, y, Mathf.Abs(x - 7.5f) + Mathf.Abs(y - 7.5f) <= 7 ? Color.white : Color.clear);
            texture.Apply();
            File.WriteAllBytes(imagePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(imagePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(imagePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            string controllerPath = Root + "/Animations/Minions/Minion.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                string[] states = { "Idle", "Run", "Hit", "Attack", "Die" };
                for (int i = 0; i < states.Length; i++)
                    controller.layers[0].stateMachine.AddState(states[i], new Vector3(240, i * 80, 0));
                EditorUtility.SetDirty(controller);
            }
            var go = new GameObject("GuardMinion");
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);
            renderer.color = new Color(0.3f, 0.9f, 1f);
            renderer.sortingOrder = 1;
            go.AddComponent<Animator>().runtimeAnimatorController = controller;
            ConfigureBody(go);
            go.AddComponent<ActorHealth>();
            go.AddComponent<TopDownMotor>();
            go.AddComponent<ActorAnimation>();
            go.AddComponent<MinionController>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            AssetDatabase.SaveAssets();
            return prefab;
        }

        private static void ConfigureBody(GameObject go)
        {
            var body = Add<Rigidbody2D>(go);
            Undo.RecordObject(body, "Configure top down physics");
            body.gravityScale = 0;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            if (go.GetComponent<Collider2D>() == null) Add<CircleCollider2D>(go).radius = 0.24f;
        }

        private static void ConfigurePixelCamera(Camera camera)
        {
            // Quality-level overrides take precedence over GraphicsSettings' fallback pipeline.
            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (!(pipeline is UniversalRenderPipelineAsset))
            {
                Debug.LogWarning("Expected URP; pixel camera setup skipped.");
                return;
            }
            Undo.RecordObject(camera, "Configure pixel camera");
            camera.orthographic = true;
            camera.allowHDR = camera.allowMSAA = camera.allowDynamicResolution = false;
            var pixel = Add<PixelPerfectCamera>(camera.gameObject);
            Undo.RecordObject(pixel, "Configure pixel camera");
            pixel.assetsPPU = 16;
            pixel.refResolutionX = 320;
            pixel.refResolutionY = 180;
            pixel.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping;
            // Windowbox shrinks the view into black bars on screens that are not an exact 320x180 multiple
            // (16:10 monitors, Windows display scaling); None keeps integer pixels and shows a little more instead.
            pixel.cropFrame = PixelPerfectCamera.CropFrame.None;
            var data = camera.GetComponent<UniversalAdditionalCameraData>();
            if (data != null)
            {
                Undo.RecordObject(data, "Disable camera antialiasing");
                data.antialiasing = AntialiasingMode.None;
                data.renderPostProcessing = false;
            }
        }

        private static T Add<T>(GameObject go) where T : Component
        {
            // Unity's missing-component sentinel is not a CLR null (so ?? is incorrect here).
            var component = go.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(go);
        }
        private static void Set(Object owner, string field, System.Action<SerializedProperty> value)
        {
            var serialized = new SerializedObject(owner);
            value(serialized.FindProperty(field));
            serialized.ApplyModifiedProperties();
        }
    }
}
