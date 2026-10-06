using System.Linq;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Minions;
using DarkLord.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DarkLord.Editor
{
    public static class MinionSummoningSetup
    {
        [MenuItem("Tools/Dark Lord/Setup Minion Summoning")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var player = Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None).FirstOrDefault(p => p.gameObject.scene == scene);
            var dungeon = Object.FindObjectsByType<DungeonGrid>(FindObjectsSortMode.None).FirstOrDefault(d => d.gameObject.scene == scene);
            if (player == null || dungeon == null) throw new System.InvalidOperationException("Set up the player and dungeon first.");
            var workers = Object.FindObjectsByType<WorkerMiner>(FindObjectsSortMode.None).Where(w => w.gameObject.scene == scene).ToArray();
            var guards = Object.FindObjectsByType<MinionController>(FindObjectsSortMode.None)
                .Where(g => g.gameObject.scene == scene && g.GetComponent<ActorHealth>().Faction == Faction.Dungeon).ToArray();
            var guardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Minions/BloodSlimeGuard.prefab");
            if (guardPrefab == null && guards.Length == 0) throw new System.InvalidOperationException("An initial guard is needed to create the summoning prefab.");
            if (guardPrefab == null) guardPrefab = SaveTemplate(guards[0].gameObject, "Assets/Prefabs/Minions/BloodSlimeGuard.prefab");
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Replace initial minions with summoning menu");
            var summon = player.GetComponent<MinionSummoning>();
            if (summon == null) summon = Undo.AddComponent<MinionSummoning>(player.gameObject);
            var serialized = new SerializedObject(summon);
            serialized.FindProperty("dungeon").objectReferenceValue = dungeon;
            serialized.FindProperty("guardPrefab").objectReferenceValue = guardPrefab.GetComponent<MinionController>();
            serialized.ApplyModifiedProperties();
            var commands = new SerializedObject(player.GetComponent<MinionCommands>());
            commands.FindProperty("minions").arraySize = 0;
            commands.ApplyModifiedProperties();
            // Their full configuration is preserved in prefabs before removing startup instances.
            foreach (var worker in workers) Undo.DestroyObjectImmediate(worker.gameObject);
            foreach (var guard in guards) Undo.DestroyObjectImmediate(guard.gameObject);
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Summoning ready. Press B and choose the Minions category; construction needs no worker.");
        }

        private static GameObject SaveTemplate(GameObject source, string path)
        {
            var copy = Object.Instantiate(source);
            try
            {
                copy.name = source.GetComponent<WorkerMiner>() != null ? "WorkerMiner" : "BloodSlimeGuard";
                copy.transform.position = Vector3.zero;
                var worker = copy.GetComponent<WorkerMiner>();
                if (worker != null)
                {
                    var settings = new SerializedObject(worker);
                    settings.FindProperty("dungeon").objectReferenceValue = null;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                }
                return PrefabUtility.SaveAsPrefabAsset(copy, path);
            }
            finally { Object.DestroyImmediate(copy); }
        }
    }
}
