using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ApplyReferenceSettingsLayout
{
    private const float PixelScale = 1080f / 242f;
    private static Vector2 Position(float x, float y) => new Vector2(x - 202.5f, 121f - y) * PixelScale;

    private static void Configure(GameObject root)
    {
        var art = root.transform.Find("Fullscreen Castle Artwork").GetComponent<UnityEngine.UI.Image>();
        art.sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprtie/UI/MainMenu/CastlePanel-Open.png").OfType<Sprite>().First();
        art.color = Color.white;
        art.gameObject.SetActive(true);
        art.preserveAspect = true;
        root.GetComponent<UnityEngine.UI.Image>().color = new Color32(19, 13, 24, 255);
        var actions = (RectTransform)root.transform.Find("Actions");
        var layout = actions.GetComponent<HorizontalLayoutGroup>();
        if (layout != null) layout.enabled = false;
        actions.anchorMin = actions.anchorMax = actions.pivot = new Vector2(.5f, .5f);
        actions.anchoredPosition = Vector2.zero;
        actions.sizeDelta = new Vector2(405, 242) * PixelScale;
        string[] names = { "Defaults", "Cancel", "Apply" };
        for (int n = 0; n < names.Length; n++)
        {
            var button = actions.Find(names[n]).GetComponent<UnityEngine.UI.Button>();
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Position(86 + n * 116, 216);
            rect.sizeDelta = new Vector2(102, 32) * PixelScale;
            rect.localScale = Vector3.one;
            button.image.pixelsPerUnitMultiplier = 1f / PixelScale;
            button.image.type = UnityEngine.UI.Image.Type.Sliced;
        }
        float[] centersY = { 80, 110, 143 };
        for (int n = 0; n < 3; n++)
        {
            var rect = (RectTransform)root.transform.Find("Category " + n);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Position(69, centersY[n]);
            rect.sizeDelta = new Vector2(70, 19) * PixelScale;
            rect.localScale = Vector3.one;
            var image = rect.GetComponent<UnityEngine.UI.Image>();
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f / PixelScale;
        }
        var elements = root.transform.Cast<Transform>().OfType<RectTransform>()
            .Where(t => t.name == "Title" || t.name == "Actions" || t.name.StartsWith("Category ") || t.name.StartsWith("Settings Page ")).ToArray();
        var component = root.GetComponent<DarkLord.UI.SettingsReferenceLayout>();
        if (component == null) component = root.AddComponent<DarkLord.UI.SettingsReferenceLayout>();
        var serialized = new SerializedObject(component);
        serialized.FindProperty("referenceSize").vector2Value = new Vector2(405, 242) * PixelScale;
        var refs = serialized.FindProperty("elements");
        var positions = serialized.FindProperty("positions");
        refs.arraySize = positions.arraySize = elements.Length;
        for (int n = 0; n < elements.Length; n++)
        {
            refs.GetArrayElementAtIndex(n).objectReferenceValue = elements[n];
            positions.GetArrayElementAtIndex(n).vector2Value = elements[n].anchoredPosition;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        component.Refresh();
        foreach (var c in root.GetComponentsInChildren<Component>(true))
        {
            if (c == null) continue;
            EditorUtility.SetDirty(c);
            if (PrefabUtility.IsPartOfPrefabInstance(c)) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        }
    }

    public static string Main()
    {
        const string path = "Assets/Prefabs/UI/SettingsPanel.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(path);
        try { Configure(prefab); PrefabUtility.SaveAsPrefabAsset(prefab, path); }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        foreach (var pathScene in new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/SampleScene.unity" })
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(pathScene);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(pathScene, OpenSceneMode.Additive);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var view in root.GetComponentsInChildren<DarkLord.UI.SettingsPanelView>(true)) Configure(view.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
        AssetDatabase.SaveAssets();
        return "Applied reference pixel layout to prefab and both scenes.";
    }
}
