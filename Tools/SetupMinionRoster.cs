if (!UnityEngine.Application.dataPath.EndsWith("Dark Lord Under Construction/Assets")) throw new System.InvalidOperationException("Unexpected project");
if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play mode first");
var rosterPath = "Assets/Resources/MinionRoster.asset";
var roster = UnityEditor.AssetDatabase.LoadAssetAtPath<DarkLord.Minions.MinionRoster>(rosterPath);
if (roster == null) { roster = UnityEngine.ScriptableObject.CreateInstance<DarkLord.Minions.MinionRoster>(); UnityEditor.AssetDatabase.CreateAsset(roster, rosterPath); }
var prefabs = new DarkLord.Minions.MinionController[2];
for (int i = 0; i < 2; i++)
{
    string path = "Assets/Prefabs/Minions/" + (i == 0 ? "BloodArcher" : "BloodSupport") + ".prefab";
    if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path) == null)
        UnityEditor.AssetDatabase.CopyAsset("Assets/Prefabs/Minions/BloodSlimeGuard.prefab", path);
    var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
    try
    {
        var controller = root.GetComponent<DarkLord.Minions.MinionController>();
        var data = new UnityEditor.SerializedObject(controller);
        data.FindProperty("displayName").stringValue = i == 0 ? "혈침 슬라임" : "수혈 슬라임";
        data.FindProperty("role").intValue = i + 1; data.FindProperty("traits").intValue = 1; data.FindProperty("attackType").intValue = 1;
        data.FindProperty("attackDamage").intValue = i == 0 ? 14 : 4;
        data.FindProperty("attackRange").floatValue = i == 0 ? 6 : 3.5f;
        data.FindProperty("detectionRange").floatValue = i == 0 ? 7 : 5;
        data.FindProperty("attackCooldown").floatValue = i == 0 ? 1.25f : 1.6f;
        data.FindProperty("attackWindup").floatValue = i == 0 ? .3f : .25f;
        data.ApplyModifiedPropertiesWithoutUndo();
        var health = new UnityEditor.SerializedObject(root.GetComponent<DarkLord.Actors.ActorHealth>());
        health.FindProperty("maximumHealth").intValue = i == 0 ? 60 : 70; health.ApplyModifiedPropertiesWithoutUndo();
        if (root.GetComponent<DarkLord.Minions.MinionRoleMarker>() == null) root.AddComponent<DarkLord.Minions.MinionRoleMarker>();
        if (i == 1 && root.GetComponent<DarkLord.Minions.MinionSupport>() == null) root.AddComponent<DarkLord.Minions.MinionSupport>();
        var renderer = root.GetComponentsInChildren<UnityEngine.SpriteRenderer>().First(r => r.sprite != null);
        renderer.color = i == 0 ? new UnityEngine.Color(.78f,.88f,1) : new UnityEngine.Color(.7f,1,.8f);
        // Keep source animation and pivot alignment; the role badge and attack traces identify the variants.
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
    }
    finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
    prefabs[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path).GetComponent<DarkLord.Minions.MinionController>();
}
roster.archer = prefabs[0]; roster.support = prefabs[1]; UnityEditor.EditorUtility.SetDirty(roster); UnityEditor.AssetDatabase.SaveAssets();
return "Blood archer and support prefabs installed in the bundled recruitment roster.";
