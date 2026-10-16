using System.Linq;
using DarkLord.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace DarkLord.Editor
{
    public static class MenuSceneSetup
    {
        private const string GamePath = "Assets/Scenes/InGame.unity";
        private const string MenuPath = "Assets/Scenes/MainMenu.unity";

        [MenuItem("Tools/Dark Lord/Setup Main Menu and Settings")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            StardustFontSetup.EnsureAssets();
            var gameplay = SceneManager.GetSceneByPath(GamePath);
            if (!gameplay.isLoaded) gameplay = EditorSceneManager.OpenScene(GamePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(gameplay);
            var gameMenu = FindMenu(gameplay);
            if (gameMenu == null) { MainMenuSetup.CreateMenuHierarchy(); gameMenu = FindMenu(gameplay); }
            var titleScene = SceneManager.GetSceneByPath(MenuPath);
            if (!titleScene.isLoaded)
                titleScene = System.IO.File.Exists(MenuPath) ? EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Additive) :
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            var titleMenu = FindMenu(titleScene);
            if (titleMenu == null)
            {
                var source = new SerializedObject(gameMenu).FindProperty("mainPanel").objectReferenceValue;
                if (source == null) throw new System.InvalidOperationException("Title hierarchy is missing; restore MainMenu.unity before running setup.");
                var copy = Object.Instantiate(gameMenu.gameObject);
                copy.name = "Main Menu";
                SceneManager.MoveGameObjectToScene(copy, titleScene);
                titleMenu = copy.GetComponent<GameMenu>();
                SceneManager.SetActiveScene(titleScene);
                var camera = new GameObject("Menu Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(17, 14, 25, 255);
                camera.orthographic = true;
                camera.transform.position = new Vector3(0, 0, -10);
                var events = new GameObject("UI Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            ConfigureRole(titleMenu, true);
            ConfigureRole(gameMenu, false);
            StardustFontSetup.ApplyToScene(titleScene);
            StardustFontSetup.ApplyToScene(gameplay);
            EditorSceneManager.SaveScene(gameplay);
            EditorSceneManager.SaveScene(titleScene, MenuPath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MenuPath, true), new EditorBuildSettingsScene(GamePath, true) }
                .Concat(EditorBuildSettings.scenes.Where(s => s.path != MenuPath && s.path != GamePath)).ToArray();
            AssetDatabase.SaveAssets();
            // Leave just the title scene open; there must not be two active EventSystems.
            EditorSceneManager.CloseScene(gameplay, true);
            SceneManager.SetActiveScene(titleScene);
            Selection.activeGameObject = titleMenu.gameObject;
            Debug.Log("Separate MainMenu scene configured as startup; Stardust applied to menu and gameplay UI.");
        }

        private static GameMenu FindMenu(Scene scene) => scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<GameMenu>(true)).FirstOrDefault();

        private static void ConfigureRole(GameMenu menu, bool title)
        {
            var data = new SerializedObject(menu);
            data.FindProperty("isTitleScene").boolValue = title;
            data.FindProperty("gameplayScene").stringValue = GamePath;
            data.FindProperty("titleScene").stringValue = MenuPath;
            var confirmation = (GameObject)data.FindProperty("quitPanel").objectReferenceValue;
            if (title)
            {
                foreach (var field in new[] { "quitPanel", "keepPlayingButton", "confirmationTitle", "confirmationDescription", "confirmationAction" })
                    data.FindProperty(field).objectReferenceValue = null;
            }
            else
            {
                data.FindProperty("confirmationTitle").objectReferenceValue = confirmation.transform.Find("Title").GetComponent<TMP_Text>();
                data.FindProperty("confirmationDescription").objectReferenceValue = confirmation.transform.Find("Description").GetComponent<TMP_Text>();
                data.FindProperty("confirmationAction").objectReferenceValue = confirmation.transform.Find("Quit Actions/Confirm Quit/Label").GetComponent<TMP_Text>();
            }
            string panelField = title ? "pausePanel" : "mainPanel";
            var obsolete = (GameObject)data.FindProperty(panelField).objectReferenceValue;
            data.FindProperty(panelField).objectReferenceValue = null;
            foreach (var field in title ? new[] { "hud", "summoning", "expansion", "resumeButton" } : new[] { "startButton", "startLabel" })
                data.FindProperty(field).objectReferenceValue = null;
            data.ApplyModifiedPropertiesWithoutUndo();
            if (title && confirmation != null) Object.DestroyImmediate(confirmation);
            if (obsolete != null) Object.DestroyImmediate(obsolete);
            ((GameObject)data.FindProperty("backdrop").objectReferenceValue).SetActive(title);
            if (title) ((GameObject)data.FindProperty("mainPanel").objectReferenceValue).SetActive(true);
            EditorUtility.SetDirty(menu);
        }
    }
}
