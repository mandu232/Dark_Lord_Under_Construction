using System.Linq;
using DarkLord.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace DarkLord.Editor
{
    public static class StorySceneSetup
    {
        [MenuItem("Tools/Dark Lord/Create Story Scene")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Exit Play Mode before authoring the story scene.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(StorySceneController.ScenePath) != null)
                throw new System.InvalidOperationException("Story scene already exists; edit the existing scene.");
            EnsureBurnMaterial();
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var camera = new GameObject("Story Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            camera.orthographic = true; camera.transform.position = new Vector3(0, 0, -10);
            var events = new GameObject("UI Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            var controller = new GameObject("Story Scene Controller").AddComponent<StorySceneController>();
            var view = OpeningStory.CreateView(controller.transform, StardustFontSetup.Regular);
            view.Begin(null); // Save the first cut as an editor preview; runtime initializes its own progress.
            var data = new SerializedObject(controller);
            data.FindProperty("story").objectReferenceValue = view;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, StorySceneController.ScenePath);
            var scenes = EditorBuildSettings.scenes.ToList();
            scenes.RemoveAll(s => s.path == StorySceneController.ScenePath);
            scenes.Insert(Mathf.Min(1, scenes.Count), new EditorBuildSettingsScene(StorySceneController.ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            if (previous.IsValid()) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
            Debug.Log("Story scene created with letterbox subtitles and click-to-advance input.");
        }

        [MenuItem("Tools/Dark Lord/Update Story Presentation")]
        public static void UpdatePresentation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Exit Play Mode before editing the story scene.");
            EnsureBurnMaterial();
            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(StorySceneController.ScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(StorySceneController.ScenePath, OpenSceneMode.Additive);
            var view = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<OpeningStory>(true)).Single();
            var area = (RectTransform)view.transform.Find("Art Area");
            area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one;
            area.offsetMin = area.offsetMax = Vector2.zero;
            area.GetComponentInChildren<UnityEngine.UI.AspectRatioFitter>().aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;
            var top = view.transform.Find("Top Box");
            if (top != null) Object.DestroyImmediate(top.gameObject);
            var bottom = view.transform.Find("Bottom Box");
            bottom.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, .92f);
            var hint = bottom.Find("Click Hint");
            if (hint != null) Object.DestroyImmediate(hint.gameObject);
            var indicator = bottom.Find("Advance Triangle") as RectTransform;
            if (indicator == null) indicator = OpeningStory.CreateAdvanceIndicator(bottom);
            if (indicator.GetComponent<CanvasRenderer>() == null) indicator.gameObject.AddComponent<CanvasRenderer>();
            var data = new SerializedObject(view);
            data.FindProperty("advanceIndicator").objectReferenceValue = indicator;
            data.ApplyModifiedPropertiesWithoutUndo();
            view.Begin(null);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (previous.IsValid()) SceneManager.SetActiveScene(previous);
            if (opened) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("Story presentation updated: fullscreen artwork, subtitle overlay and animated triangle.");
        }

        public static void EnsureBurnMaterial()
        {
            const string path = "Assets/Resources/Story/PaperBurn.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/UI/StoryPaperBurn.shader");
            if (shader == null) throw new System.InvalidOperationException("Story burn shader is missing.");
            AssetDatabase.CreateAsset(new Material(shader) { name = "Story Paper Burn" }, path);
        }
    }
}
