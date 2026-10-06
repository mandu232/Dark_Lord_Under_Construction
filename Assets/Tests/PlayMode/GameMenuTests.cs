#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using DarkLord.Player;
using DarkLord.Dungeon;
using DarkLord.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    public sealed class GameMenuTests
    {
        private Scene scene, titleScene;
        private Keyboard keyboard;
        private Mouse mouse;
        private InputSettings originalInput, testInput;
        private HideFlags inputFlags;
        private float timeScale, volume;
        private int quality, vsync;
        private bool audioPaused;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            timeScale = Time.timeScale; volume = AudioListener.volume; audioPaused = AudioListener.pause;
            quality = QualitySettings.GetQualityLevel(); vsync = QualitySettings.vSyncCount;
            originalInput = InputSystem.settings; inputFlags = originalInput.hideFlags;
            testInput = Object.Instantiate(originalInput);
            // Input System destroys replaced settings marked HideAndDontSave.
            // Retain this object until teardown restores its original flags.
            originalInput.hideFlags = HideFlags.DontUnloadUnusedAsset;
            testInput.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            testInput.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = testInput;
            keyboard = InputSystem.AddDevice<Keyboard>();
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/Scenes/SampleScene.unity");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (titleScene.IsValid() && titleScene.isLoaded) yield return SceneManager.UnloadSceneAsync(titleScene);
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            InputSystem.settings = originalInput; originalInput.hideFlags = inputFlags;
            Object.DestroyImmediate(testInput);
            Time.timeScale = timeScale; AudioListener.volume = volume; AudioListener.pause = audioPaused;
            QualitySettings.SetQualityLevel(quality); QualitySettings.vSyncCount = vsync;
        }

        [UnityTest]
        public IEnumerator MinionHoverInspectionUpdatesHealthAndHonoursConstructionInput()
        {
            var roots = scene.GetRootGameObjects();
            var inspector = roots.SelectMany(r => r.GetComponentsInChildren<MinionInspectionPanel>(true)).Single();
            var construction = roots.SelectMany(r => r.GetComponentsInChildren<DungeonExpansion>()).Single();
            var summoning = construction.GetComponent<MinionSummoning>();
            summoning.SetOpen(true); Assert.IsTrue(summoning.TrySummon(false));
            Assert.IsTrue(summoning.TryPlace(construction.Dungeon.Layout.Core + Vector2Int.up));
            yield return null; yield return null;
            var guard = construction.Dungeon.Guards.Single();
            var renderer = guard.GetComponentInChildren<SpriteRenderer>();
            var original = renderer.sharedMaterial;
            Assert.IsNull(inspector.FindAt(renderer.bounds.min + new Vector3(0.1f, 0.1f, 0)), "Transparent frame padding must not select the guard.");
            Assert.AreSame(guard, inspector.FindAt(renderer.transform.TransformPoint(new Vector3(0, -0.2f, 0))), "Visible body must be selectable.");
            var camera = roots.SelectMany(r => r.GetComponentsInChildren<Camera>()).Single();
            mouse = InputSystem.AddDevice<Mouse>();
            var position = (Vector2)camera.WorldToScreenPoint(renderer.bounds.center);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            yield return null; yield return null;
            Assert.AreSame(guard, inspector.Hovered);
            Assert.AreEqual("DarkLord/SpriteHoverOutline", renderer.sharedMaterial.shader.name);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 2 });
            yield return null; yield return null;
            Assert.IsTrue(inspector.IsOpen);
            Assert.AreSame(guard, inspector.Selected);
            Assert.That(inspector.StatsText, Does.Contain($"체력  {guard.Health.CurrentHealth} / {guard.Health.MaximumHealth}"));
            Assert.That(inspector.StatsText, Does.Contain($"공격력  {guard.AttackDamage}"));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            guard.Health.TakeDamage(13);
            yield return new WaitForSeconds(0.15f);
            Assert.That(inspector.StatsText, Does.Contain($"체력  {guard.Health.CurrentHealth} / {guard.Health.MaximumHealth}"));
            construction.SetExpansionMode(true);
            yield return null;
            Assert.IsFalse(inspector.IsOpen);
            Assert.IsNull(inspector.Hovered);
            Assert.AreSame(original, renderer.sharedMaterial);
            inspector.Show(guard);
            Assert.IsFalse(inspector.IsOpen);
            construction.SetExpansionMode(false);
            yield return null; yield return null;
            inspector.Show(guard);
            Assert.IsTrue(inspector.IsOpen);
            guard.Health.TakeDamage(guard.Health.MaximumHealth);
            yield return null; yield return null;
            Assert.IsFalse(inspector.IsOpen);
        }

        [UnityTest]
        public IEnumerator CorridorPlacementHidesWindowsZoomsOutAndRightClickRestoresMenu()
        {
            var roots = scene.GetRootGameObjects();
            var construction = roots.SelectMany(r => r.GetComponentsInChildren<DungeonExpansion>()).Single();
            var view = roots.SelectMany(r => r.GetComponentsInChildren<ConstructionPanel>(true)).Single();
            var camera = roots.SelectMany(r => r.GetComponentsInChildren<Camera>()).Single();
            var menu = roots.SelectMany(r => r.GetComponentsInChildren<GameMenu>()).Single();
            float originalSize = camera.orthographicSize;
            mouse = InputSystem.AddDevice<Mouse>();
            construction.SetExpansionMode(true);
            var corridor = view.GetComponentsInChildren<BuildItemButton>().Single(x => x.Item.constructionKind == ConstructionKind.Corridor);
            view.Choose(corridor.Item);
            yield return null;
            yield return null;
            Assert.IsTrue(construction.IsPlacementView);
            Assert.IsFalse(view.transform.Find("Construction Window").gameObject.activeSelf);
            Assert.IsFalse(view.transform.Find("Details").gameObject.activeSelf);
            Assert.IsFalse(view.transform.Find("Open Construction").gameObject.activeSelf);
            Assert.That(camera.orthographicSize, Is.GreaterThan(originalSize));
            InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 2 });
            yield return null;
            yield return null;
            Assert.IsFalse(construction.IsPlacementView);
            Assert.IsTrue(construction.ExpansionMode);
            Assert.IsTrue(view.transform.Find("Construction Window").gameObject.activeSelf);
            Assert.IsTrue(view.transform.Find("Details").gameObject.activeSelf);
            Assert.That(camera.orthographicSize, Is.EqualTo(originalSize).Within(0.001f));
            Assert.That(construction.TryBuild(construction.Dungeon.Layout.Core + Vector2Int.right * 6), Is.EqualTo(ConstructionResult.Unavailable));
            Assert.That(construction.Dungeon.Construction.PendingCount, Is.Zero);
            InputSystem.QueueStateEvent(mouse, new MouseState());
            yield return null;
            view.Choose(corridor.Item);
            Assert.That(construction.TryBuildCorridor(construction.Dungeon.Layout.Core + Vector2Int.right * 3, construction.Dungeon.Layout.Core + Vector2Int.right * 7), Is.EqualTo(ConstructionResult.Success));
            Assert.IsTrue(construction.IsPlacementView, "Successful placement should allow another placement until cancelled.");
            construction.ReturnToConstructionMenu();
            Assert.That(construction.Dungeon.Construction.Sections.Count, Is.EqualTo(5), "Cancelling selection must retain accepted construction.");
            view.Choose(corridor.Item);
            menu.PauseGame();
            yield return null;
            Assert.IsFalse(construction.IsPlacementView);
            Assert.That(camera.orthographicSize, Is.EqualTo(originalSize).Within(0.001f));
            menu.StartGame();
        }

        [UnityTest]
        public IEnumerator CorridorDragBuildsOnReleaseAndRightClickCancelsUncommittedDrag()
        {
            var roots = scene.GetRootGameObjects();
            var construction = roots.SelectMany(r => r.GetComponentsInChildren<DungeonExpansion>()).Single();
            var view = roots.SelectMany(r => r.GetComponentsInChildren<ConstructionPanel>(true)).Single();
            var camera = roots.SelectMany(r => r.GetComponentsInChildren<Camera>()).Single();
            mouse = InputSystem.AddDevice<Mouse>();
            construction.SetExpansionMode(true);
            var corridor = view.GetComponentsInChildren<BuildItemButton>().Single(x => x.Item.constructionKind == ConstructionKind.Corridor);
            Assert.That(view.GetComponentsInChildren<BuildItemButton>().Length, Is.EqualTo(3), "Room tab has two rooms and one draggable corridor.");
            view.Choose(corridor.Item);
            yield return null; yield return null;
            var grid = construction.Dungeon;
            var start = grid.Layout.Core + Vector2Int.right * 3;
            var end = grid.Layout.Core + Vector2Int.right * 7;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = camera.WorldToScreenPoint(grid.CellCenter(start)), buttons = 1 });
            yield return null; yield return null;
            Assert.IsTrue(construction.IsDraggingCorridor);
            Assert.That(grid.Construction.PendingCount, Is.Zero);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = camera.WorldToScreenPoint(grid.CellCenter(end)), buttons = 1 });
            yield return null; yield return null;
            Assert.That(grid.Construction.PendingCount, Is.Zero, "Dragging previews without reserving tiles.");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = camera.WorldToScreenPoint(grid.CellCenter(end)) });
            yield return null; yield return null;
            Assert.IsFalse(construction.IsDraggingCorridor);
            Assert.That(grid.Construction.Sections.Count, Is.EqualTo(5));
            Assert.That(grid.Construction.Sections.Last().Kind, Is.EqualTo(ConstructionKind.Corridor));
            Assert.IsTrue(construction.IsPlacementView);
            start = grid.Layout.Core + Vector2Int.up * 3;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = camera.WorldToScreenPoint(grid.CellCenter(start)), buttons = 1 });
            yield return null; yield return null;
            Assert.IsTrue(construction.IsDraggingCorridor);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = camera.WorldToScreenPoint(grid.CellCenter(start + Vector2Int.up * 4)), buttons = 2 });
            yield return null; yield return null;
            Assert.IsFalse(construction.IsDraggingCorridor);
            Assert.IsFalse(construction.IsPlacementView);
            Assert.That(grid.Construction.Sections.Count, Is.EqualTo(5), "Cancelled drag creates no second corridor.");
            Assert.IsTrue(view.transform.Find("Construction Window").gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator UnifiedWindowConsumesEscapeAndKeepsSummonPlacementInsideIt()
        {
            var roots = scene.GetRootGameObjects();
            var menu = roots.SelectMany(r => r.GetComponentsInChildren<GameMenu>()).Single();
            var construction = roots.SelectMany(r => r.GetComponentsInChildren<DungeonExpansion>()).Single();
            var summoning = construction.GetComponent<MinionSummoning>();
            var view = roots.SelectMany(r => r.GetComponentsInChildren<ConstructionPanel>(true)).Single();
            construction.SetExpansionMode(true);
            Assert.IsTrue(view.transform.Find("Construction Window").gameObject.activeSelf);
            yield return PressEscape();
            Assert.IsFalse(construction.ExpansionMode);
            Assert.IsFalse(view.transform.Find("Construction Window").gameObject.activeSelf);
            Assert.That(menu.State, Is.EqualTo(GameMenu.ScreenState.Playing));
            construction.SetExpansionMode(true);
            summoning.SetOpen(true);
            Assert.IsTrue(construction.ExpansionMode);
            Assert.IsTrue(view.transform.Find("Construction Window").gameObject.activeSelf);
            Assert.IsTrue(summoning.IsOpen);
            summoning.SetOpen(false);
            yield return null;
            menu.PauseGame();
            Assert.IsFalse(construction.ExpansionMode);
        }

        [UnityTest]
        public IEnumerator CategoriesHoverDetailsAndGuardPlacementShareOneWindow()
        {
            var roots = scene.GetRootGameObjects();
            var construction = roots.SelectMany(r => r.GetComponentsInChildren<DungeonExpansion>()).Single();
            var view = roots.SelectMany(r => r.GetComponentsInChildren<ConstructionPanel>(true)).Single();
            construction.SetExpansionMode(true);
            view.ShowCategory(BuildCategory.Minions);
            var cards = view.GetComponentsInChildren<BuildItemButton>();
            Assert.That(cards.Length, Is.EqualTo(1));
            var guard = cards.Single();
            Assert.That(guard.Item.category, Is.EqualTo(BuildCategory.Minions));
            UnityEngine.EventSystems.ExecuteEvents.Execute(guard.gameObject,
                new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current),
                UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler);
            Assert.That(view.DetailDescription, Is.EqualTo(guard.Item.description));
            view.Choose(guard.Item);
            var summoning = construction.GetComponent<MinionSummoning>();
            Assert.IsTrue(summoning.IsPlacing);
            Assert.IsTrue(construction.ExpansionMode);
            Assert.IsTrue(summoning.TryPlace(construction.Dungeon.Layout.Core + Vector2Int.up));
            Assert.That(summoning.GuardCount, Is.EqualTo(1));
            Assert.IsTrue(construction.ExpansionMode);
            view.ShowCategory(BuildCategory.Traps);
            var trap = view.GetComponentsInChildren<BuildItemButton>().Single();
            view.Choose(trap.Item);
            Assert.IsFalse(summoning.IsPlacing);
            Assert.IsTrue(trap.Item.available);
            var trapCell = construction.Dungeon.Layout.Core + Vector2Int.right;
            Assert.IsTrue(construction.Dungeon.TryPlaceTrap(trapCell, trap.Item.prefab));
            view.Demolish();
            Assert.IsTrue(construction.DemolitionMode);
            Assert.IsTrue(construction.TryDemolish(trapCell));
            Assert.IsFalse(construction.Dungeon.HasTrap(trapCell));
            Assert.IsTrue(construction.TryDemolish(construction.Dungeon.Layout.Core + Vector2Int.up));
            Assert.That(summoning.GuardCount, Is.Zero);
            Assert.IsFalse(construction.TryDemolish(construction.Dungeon.Layout.Core));
            yield return PressEscape();
            Assert.IsFalse(construction.ExpansionMode);
        }

        [UnityTest]
        public IEnumerator RemovingEntranceSuspendsSpawnsAndNewEntranceResumesSameWave()
        {
            var director = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<DarkLord.Gameplay.WaveDirector>()).Single();
            var grid = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<DungeonGrid>()).Single();
            director.BeginNextWave();
            int before = director.transform.childCount;
            Assert.That(before, Is.EqualTo(1));
            grid.RemoveEntrance();
            yield return new WaitForSeconds(0.9f);
            Assert.That(director.transform.childCount, Is.EqualTo(before));
            Assert.That(director.CurrentWave, Is.EqualTo(1));
            Assert.IsTrue(grid.TryPlaceEntrance(grid.Layout.Core + Vector2Int.up * 2));
            yield return new WaitForSeconds(1.6f);
            Assert.That(director.transform.childCount, Is.EqualTo(3));
            Assert.That(director.CurrentWave, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator WavesWaitForStartButtonAndCannotOverlap()
        {
            var director = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<DarkLord.Gameplay.WaveDirector>()).Single();
            var button = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UnityEngine.UI.Button>(true)).Single(b => b.name == "Start Wave Button");
            yield return new WaitForSeconds(0.2f);
            Assert.That(director.CurrentWave, Is.Zero);
            Assert.IsTrue(button.gameObject.activeSelf);
            Assert.IsTrue(button.interactable);
            button.onClick.Invoke();
            Assert.That(director.CurrentWave, Is.EqualTo(1));
            Assert.IsFalse(button.gameObject.activeSelf);
            director.BeginNextWave();
            Assert.That(director.CurrentWave, Is.EqualTo(1));
            yield return new WaitForSeconds(1.6f);
            foreach (var health in director.GetComponentsInChildren<DarkLord.Actors.ActorHealth>()) health.TakeDamage(health.MaximumHealth);
            yield return new WaitForSeconds(0.2f);
            Assert.That(director.State, Is.EqualTo(DarkLord.Gameplay.WaveState.Preparing));
            Assert.That(director.CurrentWave, Is.EqualTo(1));
            Assert.IsTrue(button.gameObject.activeSelf);
            Time.timeScale = 0;
            director.BeginNextWave();
            Assert.That(director.CurrentWave, Is.EqualTo(1));
            Time.timeScale = 1;
            button.onClick.Invoke();
            Assert.That(director.CurrentWave, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator GameplayStartsImmediatelyAndEscapePausesWithoutStealingSummonCancel()
        {
            var roots = scene.GetRootGameObjects();
            var menu = roots.SelectMany(r => r.GetComponentsInChildren<GameMenu>()).Single();
            var summoning = roots.SelectMany(r => r.GetComponentsInChildren<MinionSummoning>()).Single();
            Assert.That(menu.State, Is.EqualTo(GameMenu.ScreenState.Playing));
            Assert.That(Time.timeScale, Is.GreaterThan(0));
            yield return PressEscape();
            Assert.That(menu.State, Is.EqualTo(GameMenu.ScreenState.Pause));
            Assert.That(Time.timeScale, Is.Zero);
            yield return PressEscape();
            Assert.That(menu.State, Is.EqualTo(GameMenu.ScreenState.Playing));
            Assert.That(Time.timeScale, Is.GreaterThan(0));
            summoning.SetOpen(true);
            Assert.That(summoning.IsOpen, Is.True);
            yield return PressEscape();
            Assert.That(summoning.IsOpen, Is.False);
            Assert.That(menu.State, Is.EqualTo(GameMenu.ScreenState.Playing));
            menu.PauseGame();
            menu.ReturnToMain();
            Assert.That(menu.State, Is.EqualTo(GameMenu.ScreenState.Quit));
            menu.CancelQuit();
            Assert.That(menu.State, Is.EqualTo(GameMenu.ScreenState.Pause));
            menu.StartGame();
            Assert.That(Time.timeScale, Is.GreaterThan(0));
            Assert.That(roots.Single(r => r.name == "Game HUD").activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator SettingsCategoriesKeepDraftAndOpeningAnimationRunsWhilePaused()
        {
            var menu = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameMenu>()).Single();
            menu.PauseGame();
            menu.OpenSettings();
            var view = menu.GetComponentInChildren<SettingsPanelView>();
            var group = view.GetComponent<CanvasGroup>();
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(group.alpha, Is.LessThan(1));
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(group.alpha, Is.EqualTo(1));
            Assert.That(view.transform.localScale, Is.EqualTo(Vector3.one));
            menu.SetVolume(0.23f);
            for (int i = 0; i < 3; i++)
            {
                view.SelectCategory(i);
                Assert.That(view.SelectedCategory, Is.EqualTo(i));
                for (int page = 0; page < 3; page++)
                    Assert.That(view.transform.Find("Settings Page " + page).gameObject.activeSelf, Is.EqualTo(page == i));
            }
            var draftField = typeof(GameMenu).GetField("draft", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.That(((GameSettings)draftField.GetValue(menu)).MasterVolume, Is.EqualTo(0.23f));
            menu.CancelSettings();
            Assert.That(menu.State, Is.EqualTo(GameMenu.ScreenState.Pause));
            menu.OpenSettings();
            menu.CancelSettings(); // Interrupt the opening animation, then reopen.
            menu.OpenSettings();
            Assert.That(view.SelectedCategory, Is.Zero);
            Assert.That(((GameSettings)draftField.GetValue(menu)).MasterVolume, Is.EqualTo(GameSettings.Load().MasterVolume));
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(group.alpha, Is.EqualTo(1));
            menu.CancelSettings();
            menu.StartGame();
            Assert.That(Time.timeScale, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator TitlePrefabOpensSettingsWithAllVolumeControlsAndReturnsToMain()
        {
            yield return SceneManager.UnloadSceneAsync(scene);
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/MainMenu.unity", LoadSceneMode.Additive);
            titleScene = SceneManager.GetSceneByPath("Assets/Scenes/MainMenu.unity");
            yield return null;
            var menu = titleScene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameMenu>()).Single();
            Assert.That(menu.State, Is.EqualTo(GameMenu.ScreenState.Main));
            Assert.That(Time.timeScale, Is.Zero);
            menu.OpenSettings();
            yield return new WaitForSecondsRealtime(0.35f);
            var view = menu.GetComponentInChildren<SettingsPanelView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1));
            var sliders = view.transform.Find("Settings Page 0").GetComponentsInChildren<UnityEngine.UI.Slider>();
            Assert.That(sliders.Length, Is.EqualTo(4));
            foreach (var slider in sliders)
            {
                Assert.That(slider.onValueChanged.GetPersistentEventCount(), Is.EqualTo(1));
                Assert.That(slider.onValueChanged.GetPersistentTarget(0), Is.EqualTo(view.GetComponent<SettingsPanelBindings>()));
                slider.value = 0.25f;
            }
            var draftField = typeof(GameMenu).GetField("draft", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var draft = (GameSettings)draftField.GetValue(menu);
            Assert.That(draft.MasterVolume, Is.EqualTo(0.25f));
            Assert.That(draft.CharacterVolume, Is.EqualTo(0.25f));
            Assert.That(draft.MusicVolume, Is.EqualTo(0.25f));
            Assert.That(draft.EffectsVolume, Is.EqualTo(0.25f));
            view.SelectCategory(1);
            view.SelectCategory(2);
            menu.CancelSettings();
            Assert.That(menu.State, Is.EqualTo(GameMenu.ScreenState.Main));
            menu.OpenSettings();
            Assert.That(view.SelectedCategory, Is.Zero);
            menu.CancelSettings();
        }

        private IEnumerator PressEscape()
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }
    }
}
#endif
