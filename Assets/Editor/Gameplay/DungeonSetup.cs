using System.Linq;
using System.Collections.Generic;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Minions;
using DarkLord.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DarkLord.Editor
{
    public static class DungeonSetup
    {
        [MenuItem("Tools/Dark Lord/Setup Dungeon Expansion")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var player = Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None).FirstOrDefault(p => p.gameObject.scene == scene);
            if (player == null) { Debug.LogError("Dungeon setup requires a PlayerMovement in the active scene."); return; }
            var shader = Shader.Find("DarkLord/DungeonPreview");
            if (shader == null) { Debug.LogError("Dungeon preview shader has not imported yet."); return; }
            // Use a dedicated terrain layer so minion sight checks do not hit other actors.
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tags.FindProperty("layers");
            int terrain = LayerMask.NameToLayer("DungeonTerrain");
            if (terrain < 0)
            {
                for (int i = 8; i < 32; i++) if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) { terrain = i; break; }
                if (terrain < 0) { Debug.LogError("No free user layer for DungeonTerrain."); return; }
                layers.GetArrayElementAtIndex(terrain).stringValue = "DungeonTerrain";
                tags.ApplyModifiedProperties();
            }
            string materialPath = "Assets/Gameplay/Materials/DungeonPreview.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath); }
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Setup dungeon expansion");
            var dungeon = Object.FindObjectsByType<DungeonGrid>(FindObjectsSortMode.None).FirstOrDefault(d => d.gameObject.scene == scene);
            if (dungeon == null)
            {
                var root = new GameObject("Dungeon");
                Undo.RegisterCreatedObjectUndo(root, "Create dungeon");
                root.transform.position = new Vector3(player.transform.position.x - 20.5f, player.transform.position.y - 15.5f, 0);
                dungeon = Undo.AddComponent<DungeonGrid>(root);
            }
            Undo.RecordObject(dungeon.gameObject, "Assign terrain layer");
            dungeon.gameObject.layer = terrain;
            Set(dungeon, "previewMaterial", p => p.objectReferenceValue = material);
            var expansion = player.GetComponent<DungeonExpansion>();
            if (expansion == null) expansion = Undo.AddComponent<DungeonExpansion>(player.gameObject);
            Set(expansion, "dungeon", p => p.objectReferenceValue = dungeon);
            Set(expansion, "worldCamera", p => p.objectReferenceValue = Camera.main);
            var worker = Object.FindObjectsByType<WorkerMiner>(FindObjectsSortMode.None).FirstOrDefault(w => w.gameObject.scene == scene);
            if (worker == null && player.GetComponent<MinionSummoning>() == null)
            {
                var workerObject = new GameObject("Worker Minion");
                Undo.RegisterCreatedObjectUndo(workerObject, "Create worker minion");
                workerObject.transform.position = player.transform.position + Vector3.up;
                worker = Undo.AddComponent<WorkerMiner>(workerObject);
                var renderer = Undo.AddComponent<MeshRenderer>(workerObject);
                renderer.sharedMaterial = material;
                renderer.sortingOrder = 2;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Undo.AddComponent<MeshFilter>(workerObject).sharedMesh = WorkerMesh();
                var body = workerObject.GetComponent<Rigidbody2D>();
                body.gravityScale = 0;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                workerObject.GetComponent<CircleCollider2D>().radius = 0.22f;
            }
            if (worker != null) Set(worker, "dungeon", p => p.objectReferenceValue = dungeon);
            Set(expansion, "worker", p => p.objectReferenceValue = worker);
            foreach (var minion in Object.FindObjectsByType<MinionController>(FindObjectsSortMode.None).Where(m => m.gameObject.scene == scene))
                Set(minion, "obstacleLayers", p => p.intValue |= 1 << terrain);
            foreach (var commands in Object.FindObjectsByType<MinionCommands>(FindObjectsSortMode.None).Where(m => m.gameObject.scene == scene))
                Set(commands, "obstacleLayers", p => p.intValue |= 1 << terrain);
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Dungeon ready. B: mining orders. Left click rock: queue / cancel. Worker moves and mines automatically.");
        }

        private static Mesh WorkerMesh()
        {
            const string path = "Assets/Gameplay/Art/Placeholders/WorkerPlaceholder.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            void Rect(float x, float y, float w, float h, Color color)
            {
                int v = vertices.Count;
                vertices.Add(new Vector3(x, y)); vertices.Add(new Vector3(x, y + h));
                vertices.Add(new Vector3(x + w, y + h)); vertices.Add(new Vector3(x + w, y));
                for (int i = 0; i < 4; i++) colors.Add(color);
                triangles.AddRange(new[] { v, v + 1, v + 2, v, v + 2, v + 3 });
            }
            Rect(-0.19f, -0.23f, 0.38f, 0.40f, new Color(0.85f, 0.40f, 0.08f));
            Rect(-0.16f, 0.1f, 0.32f, 0.23f, new Color(0.9f, 0.72f, 0.5f));
            Rect(-0.24f, 0.29f, 0.48f, 0.13f, new Color(1f, 0.8f, 0.1f));
            Rect(0.25f, -0.15f, 0.055f, 0.56f, new Color(0.40f, 0.25f, 0.13f));
            Rect(0.15f, 0.35f, 0.28f, 0.07f, new Color(0.75f, 0.85f, 0.90f));
            var mesh = new Mesh { name = "Worker placeholder (no sprites)" };
            mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static void Set(Object target, string field, System.Action<SerializedProperty> edit)
        {
            var serialized = new SerializedObject(target);
            edit(serialized.FindProperty(field));
            serialized.ApplyModifiedProperties();
        }
    }
}
