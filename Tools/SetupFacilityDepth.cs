using DarkLord.Dungeon;
using UnityEditor;
using UnityEngine;

public static class SetupFacilityDepth
{
    public static string Run()
    {
        var keys = new[]{"AcidVat","ArcaneFocus","ArcaneWard","BloodAltar","BloodstoneExtractor","GoldMint","IronWard","MeleeTotem","MinionQuarters","MinionRecruitment","WarBanner"};
        foreach (var key in keys)
        {
            string path = "Assets/Prefabs/Dungeon/" + key + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var circle in root.GetComponents<CircleCollider2D>()) UnityEngine.Object.DestroyImmediate(circle);
                var box = root.GetComponent<BoxCollider2D>();
                if (box == null) box = root.AddComponent<BoxCollider2D>();
                box.offset = new Vector2(0,.5625f); box.size = new Vector2(1.25f,.5f); box.isTrigger = false;
                ConfigureDepth(root);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        const string corePath = "Assets/Resources/DungeonCoreVisual.prefab";
        var core = PrefabUtility.LoadPrefabContents(corePath);
        try { ConfigureDepth(core); PrefabUtility.SaveAsPrefabAsset(core,corePath); }
        finally { PrefabUtility.UnloadPrefabContents(core); }
        return "11 facility footprints and depth ordering; core visual depth ordering.";
    }
    private static void ConfigureDepth(GameObject root)
    {
        var depth = root.GetComponent<FacilityDepthSorting>();
        if (depth == null) depth = root.AddComponent<FacilityDepthSorting>();
        var data = new SerializedObject(depth);
        data.FindProperty("body").objectReferenceValue = root.transform.Find("Intact").GetComponent<SpriteRenderer>();
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
